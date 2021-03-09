#nullable enable
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

        // Collapse multi-line method calls into single logical lines for regex matching
        var normalised = System.Text.RegularExpressions.Regex.Replace(
            migration.Content,
            @"\r?\n\s*",
            " ");

        // Split on statement boundaries (each migrationBuilder call starts a new logical line)
        var statements = System.Text.RegularExpressions.Regex.Split(
            normalised,
            @"(?=migrationBuilder\.)");

        int lineNumber = 1;
        foreach (var stmt in statements)
        {
            var trimmed = stmt.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                lineNumber++;
                continue;
            }

            var change = ParseLine(migration.Id, trimmed, lineNumber);
            if (change is not null)
            {
                changes.Add(change);
            }

            lineNumber++;
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
        var addColumnMatch = Regex.Match(line, @"AddColumn\s*<[^>]+>\s*\([^)]*name:\s*""([^""]+)""[^)]*table:\s*""([^""]+)""", RegexOptions.IgnoreCase);
        if (addColumnMatch.Success)
        {
            var change = new SchemaChange(migrationId, SqlChangeType.AddColumn, line)
            {
                ColumnName = addColumnMatch.Groups[1].Value,
                TableName = addColumnMatch.Groups[2].Value,
                LineNumber = lineNumber
            };

            var defaultValueMatch = Regex.Match(line, @"defaultValue(?:Sql)?:\s*(.+?)(?:,|$|\))", RegexOptions.IgnoreCase);
            if (defaultValueMatch.Success)
            {
                change.DefaultValue = defaultValueMatch.Groups[1].Value.Trim().TrimEnd(',');
            }

            var nullableMatch = Regex.Match(line, @"nullable:\s*(true|false)", RegexOptions.IgnoreCase);
            if (nullableMatch.Success)
            {
                change.AddMetadata("Nullable", nullableMatch.Groups[1].Value);
            }

            return change;
        }

        // Extract DROP COLUMN
        var dropColumnMatch = Regex.Match(line, @"DropColumn\s*\([^)]*name:\s*""([^""]+)""[^)]*table:\s*""([^""]+)""", RegexOptions.IgnoreCase);
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
        var modifyColumnMatch = Regex.Match(line, @"AlterColumn\s*<[^>]+>\s*\([^)]*name:\s*""([^""]+)""[^)]*table:\s*""([^""]+)""", RegexOptions.IgnoreCase);
        if (modifyColumnMatch.Success)
        {
            var change = new SchemaChange(migrationId, SqlChangeType.ModifyColumn, line)
            {
                ColumnName = modifyColumnMatch.Groups[1].Value,
                TableName = modifyColumnMatch.Groups[2].Value,
                LineNumber = lineNumber
            };

            var defaultValueMatch = Regex.Match(line, @"defaultValue(?:Sql)?:\s*(.+?)(?:,|$|\))", RegexOptions.IgnoreCase);
            if (defaultValueMatch.Success)
            {
                change.DefaultValue = defaultValueMatch.Groups[1].Value.Trim().TrimEnd(',');
            }

            return change;
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

        // Extract ALTER TABLE
        var alterTableMatch = Regex.Match(line, @"AlterTable\s*\(\s*name:\s*""([^""]+)""", RegexOptions.IgnoreCase);
        if (alterTableMatch.Success)
        {
            return new SchemaChange(migrationId, SqlChangeType.AlterTable, line)
            {
                TableName = alterTableMatch.Groups[1].Value,
                LineNumber = lineNumber
            };
        }

        // Extract RENAME TABLE
        var renameTableMatch = Regex.Match(line, @"RenameTable\s*\([^)]*name:\s*""([^""]+)""[^)]*newName:\s*""([^""]+)""", RegexOptions.IgnoreCase);
        if (renameTableMatch.Success)
        {
            return new SchemaChange(migrationId, SqlChangeType.Rename, line)
            {
                OldValue = renameTableMatch.Groups[1].Value,
                NewValue = renameTableMatch.Groups[2].Value,
                LineNumber = lineNumber
            };
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
    /// Checks if a migration is safe (no destructive operations or non-nullable column additions).
    /// </summary>
    public bool IsMigrationSafe(Migration migration)
    {
        if (CountDestructiveChanges(migration) > 0)
            return false;

        // Adding a non-nullable column to an existing table is a breaking change
        var changes = DetectChanges(migration);
        foreach (var change in changes)
        {
            if (change.ChangeType == SqlChangeType.AddColumn &&
                change.GetMetadata("Nullable") is string nullable &&
                nullable.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
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
