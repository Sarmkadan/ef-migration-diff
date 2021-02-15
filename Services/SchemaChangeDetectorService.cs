// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Models;
using System.Text.RegularExpressions;

namespace EfMigrationDiff.Services;

/// <summary>
/// Service for parsing migration files and detecting schema changes.
/// </summary>
public class SchemaChangeDetectorService
{
    /// <summary>
    /// Detects all schema changes in a migration.
    /// </summary>
    public List<SchemaChange> DetectChanges(Migration migration)
    {
        var changes = new List<SchemaChange>();
        var lines = migration.Content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var change = ParseLine(migration.Id, line, i + 1);
            if (change != null)
            {
                changes.Add(change);
            }
        }

        return changes;
    }

    /// <summary>
    /// Parses a single line and detects the schema change type.
    /// </summary>
    private SchemaChange? ParseLine(string migrationId, string line, int lineNumber)
    {
        // Extract CREATE TABLE
        var createTableMatch = Regex.Match(line, @"CreateTable\s*\(\s*name:\s*""([^""]+)""", RegexOptions.IgnoreCase);
        if (createTableMatch.Success)
        {
            return new SchemaChange(migrationId, SqlChangeType.CreateTable, line)
            {
                TableName = createTableMatch.Groups[1].Value,
                LineNumber = lineNumber
            };
        }

        // Extract DROP TABLE
        var dropTableMatch = Regex.Match(line, @"DropTable\s*\(\s*name:\s*""([^""]+)""", RegexOptions.IgnoreCase);
        if (dropTableMatch.Success)
        {
            return new SchemaChange(migrationId, SqlChangeType.DropTable, line)
            {
                TableName = dropTableMatch.Groups[1].Value,
                LineNumber = lineNumber
            };
        }

        // Extract ADD COLUMN
        var addColumnMatch = Regex.Match(line, @"AddColumn\s*\([^)]*name:\s*""([^""]+)""[^)]*\)\s*on\s+""([^""]+)""", RegexOptions.IgnoreCase);
        if (addColumnMatch.Success)
        {
            return new SchemaChange(migrationId, SqlChangeType.AddColumn, line)
            {
                ColumnName = addColumnMatch.Groups[1].Value,
                TableName = addColumnMatch.Groups[2].Value,
                LineNumber = lineNumber
            };
        }

        // Extract DROP COLUMN
        var dropColumnMatch = Regex.Match(line, @"DropColumn\s*\([^)]*name:\s*""([^""]+)""[^)]*\)\s*on\s+""([^""]+)""", RegexOptions.IgnoreCase);
        if (dropColumnMatch.Success)
        {
            return new SchemaChange(migrationId, SqlChangeType.DropColumn, line)
            {
                ColumnName = dropColumnMatch.Groups[1].Value,
                TableName = dropColumnMatch.Groups[2].Value,
                LineNumber = lineNumber
            };
        }

        // Extract ALTER COLUMN/MODIFY COLUMN
        var modifyColumnMatch = Regex.Match(line, @"AlterColumn\s*\([^)]*name:\s*""([^""]+)""[^)]*\)\s*on\s+""([^""]+)""", RegexOptions.IgnoreCase);
        if (modifyColumnMatch.Success)
        {
            return new SchemaChange(migrationId, SqlChangeType.ModifyColumn, line)
            {
                ColumnName = modifyColumnMatch.Groups[1].Value,
                TableName = modifyColumnMatch.Groups[2].Value,
                LineNumber = lineNumber
            };
        }

        // Extract CREATE INDEX
        var createIndexMatch = Regex.Match(line, @"CreateIndex\s*\([^)]*name:\s*""([^""]+)""[^)]*table:\s*""([^""]+)""", RegexOptions.IgnoreCase);
        if (createIndexMatch.Success)
        {
            var change = new SchemaChange(migrationId, SqlChangeType.CreateIndex, line)
            {
                TableName = createIndexMatch.Groups[2].Value,
                LineNumber = lineNumber
            };
            change.AddMetadata("IndexName", createIndexMatch.Groups[1].Value);
            return change;
        }

        // Extract DROP INDEX
        var dropIndexMatch = Regex.Match(line, @"DropIndex\s*\([^)]*name:\s*""([^""]+)""[^)]*table:\s*""([^""]+)""", RegexOptions.IgnoreCase);
        if (dropIndexMatch.Success)
        {
            var change = new SchemaChange(migrationId, SqlChangeType.DropIndex, line)
            {
                TableName = dropIndexMatch.Groups[2].Value,
                LineNumber = lineNumber
            };
            change.AddMetadata("IndexName", dropIndexMatch.Groups[1].Value);
            return change;
        }

        // Extract ADD FOREIGN KEY
        var addFkMatch = Regex.Match(line, @"AddForeignKey\s*\([^)]*table:\s*""([^""]+)""", RegexOptions.IgnoreCase);
        if (addFkMatch.Success)
        {
            return new SchemaChange(migrationId, SqlChangeType.AddForeignKey, line)
            {
                TableName = addFkMatch.Groups[1].Value,
                LineNumber = lineNumber
            };
        }

        // Extract DROP FOREIGN KEY
        var dropFkMatch = Regex.Match(line, @"DropForeignKey\s*\([^)]*table:\s*""([^""]+)""", RegexOptions.IgnoreCase);
        if (dropFkMatch.Success)
        {
            return new SchemaChange(migrationId, SqlChangeType.DropForeignKey, line)
            {
                TableName = dropFkMatch.Groups[1].Value,
                LineNumber = lineNumber
            };
        }

        // Extract RAW SQL operations
        if (line.Contains("migrationBuilder.Sql(", StringComparison.OrdinalIgnoreCase))
        {
            var sqlMatch = Regex.Match(line, @"Sql\s*\(\s*""([^""]+)""", RegexOptions.IgnoreCase);
            if (sqlMatch.Success)
            {
                var sql = sqlMatch.Groups[1].Value;
                var changeType = DetermineChangeTypeFromSql(sql);
                if (changeType != SqlChangeType.Unknown)
                {
                    return new SchemaChange(migrationId, changeType, line)
                    {
                        LineNumber = lineNumber
                    };
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Determines the change type from raw SQL.
    /// </summary>
    private SqlChangeType DetermineChangeTypeFromSql(string sql)
    {
        var upperSql = sql.ToUpperInvariant();

        if (upperSql.Contains("CREATE TABLE"))
            return SqlChangeType.CreateTable;
        if (upperSql.Contains("DROP TABLE"))
            return SqlChangeType.DropTable;
        if (upperSql.Contains("ALTER TABLE") && upperSql.Contains("ADD"))
            return SqlChangeType.AddColumn;
        if (upperSql.Contains("ALTER TABLE") && upperSql.Contains("DROP"))
            return SqlChangeType.DropColumn;
        if (upperSql.Contains("CREATE INDEX"))
            return SqlChangeType.CreateIndex;
        if (upperSql.Contains("DROP INDEX"))
            return SqlChangeType.DropIndex;
        if (upperSql.Contains("CREATE PROCEDURE"))
            return SqlChangeType.CreateProcedure;
        if (upperSql.Contains("DROP PROCEDURE"))
            return SqlChangeType.DropProcedure;
        if (upperSql.Contains("CREATE VIEW"))
            return SqlChangeType.CreateView;
        if (upperSql.Contains("DROP VIEW"))
            return SqlChangeType.DropView;

        return SqlChangeType.Unknown;
    }

    /// <summary>
    /// Gets all changes of a specific type in a migration.
    /// </summary>
    public List<SchemaChange> GetChangesByType(Migration migration, SqlChangeType changeType)
    {
        var changes = DetectChanges(migration);
        return changes.Where(c => c.ChangeType == changeType).ToList();
    }

    /// <summary>
    /// Gets all table names affected by changes in a migration.
    /// </summary>
    public List<string> GetAffectedTables(Migration migration)
    {
        var changes = DetectChanges(migration);
        return changes.Where(c => !string.IsNullOrEmpty(c.TableName))
                     .Select(c => c.TableName)
                     .Distinct()
                     .ToList();
    }

    /// <summary>
    /// Counts destructive changes in a migration.
    /// </summary>
    public int CountDestructiveChanges(Migration migration)
    {
        var changes = DetectChanges(migration);
        return changes.Count(c => c.IsDestructive());
    }

    /// <summary>
    /// Checks if a migration is safe (no destructive operations).
    /// </summary>
    public bool IsMigrationSafe(Migration migration)
    {
        return CountDestructiveChanges(migration) == 0;
    }

    /// <summary>
    /// Gets all metadata about schema changes in a migration.
    /// </summary>
    public Dictionary<string, object> GetMigrationMetadata(Migration migration)
    {
        var changes = DetectChanges(migration);
        var metadata = new Dictionary<string, object>
        {
            { "TotalChanges", changes.Count },
            { "DestructiveChanges", changes.Count(c => c.IsDestructive()) },
            { "TablesCreated", changes.Count(c => c.ChangeType == SqlChangeType.CreateTable) },
            { "TablesDropped", changes.Count(c => c.ChangeType == SqlChangeType.DropTable) },
            { "ColumnsAdded", changes.Count(c => c.ChangeType == SqlChangeType.AddColumn) },
            { "ColumnsDropped", changes.Count(c => c.ChangeType == SqlChangeType.DropColumn) },
            { "IndexesCreated", changes.Count(c => c.ChangeType == SqlChangeType.CreateIndex) },
            { "IndexesDropped", changes.Count(c => c.ChangeType == SqlChangeType.DropIndex) },
            { "IsSafe", IsMigrationSafe(migration) }
        };
        return metadata;
    }
}
