#nullable enable
using EfMigrationDiff.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace EfMigrationDiff.Services;

/// <summary>
/// Service for detecting conflicts between migration schema changes.
/// </summary>
public class ConflictDetectionService
{
    private const int MaxTableNameLength = 128;
    private const int MaxColumnNameLength = 128;
    private readonly ILogger<ConflictDetectionService> _logger;

    public ConflictDetectionService(ILogger<ConflictDetectionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detects conflicts between two sets of schema changes by analyzing table, column,
    /// index, rename, and naming conflicts. Each conflict is returned with severity and metadata
    /// describing the nature of the incompatibility.
    /// </summary>
    /// <param name="sourceChanges">Schema changes from the source (base) branch.</param>
    /// <param name="targetChanges">Schema changes from the target (feature) branch.</param>
    /// <returns>
    /// A list of <see cref="ConflictInfo"/> objects describing each detected conflict,
    /// including type, severity, affected elements, and resolution metadata.
    /// Returns an empty list when no conflicts are found.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when either sourceChanges or targetChanges is null.</exception>
    public List<ConflictInfo> DetectConflicts(List<SchemaChange> sourceChanges, List<SchemaChange> targetChanges)
    {
        ArgumentNullException.ThrowIfNull(sourceChanges);
        ArgumentNullException.ThrowIfNull(targetChanges);

        _logger.LogInformation("DetectConflicts called with {SourceChangeCount} source changes and {TargetChangeCount} target changes", sourceChanges.Count, targetChanges.Count);

        var conflicts = new List<ConflictInfo>();

        // Check for rename vs modify conflicts (high priority)
        conflicts.AddRange(DetectRenameVsModifyConflicts(sourceChanges, targetChanges));

        // Check for operation order conflicts (reordered migrations)
        conflicts.AddRange(DetectOperationOrderConflicts(sourceChanges, targetChanges));

        // Check for conflicting table operations
        conflicts.AddRange(DetectTableConflicts(sourceChanges, targetChanges));

        // Check for conflicting column operations
        conflicts.AddRange(DetectColumnConflicts(sourceChanges, targetChanges));

        // Check for conflicting index operations
        conflicts.AddRange(DetectIndexConflicts(sourceChanges, targetChanges));

        // Check for index definition conflicts
        conflicts.AddRange(DetectIndexDefinitionConflicts(sourceChanges, targetChanges));

        // Check for naming conflicts
        conflicts.AddRange(DetectNamingConflicts(sourceChanges, targetChanges));
        
        if (conflicts.Count > 0)
        {
            _logger.LogWarning("Detected {ConflictCount} conflicts during schema analysis", conflicts.Count);
        }
        else
        {
            _logger.LogDebug("No conflicts detected during schema analysis");
        }

        _logger.LogInformation("DetectConflicts finished, returning {ConflictCount} conflicts", conflicts.Count);
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
                                                      SqlChangeType.AlterTable or
            SqlChangeType.Rename).ToList();
        var targetTableOps = targetChanges.Where(c => c.ChangeType is SqlChangeType.CreateTable or
                                                      SqlChangeType.DropTable or
                                                      SqlChangeType.AlterTable or
            SqlChangeType.Rename).ToList();

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
                           (OperationsConflict(sourceOp.ChangeType, t.ChangeType) ||
                            (sourceOp.ChangeType == SqlChangeType.ModifyColumn &&
                             t.ChangeType == SqlChangeType.ModifyColumn &&
                             sourceOp.DefaultValue != t.DefaultValue)))
                .ToList();

            foreach (var targetOp in conflictingOps)
            {
                var conflict = new ConflictInfo(sourceOp.MigrationId, targetOp.MigrationId, ConflictType.ColumnConflict)
                {
                    Description = $"Column [{sourceOp.TableName}].[{sourceOp.ColumnName}] has conflicting operations",
                    Severity = ConflictSeverity.Error
                };
                conflict.AddAffectedElement($"{sourceOp.TableName}.{sourceOp.ColumnName}");
                conflict.AddDetail("SourceOperation", sourceOp.ChangeType.ToString());
                conflict.AddDetail("TargetOperation", targetOp.ChangeType.ToString());
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

            if (sourceChange is not null && targetChange is not null && sourceChange.Sql != targetChange.Sql)
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
    /// Detects conflicts where a column is renamed in one branch and modified in another.
    /// This is a critical conflict because the rename operation hides the target column,
    /// making the modify operation fail or target the wrong column.
    /// </summary>
    private List<ConflictInfo> DetectRenameVsModifyConflicts(List<SchemaChange> sourceChanges, List<SchemaChange> targetChanges)
    {
        var conflicts = new List<ConflictInfo>();

        var sourceRenameOps = sourceChanges.Where(c => c.ChangeType == SqlChangeType.Rename && c.OldValue != null).ToList();
        var targetModifyOps = targetChanges.Where(c => c.ChangeType == SqlChangeType.ModifyColumn).ToList();

        foreach (var renameOp in sourceRenameOps)
        {
            // Check if the rename target is being modified in the other branch
            var targetColumnName = renameOp.NewValue;
            var conflictingModifies = targetModifyOps
                .Where(m => m.TableName == renameOp.TableName && m.ColumnName == targetColumnName)
                .ToList();

            foreach (var modifyOp in conflictingModifies)
            {
                var conflict = new ConflictInfo(renameOp.MigrationId, modifyOp.MigrationId, ConflictType.ColumnConflict)
                {
                    Description = $"Column [{renameOp.TableName}].[{targetColumnName}] is renamed in one branch and modified in another. The rename hides the target, making the modify operation fail.",
                    Severity = ConflictSeverity.Critical
                };
                conflict.AddAffectedElement($"{renameOp.TableName}.{targetColumnName}");
                conflict.AddDetail("SourceOperation", $"RENAME {renameOp.OldValue} TO {targetColumnName}");
                conflict.AddDetail("TargetOperation", $"MODIFY COLUMN {targetColumnName}");
                conflict.AddDetail("ConflictReason", "Rename hides the target column, making modification fail");
                conflicts.Add(conflict);
            }

            // Also check if the rename source (old column name) is being modified in the other branch
            var sourceColumnName = renameOp.OldValue;
            conflictingModifies = targetModifyOps
                .Where(m => m.TableName == renameOp.TableName && m.ColumnName == sourceColumnName)
                .ToList();

            foreach (var modifyOp in conflictingModifies)
            {
                var conflict = new ConflictInfo(renameOp.MigrationId, modifyOp.MigrationId, ConflictType.ColumnConflict)
                {
                    Description = $"Column [{renameOp.TableName}].[{sourceColumnName}] is renamed in one branch while being modified in another. The rename changes the column identity, causing conflicts.",
                    Severity = ConflictSeverity.Critical
                };
                conflict.AddAffectedElement($"{renameOp.TableName}.{sourceColumnName}");
                conflict.AddDetail("SourceOperation", $"RENAME {sourceColumnName} TO {targetColumnName}");
                conflict.AddDetail("TargetOperation", $"MODIFY COLUMN {sourceColumnName}");
                conflict.AddDetail("ConflictReason", "Rename changes column identity while it's being modified");
                conflicts.Add(conflict);
            }
        }

        var targetRenameOps = targetChanges.Where(c => c.ChangeType == SqlChangeType.Rename && c.OldValue != null).ToList();
        var sourceModifyOps = sourceChanges.Where(c => c.ChangeType == SqlChangeType.ModifyColumn).ToList();

        foreach (var renameOp in targetRenameOps)
        {
            // Check if the rename target is being modified in the source branch
            var targetColumnName = renameOp.NewValue;
            var conflictingModifies = sourceModifyOps
                .Where(m => m.TableName == renameOp.TableName && m.ColumnName == targetColumnName)
                .ToList();

            foreach (var modifyOp in conflictingModifies)
            {
                var conflict = new ConflictInfo(renameOp.MigrationId, modifyOp.MigrationId, ConflictType.ColumnConflict)
                {
                    Description = $"Column [{renameOp.TableName}].[{targetColumnName}] is renamed in one branch and modified in another. The rename hides the target, making the modify operation fail.",
                    Severity = ConflictSeverity.Critical
                };
                conflict.AddAffectedElement($"{renameOp.TableName}.{targetColumnName}");
                conflict.AddDetail("SourceOperation", $"MODIFY COLUMN {targetColumnName}");
                conflict.AddDetail("TargetOperation", $"RENAME {renameOp.OldValue} TO {targetColumnName}");
                conflict.AddDetail("ConflictReason", "Rename hides the target column, making modification fail");
                conflicts.Add(conflict);
            }

            // Also check if the rename source (old column name) is being modified in the source branch
            var sourceColumnName = renameOp.OldValue;
            conflictingModifies = sourceModifyOps
                .Where(m => m.TableName == renameOp.TableName && m.ColumnName == sourceColumnName)
                .ToList();

            foreach (var modifyOp in conflictingModifies)
            {
                var conflict = new ConflictInfo(renameOp.MigrationId, modifyOp.MigrationId, ConflictType.ColumnConflict)
                {
                    Description = $"Column [{renameOp.TableName}].[{sourceColumnName}] is renamed in one branch while being modified in another. The rename changes the column identity, causing conflicts.",
                    Severity = ConflictSeverity.Critical
                };
                conflict.AddAffectedElement($"{renameOp.TableName}.{sourceColumnName}");
                conflict.AddDetail("SourceOperation", $"MODIFY COLUMN {sourceColumnName}");
                conflict.AddDetail("TargetOperation", $"RENAME {sourceColumnName} TO {targetColumnName}");
                conflict.AddDetail("ConflictReason", "Rename changes column identity while it's being modified");
                conflicts.Add(conflict);
            }
        }

        if (conflicts.Count > 0)
        {
            _logger.LogWarning("Detected {ConflictCount} rename vs modify conflicts", conflicts.Count);
        }

        return conflicts;
    }

    /// <summary>
    /// Detects conflicts where the same migration operations are applied in different order.
    /// Identical operations in different order should NOT conflict as they produce the same final state.
    /// However, if the operations are semantically different (e.g., different SQL), they should conflict.
    /// </summary>
    private List<ConflictInfo> DetectOperationOrderConflicts(List<SchemaChange> sourceChanges, List<SchemaChange> targetChanges)
    {
        var conflicts = new List<ConflictInfo>();

        // Group changes by their semantic identity (table, column, operation type)
        var sourceGroups = sourceChanges
            .Where(c => c.ChangeType != SqlChangeType.Rename) // Rename operations are order-dependent
            .GroupBy(c => new { c.TableName, c.ColumnName, c.ChangeType, c.Sql })
            .Where(g => !string.IsNullOrEmpty(g.Key.TableName))
            .ToList();

        var targetGroups = targetChanges
            .Where(c => c.ChangeType != SqlChangeType.Rename)
            .GroupBy(c => new { c.TableName, c.ColumnName, c.ChangeType, c.Sql })
            .Where(g => !string.IsNullOrEmpty(g.Key.TableName))
            .ToList();

        // Find groups that exist in both branches but might be in different order
        foreach (var sourceGroup in sourceGroups)
        {
            var matchingTargetGroup = targetGroups.FirstOrDefault(tg =>
                tg.Key.TableName == sourceGroup.Key.TableName &&
                tg.Key.ColumnName == sourceGroup.Key.ColumnName &&
                tg.Key.ChangeType == sourceGroup.Key.ChangeType &&
                tg.Key.Sql == sourceGroup.Key.Sql);

            if (matchingTargetGroup != null)
            {
                // Check if the operations are semantically identical
                if (sourceGroup.Count() == matchingTargetGroup.Count() &&
                    sourceGroup.Key.ChangeType != SqlChangeType.Rename)
                {
                    // Identical operations in different order - NOT a conflict
                    continue;
                }
            }
        }

        // Check for destructive operations that change order
        // Drop operations followed by Create operations in different order can be problematic
        var sourceDrops = sourceChanges.Where(c => c.ChangeType == SqlChangeType.DropTable || c.ChangeType == SqlChangeType.DropColumn).ToList();
        var targetDrops = targetChanges.Where(c => c.ChangeType == SqlChangeType.DropTable || c.ChangeType == SqlChangeType.DropColumn).ToList();

        foreach (var sourceDrop in sourceDrops)
        {
            var matchingTargetDrop = targetDrops.FirstOrDefault(t =>
                t.TableName == sourceDrop.TableName &&
                t.ColumnName == sourceDrop.ColumnName &&
                t.ChangeType == sourceDrop.ChangeType);

            if (matchingTargetDrop != null)
            {
                // Both branches drop the same object - this is idempotent, not a conflict
                continue;
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Detects conflicts where the same index name is used with different definitions.
    /// </summary>
    private List<ConflictInfo> DetectIndexDefinitionConflicts(List<SchemaChange> sourceChanges, List<SchemaChange> targetChanges)
    {
        var conflicts = new List<ConflictInfo>();

        var sourceIndexOps = sourceChanges.Where(c => c.ChangeType == SqlChangeType.CreateIndex).ToList();
        var targetIndexOps = targetChanges.Where(c => c.ChangeType == SqlChangeType.CreateIndex).ToList();

        foreach (var sourceIndex in sourceIndexOps)
        {
            var indexName = sourceIndex.GetMetadata("IndexName")?.ToString() ?? "";
            var targetIndex = targetIndexOps.FirstOrDefault(t =>
                (t.GetMetadata("IndexName")?.ToString() ?? "") == indexName &&
                t.TableName == sourceIndex.TableName);

            if (targetIndex != null)
            {
                // Same index name exists in both branches - check if definitions differ
                if (sourceIndex.Sql != targetIndex.Sql)
                {
                    var conflict = new ConflictInfo(sourceIndex.MigrationId, targetIndex.MigrationId, ConflictType.IndexConflict)
                    {
                        Description = $"Index [{sourceIndex.TableName}].[{indexName}] has different definitions in each branch",
                        Severity = ConflictSeverity.Error
                    };
                    conflict.AddAffectedElement($"{sourceIndex.TableName}.{indexName}");
                    conflict.AddDetail("SourceDefinition", sourceIndex.Sql);
                    conflict.AddDetail("TargetDefinition", targetIndex.Sql);
                    conflict.AddDetail("ConflictReason", "Index definitions differ");
                    conflicts.Add(conflict);
                }
            }
        }

        if (conflicts.Count > 0)
        {
            _logger.LogWarning("Detected {ConflictCount} index definition conflicts", conflicts.Count);
        }

        return conflicts;
    }

    /// <summary>
    /// Checks if two operations conflict with each other.
    /// </summary>
    private bool OperationsConflict(SqlChangeType operation1, SqlChangeType operation2)
    {
        // Drop and Create of same object conflict (Tables)
        if ((operation1 == SqlChangeType.DropTable && operation2 == SqlChangeType.CreateTable) ||
            (operation1 == SqlChangeType.CreateTable && operation2 == SqlChangeType.DropTable))
            return true;

        // Drop and Modify conflict (Tables)
        if ((operation1 == SqlChangeType.DropTable && operation2 == SqlChangeType.AlterTable) ||
            (operation1 == SqlChangeType.AlterTable && operation2 == SqlChangeType.DropTable))
            return true;

        // Drop and Add conflict (Columns)
        if ((operation1 == SqlChangeType.DropColumn && operation2 == SqlChangeType.AddColumn) ||
            (operation1 == SqlChangeType.AddColumn && operation2 == SqlChangeType.DropColumn))
            return true;

        // Create and Drop conflict (Indexes)
        if ((operation1 == SqlChangeType.DropIndex && operation2 == SqlChangeType.CreateIndex) ||
            (operation1 == SqlChangeType.CreateIndex && operation2 == SqlChangeType.DropIndex))
            return true;

        // Drop and Drop is harmless (idempotent)
        if (operation1 == SqlChangeType.DropTable && operation2 == SqlChangeType.DropTable)
            return false;

        // Create and Create of same object conflicts
        if (operation1 == SqlChangeType.CreateTable && operation2 == SqlChangeType.CreateTable)
            return true;

        // Rename operations conflict with any other operation on the same table
        // because rename changes the object identity
        if (operation1 == SqlChangeType.Rename || operation2 == SqlChangeType.Rename)
        {
            return true; // Any rename conflicts with any other operation
        }

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
    /// Validates if the table name length is within Entity Framework identifier limits.
    /// Checks for null/whitespace and enforces the maximum length of 128 characters,
    /// which corresponds to SQL Server's identifier length constraint.
    /// </summary>
    /// <param name="tableName">The table name to validate.</param>
    /// <returns><c>true</c> if the name is non-empty and within the allowed length; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when tableName is null or whitespace.</exception>
    public bool IsValidTableName(string tableName)
    {
        return !string.IsNullOrWhiteSpace(tableName) && tableName.Length <= MaxTableNameLength;
    }

    /// <summary>
    /// Validates if the column name length is within Entity Framework identifier limits.
    /// Enforces the maximum length of 128 characters for column identifiers.
    /// </summary>
    /// <param name="columnName">The column name to validate.</param>
    /// <returns><c>true</c> if the name is non-empty and within the allowed length; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when columnName is null or whitespace.</exception>
    public bool IsValidColumnName(string columnName)
    {
        return !string.IsNullOrWhiteSpace(columnName) && columnName.Length <= MaxColumnNameLength;
    }
}
