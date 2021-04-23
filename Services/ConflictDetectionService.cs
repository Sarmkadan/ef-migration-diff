// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Models;

namespace EfMigrationDiff.Services;

/// <summary>
/// Service for detecting conflicts between migration schema changes.
/// </summary>
public class ConflictDetectionService
{
    private const int MaxTableNameLength = 128;
    private const int MaxColumnNameLength = 128;

    /// <summary>
    /// Detects conflicts between two sets of schema changes.
    /// </summary>
    public List<ConflictInfo> DetectConflicts(List<SchemaChange> sourceChanges, List<SchemaChange> targetChanges)
    {
        var conflicts = new List<ConflictInfo>();

        // Check for conflicting table operations
        conflicts.AddRange(DetectTableConflicts(sourceChanges, targetChanges));

        // Check for conflicting column operations
        conflicts.AddRange(DetectColumnConflicts(sourceChanges, targetChanges));

        // Check for conflicting index operations
        conflicts.AddRange(DetectIndexConflicts(sourceChanges, targetChanges));

        // Check for naming conflicts
        conflicts.AddRange(DetectNamingConflicts(sourceChanges, targetChanges));

        return conflicts;
    }

    /// <summary>
    /// Detects table-related conflicts.
    /// </summary>
    private List<ConflictInfo> DetectTableConflicts(List<SchemaChange> sourceChanges, List<SchemaChange> targetChanges)
    {
        var conflicts = new List<ConflictInfo>();

        var sourceTableOps = sourceChanges.Where(c => c.ChangeType is SqlChangeType.CreateTable or
                                                      SqlChangeType.DropTable or
                                                      SqlChangeType.AlterTable).ToList();
        var targetTableOps = targetChanges.Where(c => c.ChangeType is SqlChangeType.CreateTable or
                                                      SqlChangeType.DropTable or
                                                      SqlChangeType.AlterTable).ToList();

        foreach (var sourceOp in sourceTableOps)
        {
            var conflictingOps = targetTableOps
                .Where(t => t.TableName == sourceOp.TableName && OperationsConflict(sourceOp.ChangeType, t.ChangeType))
                .ToList();

            foreach (var targetOp in conflictingOps)
            {
                var conflict = new ConflictInfo(sourceOp.MigrationId, targetOp.MigrationId, ConflictType.TableConflict)
                {
                    Description = $"Table [{sourceOp.TableName}] has conflicting operations: {sourceOp.ChangeType} vs {targetOp.ChangeType}",
                    Severity = ConflictSeverity.Error
                };
                conflict.AddAffectedElement(sourceOp.TableName);
                conflict.AddDetail("SourceOperation", sourceOp.ChangeType.ToString());
                conflict.AddDetail("TargetOperation", targetOp.ChangeType.ToString());
                conflicts.Add(conflict);
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Detects column-related conflicts.
    /// </summary>
    private List<ConflictInfo> DetectColumnConflicts(List<SchemaChange> sourceChanges, List<SchemaChange> targetChanges)
    {
        var conflicts = new List<ConflictInfo>();

        var sourceColumnOps = sourceChanges.Where(c => c.ChangeType is SqlChangeType.AddColumn or
                                                       SqlChangeType.DropColumn or
                                                       SqlChangeType.ModifyColumn).ToList();
        var targetColumnOps = targetChanges.Where(c => c.ChangeType is SqlChangeType.AddColumn or
                                                       SqlChangeType.DropColumn or
                                                       SqlChangeType.ModifyColumn).ToList();

        foreach (var sourceOp in sourceColumnOps)
        {
            var conflictingOps = targetColumnOps
                .Where(t => t.TableName == sourceOp.TableName &&
                           t.ColumnName == sourceOp.ColumnName &&
                           OperationsConflict(sourceOp.ChangeType, t.ChangeType))
                .ToList();

            foreach (var targetOp in conflictingOps)
            {
                var conflict = new ConflictInfo(sourceOp.MigrationId, targetOp.MigrationId, ConflictType.ColumnConflict)
                {
                    Description = $"Column [{sourceOp.TableName}].[{sourceOp.ColumnName}] has conflicting operations",
                    Severity = ConflictSeverity.Error
                };
                conflict.AddAffectedElement($"{sourceOp.TableName}.{sourceOp.ColumnName}");
                conflicts.Add(conflict);
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Detects index-related conflicts.
    /// </summary>
    private List<ConflictInfo> DetectIndexConflicts(List<SchemaChange> sourceChanges, List<SchemaChange> targetChanges)
    {
        var conflicts = new List<ConflictInfo>();

        var sourceIndexOps = sourceChanges.Where(c => c.ChangeType is SqlChangeType.CreateIndex or
                                                      SqlChangeType.DropIndex).ToList();
        var targetIndexOps = targetChanges.Where(c => c.ChangeType is SqlChangeType.CreateIndex or
                                                      SqlChangeType.DropIndex).ToList();

        foreach (var sourceOp in sourceIndexOps)
        {
            var indexName = sourceOp.GetMetadata("IndexName")?.ToString() ?? "";
            var conflictingOps = targetIndexOps
                .Where(t => t.TableName == sourceOp.TableName &&
                           (t.GetMetadata("IndexName")?.ToString() ?? "") == indexName)
                .ToList();

            foreach (var targetOp in conflictingOps)
            {
                if (OperationsConflict(sourceOp.ChangeType, targetOp.ChangeType))
                {
                    var conflict = new ConflictInfo(sourceOp.MigrationId, targetOp.MigrationId, ConflictType.IndexConflict)
                    {
                        Description = $"Index on [{sourceOp.TableName}] has conflicting operations",
                        Severity = ConflictSeverity.Warning
                    };
                    conflicts.Add(conflict);
                }
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Detects naming conflicts (e.g., same name used for different objects).
    /// </summary>
    private List<ConflictInfo> DetectNamingConflicts(List<SchemaChange> sourceChanges, List<SchemaChange> targetChanges)
    {
        var conflicts = new List<ConflictInfo>();

        var sourceTableNames = sourceChanges.Where(c => c.ChangeType == SqlChangeType.CreateTable)
                                           .Select(c => c.TableName)
                                           .ToHashSet();

        var targetTableNames = targetChanges.Where(c => c.ChangeType == SqlChangeType.CreateTable)
                                           .Select(c => c.TableName)
                                           .ToHashSet();

        var duplicateNames = sourceTableNames.Intersect(targetTableNames).ToList();

        foreach (var sourceName in duplicateNames)
        {
            var sourceChange = sourceChanges.FirstOrDefault(c => c.TableName == sourceName && c.ChangeType == SqlChangeType.CreateTable);
            var targetChange = targetChanges.FirstOrDefault(c => c.TableName == sourceName && c.ChangeType == SqlChangeType.CreateTable);

            if (sourceChange != null && targetChange != null && sourceChange.Sql != targetChange.Sql)
            {
                var conflict = new ConflictInfo(sourceChange.MigrationId, targetChange.MigrationId, ConflictType.NameConflict)
                {
                    Description = $"Table [{sourceName}] is created with different schema definitions",
                    Severity = ConflictSeverity.Critical
                };
                conflict.AddAffectedElement(sourceName);
                conflicts.Add(conflict);
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Checks if two operations conflict with each other.
    /// </summary>
    private bool OperationsConflict(SqlChangeType operation1, SqlChangeType operation2)
    {
        // Drop and Create of same object conflict
        if ((operation1 == SqlChangeType.DropTable && operation2 == SqlChangeType.CreateTable) ||
            (operation1 == SqlChangeType.CreateTable && operation2 == SqlChangeType.DropTable))
            return true;

        // Drop and Modify conflict
        if ((operation1 == SqlChangeType.DropTable && operation2 == SqlChangeType.AlterTable) ||
            (operation1 == SqlChangeType.AlterTable && operation2 == SqlChangeType.DropTable))
            return true;

        // Drop and Drop is harmless (idempotent)
        if (operation1 == SqlChangeType.DropTable && operation2 == SqlChangeType.DropTable)
            return false;

        // Create and Create of same object conflicts
        if (operation1 == SqlChangeType.CreateTable && operation2 == SqlChangeType.CreateTable)
            return true;

        return false;
    }

    /// <summary>
    /// Gets severity level based on change type.
    /// </summary>
    private ConflictSeverity GetSeverityForChangeType(SqlChangeType changeType)
    {
        return changeType switch
        {
            SqlChangeType.DropTable or SqlChangeType.DropColumn => ConflictSeverity.Critical,
            SqlChangeType.ModifyColumn => ConflictSeverity.Error,
            SqlChangeType.CreateIndex or SqlChangeType.DropIndex => ConflictSeverity.Warning,
            _ => ConflictSeverity.Error
        };
    }

    /// <summary>
    /// Validates if the table name length is within EF limits.
    /// </summary>
    public bool IsValidTableName(string tableName)
    {
        return !string.IsNullOrWhiteSpace(tableName) && tableName.Length <= MaxTableNameLength;
    }

    /// <summary>
    /// Validates if the column name length is within EF limits.
    /// </summary>
    public bool IsValidColumnName(string columnName)
    {
        return !string.IsNullOrWhiteSpace(columnName) && columnName.Length <= MaxColumnNameLength;
    }
}
