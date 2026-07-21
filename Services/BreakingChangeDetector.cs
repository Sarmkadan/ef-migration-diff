#nullable enable

using EfMigrationDiff.Models;
using Microsoft.Extensions.Logging;

namespace EfMigrationDiff.Services;

/// <summary>
/// Service that classifies schema changes as Breaking vs Safe based on the schema diff model.
/// </summary>
/// <remarks>
/// This service analyzes SchemaChange objects and determines whether each change is:
/// - Breaking: Changes that break backward compatibility (e.g., dropped columns/tables, narrowed types)
/// - Safe: Changes that maintain backward compatibility
/// </remarks>
public class BreakingChangeDetector
{
    private readonly ILogger<BreakingChangeDetector> _logger;

    public BreakingChangeDetector(ILogger<BreakingChangeDetector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Classifies schema changes from a diff result as Breaking vs Safe.
    /// </summary>
    /// <param name="changes">List of schema changes to classify</param>
    /// <returns>List of classified changes with severity and reason</returns>
    public List<BreakingChangeClassification> ClassifyChanges(IReadOnlyList<SchemaChange> changes)
    {
        _logger.LogInformation("Classifying {ChangeCount} schema changes for breaking changes", changes.Count);

        var classifications = new List<BreakingChangeClassification>(changes.Count);

        foreach (var change in changes)
        {
            var classification = ClassifyChange(change);
            classifications.Add(classification);
        }

        return classifications;
    }

    /// <summary>
    /// Classifies a single schema change as Breaking vs Safe.
    /// </summary>
    /// <param name="change">Schema change to classify</param>
    /// <returns>Classification result with severity and reason</returns>
    public BreakingChangeClassification ClassifyChange(SchemaChange change)
    {
        // Check for destructive changes (always breaking)
        if (change.IsDestructive())
        {
            return new BreakingChangeClassification(
                change,
                BreakingChangeSeverity.Breaking,
                "destructive operation: " + GetDestructiveReason(change)
            );
        }

        // Check for type narrowing (always breaking)
        if (change.ChangeType == SqlChangeType.ModifyColumn)
        {
            var narrowingReason = CheckTypeNarrowing(change);
            if (narrowingReason != null)
            {
                return new BreakingChangeClassification(
                    change,
                    BreakingChangeSeverity.Breaking,
                    narrowingReason
                );
            }
        }

        // Check for adding non-nullable column without default (breaking)
        if (change.ChangeType == SqlChangeType.AddColumn)
        {
            var nonNullableReason = CheckNonNullableColumn(change);
            if (nonNullableReason != null)
            {
                return new BreakingChangeClassification(
                    change,
                    BreakingChangeSeverity.Breaking,
                    nonNullableReason
                );
            }
        }

        // Check for dropping primary key or unique constraint (breaking)
        if (change.ChangeType == SqlChangeType.DropForeignKey ||
            change.ChangeType == SqlChangeType.DropIndex)
        {
            return new BreakingChangeClassification(
                change,
                BreakingChangeSeverity.Breaking,
                "removes constraint or index that may be depended upon by application code"
            );
        }

        // Check for adding nullable column (safe)
        if (change.ChangeType == SqlChangeType.AddColumn)
        {
            var nullableReason = CheckNullableColumn(change);
            if (nullableReason != null)
            {
                return new BreakingChangeClassification(
                    change,
                    BreakingChangeSeverity.Safe,
                    nullableReason
                );
            }
        }

        // Check for renaming objects (potential warning - affects references)
        if (change.ChangeType == SqlChangeType.Rename)
        {
            return new BreakingChangeClassification(
                change,
                BreakingChangeSeverity.Warning,
                "object renamed - may affect application code that references old name"
            );
        }

        // Check for adding foreign key (warning - affects relationships)
        if (change.ChangeType == SqlChangeType.AddForeignKey)
        {
            return new BreakingChangeClassification(
                change,
                BreakingChangeSeverity.Warning,
                "foreign key added - may affect data integrity constraints"
            );
        }

        // Safe changes
        return new BreakingChangeClassification(
            change,
            BreakingChangeSeverity.Safe,
            "backward compatible change"
        );
    }

    /// <summary>
    /// Gets the breaking change classification for a diff result.
    /// </summary>
    /// <param name="diffResult">Schema diff result to analyze</param>
    /// <returns>Classification summary with counts and details</returns>
    public BreakingChangeSummary ClassifyDiffResult(SchemaDiffResult diffResult)
    {
        var allChanges = new List<SchemaChange>();
        allChanges.AddRange(diffResult.SourceOnlyChanges);
        allChanges.AddRange(diffResult.TargetOnlyChanges);
        allChanges.AddRange(diffResult.ModifiedChanges);

        var classifications = ClassifyChanges(allChanges);

        var breaking = classifications.Count(c => c.Severity == BreakingChangeSeverity.Breaking);
        var safe = classifications.Count(c => c.Severity == BreakingChangeSeverity.Safe);
        var warnings = classifications.Count(c => c.Severity == BreakingChangeSeverity.Warning);

        return new BreakingChangeSummary(
            TotalChanges: allChanges.Count,
            BreakingChanges: breaking,
            SafeChanges: safe,
            WarningChanges: warnings,
            Classifications: classifications,
            HasBreakingChanges: breaking > 0,
            IsSafe: breaking == 0 && warnings == 0
        );
    }

    /// <summary>
    /// Checks if a column modification is a type narrowing (breaking change).
    /// </summary>
    private string? CheckTypeNarrowing(SchemaChange change)
    {
        // Check metadata for type changes
        if (change.Metadata.TryGetValue("OldType", out var oldTypeObj) &&
            change.Metadata.TryGetValue("NewType", out var newTypeObj))
        {
            var oldType = oldTypeObj?.ToString();
            var newType = newTypeObj?.ToString();

            if (!string.IsNullOrEmpty(oldType) && !string.IsNullOrEmpty(newType))
            {
                // Common narrowing patterns
                if (IsNarrowingType(oldType, newType))
                {
                    return $"column type narrowed from {oldType} to {newType}";
                }
            }
        }

        // Check for precision/scale reductions
        if (change.Metadata.TryGetValue("OldPrecision", out var oldPrecisionObj) &&
            change.Metadata.TryGetValue("NewPrecision", out var newPrecisionObj))
        {
            if (int.TryParse(oldPrecisionObj?.ToString(), out var oldPrecision) &&
                int.TryParse(newPrecisionObj?.ToString(), out var newPrecision) &&
                newPrecision < oldPrecision)
            {
                return $"column precision reduced from {oldPrecision} to {newPrecision}";
            }
        }

        if (change.Metadata.TryGetValue("OldScale", out var oldScaleObj) &&
            change.Metadata.TryGetValue("NewScale", out var newScaleObj))
        {
            if (int.TryParse(oldScaleObj?.ToString(), out var oldScale) &&
                int.TryParse(newScaleObj?.ToString(), out var newScale) &&
                newScale < oldScale)
            {
                return $"column scale reduced from {oldScale} to {newScale}";
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a new column is non-nullable without a default value (breaking change).
    /// </summary>
    private string? CheckNonNullableColumn(SchemaChange change)
    {
        // Check if column is explicitly non-nullable
        if (change.GetMetadata("Nullable") is string nullable &&
            nullable.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            // Check if there's a default value
            if (change.DefaultValue == null || string.IsNullOrWhiteSpace(change.DefaultValue.ToString()))
            {
                return $"added non-nullable column '{change.ColumnName}' without default value";
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a new column is nullable (safe change).
    /// </summary>
    private string? CheckNullableColumn(SchemaChange change)
    {
        // Check if column is explicitly nullable
        if (change.GetMetadata("Nullable") is string nullable &&
            nullable.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return $"added nullable column '{change.ColumnName}' - backward compatible";
        }

        return null;
    }

    /// <summary>
    /// Determines if one type is a narrowing of another.
    /// </summary>
    private bool IsNarrowingType(string oldType, string newType)
    {
        // Remove whitespace and normalize
        oldType = oldType.Replace(" ", "").ToLowerInvariant();
        newType = newType.Replace(" ", "").ToLowerInvariant();

        // Common narrowing patterns
        return (oldType, newType) switch
        {
            ("nvarchar(max)", "nvarchar") => true,
            ("varchar(max)", "varchar") => true,
            ("nvarchar", "nvarchar(255)") => true,
            ("varchar", "varchar(255)") => true,
            ("decimal(18,2)", "decimal(10,2)") => true,
            ("decimal(19,4)", "decimal(18,2)") => true,
            ("int", "smallint") => true,
            ("bigint", "int") => true,
            ("datetime2", "datetime") => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets a human-readable reason for destructive changes.
    /// </summary>
    private string GetDestructiveReason(SchemaChange change)
    {
        return change.ChangeType switch
        {
            SqlChangeType.DropTable => $"table '{change.TableName}' is dropped",
            SqlChangeType.DropColumn => $"column '{change.ColumnName}' is dropped from table '{change.TableName}'",
            SqlChangeType.DropIndex => $"index on table '{change.TableName}' is dropped",
            SqlChangeType.DropForeignKey => $"foreign key on table '{change.TableName}' is dropped",
            SqlChangeType.DropProcedure => "stored procedure is dropped",
            SqlChangeType.DropView => "view is dropped",
            _ => "destructive operation"
        };
    }
}

/// <summary>
/// Represents the severity of a breaking change classification.
/// </summary>
public enum BreakingChangeSeverity
{
    /// <summary>Change is backward compatible and safe to apply</summary>
    Safe = 0,

    /// <summary>Change may cause issues but is not strictly breaking</summary>
    Warning = 1,

    /// <summary>Change breaks backward compatibility and may break application code</summary>
    Breaking = 2
}

/// <summary>
/// Represents a classification of a schema change as Breaking vs Safe.
/// </summary>
/// <param name="Change">The schema change that was classified</param>
/// <param name="Severity">The severity level (Safe/Breaking/Warning)</param>
/// <param name="Reason">Human-readable reason for the classification</param>
public sealed record BreakingChangeClassification(
    SchemaChange Change,
    BreakingChangeSeverity Severity,
    string Reason
);

/// <summary>
/// Summary of breaking change analysis for a schema diff result.
/// </summary>
public sealed record BreakingChangeSummary(
    int TotalChanges,
    int BreakingChanges,
    int SafeChanges,
    int WarningChanges,
    IReadOnlyList<BreakingChangeClassification> Classifications,
    bool HasBreakingChanges,
    bool IsSafe
);
