#nullable enable

using System.Text;
using EfMigrationDiff.Models;

namespace EfMigrationDiff.Models;

/// <summary>
/// Provides useful extension methods for the Migration class to enhance functionality
/// and simplify common operations when working with EF Core migrations.
/// </summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Determines if this migration has any destructive schema changes that could
    /// potentially cause data loss or break existing functionality.
    /// </summary>
    /// <param name="migration">The migration to check</param>
    /// <returns>True if the migration contains destructive operations; otherwise false</returns>
    public static bool HasDestructiveChanges(this Migration migration)
    {
        if (migration.SchemaChanges == null || migration.SchemaChanges.Count == 0)
            return false;

        return migration.SchemaChanges.Any(change => change.IsDestructive());
    }

    /// <summary>
    /// Gets a summary of all schema changes in this migration as a formatted string.
    /// </summary>
    /// <param name="migration">The migration to analyze</param>
    /// <param name="includeDetails">Whether to include detailed information about each change</param>
    /// <returns>Formatted string containing change summary</returns>
    public static string GetSchemaChangesSummary(this Migration migration, bool includeDetails = false)
    {
        if (migration.SchemaChanges == null || migration.SchemaChanges.Count == 0)
            return "No schema changes detected.";

        var sb = new StringBuilder();
        sb.AppendLine($"Migration '{migration.Name}' contains {migration.SchemaChanges.Count} schema changes:");

        var changesByType = migration.SchemaChanges
            .GroupBy(change => change.ChangeType)
            .OrderByDescending(group => group.Count());

        foreach (var group in changesByType)
        {
            sb.AppendLine($"  - {group.Count()} {group.Key} operations");
        }

        if (includeDetails)
        {
            sb.AppendLine("\nDetailed changes:");
            foreach (var change in migration.SchemaChanges.OrderBy(c => c.LineNumber))
            {
                sb.AppendLine($"  Line {change.LineNumber}: {change.GetDescription()}");
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Checks if this migration conflicts with any other migration based on schema changes.
    /// </summary>
    /// <param name="migration">The migration to check for conflicts</param>
    /// <param name="otherMigrations">Collection of other migrations to compare against</param>
    /// <returns>List of detected conflicts; empty if no conflicts found</returns>
    public static List<ConflictInfo> FindConflictsWith(this Migration migration, IEnumerable<Migration> otherMigrations)
    {
        var conflicts = new List<ConflictInfo>();

        if (migration.SchemaChanges == null || migration.DetectedConflicts == null)
            return conflicts;

        foreach (var otherMigration in otherMigrations)
        {
            if (otherMigration.SchemaChanges == null || migration.Id == otherMigration.Id)
                continue;

            // Check for conflicts between schema changes
            foreach (var change1 in migration.SchemaChanges)
            {
                foreach (var change2 in otherMigration.SchemaChanges)
                {
                    if (change1.ConflictsWith(change2))
                    {
                        var conflict = new ConflictInfo(migration.Id, otherMigration.Id, ConflictType.OperationConflict)
                        {
                            Description = $"Conflicting operations: {change1.GetDescription()} vs {change2.GetDescription()}",
                            Severity = ConflictSeverity.Error
                        };
                        conflict.AddAffectedElement(change1.TableName);
                        conflict.AddAffectedElement(change2.TableName);
                        conflicts.Add(conflict);
                    }
                }
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Creates a new migration with updated content by applying a transformation function.
    /// </summary>
    /// <param name="migration">The original migration to transform</param>
    /// <param name="transform">Function that takes the original content and returns modified content</param>
    /// <returns>A new migration with transformed content</returns>
    public static Migration TransformContent(this Migration migration, Func<string, string> transform)
    {
        if (transform == null)
            throw new ArgumentNullException(nameof(transform));

        var newMigration = migration.Clone();
        newMigration.Content = transform(migration.Content);
        newMigration.MetadataContent = transform(migration.MetadataContent);

        // Update content size after transformation
        newMigration.Description = $"{migration.Description} (Transformed: {newMigration.GetContentSize()} bytes)";

        return newMigration;
    }
}