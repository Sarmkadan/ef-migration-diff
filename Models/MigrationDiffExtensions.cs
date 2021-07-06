#nullable enable

using EfMigrationDiff.Models;

namespace EfMigrationDiff.Models;

/// <summary>
/// Provides useful extension methods for <see cref="MigrationDiff"/> class.
/// </summary>
public static class MigrationDiffExtensions
{
    /// <summary>
    /// Gets the total number of migrations across both branches.
    /// </summary>
    /// <param name="diff">The migration diff instance.</param>
    /// <returns>The total count of all migrations.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="diff"/> is null.</exception>
    public static int GetTotalMigrations(this MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        return diff.OnlyInSource.Count +
               diff.OnlyInTarget.Count +
               diff.InBoth.Count;
    }

    /// <summary>
    /// Gets the number of migrations that need attention (source-only + target-only).
    /// </summary>
    /// <param name="diff">The migration diff instance.</param>
    /// <returns>The count of migrations that need attention.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="diff"/> is null.</exception>
    public static int GetMigrationsNeedingAttention(this MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        return diff.OnlyInSource.Count + diff.OnlyInTarget.Count;
    }

    /// <summary>
    /// Checks if the migration diff has any migrations that need attention.
    /// </summary>
    /// <param name="diff">The migration diff instance.</param>
    /// <returns>True if migrations need attention, otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="diff"/> is null.</exception>
    public static bool HasMigrationsNeedingAttention(this MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        return diff.GetMigrationsNeedingAttention() > 0;
    }

    /// <summary>
    /// Gets the percentage of common migrations relative to total migrations.
    /// </summary>
    /// <param name="diff">The migration diff instance.</param>
    /// <returns>The percentage (0-100) of common migrations, or 0 if no migrations exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="diff"/> is null.</exception>
    public static double GetCommonMigrationPercentage(this MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        var total = diff.GetTotalMigrations();
        if (total == 0)
        {
            return 0;
        }

        return (double)diff.InBoth.Count / total * 100;
    }

    /// <summary>
    /// Gets all schema changes from both branches as a single combined list.
    /// </summary>
    /// <param name="diff">The migration diff instance.</param>
    /// <returns>A combined list of all schema changes.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="diff"/> is null.</exception>
    public static List<SchemaChange> GetAllSchemaChanges(this MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        var allChanges = new List<SchemaChange>();
        allChanges.AddRange(diff.SourceSchemaChanges);
        allChanges.AddRange(diff.TargetSchemaChanges);
        return allChanges;
    }

    /// <summary>
    /// Gets the most recent migration timestamp from all migrations.
    /// </summary>
    /// <param name="diff">The migration diff instance.</param>
    /// <returns>The most recent timestamp string, or null if no migrations exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="diff"/> is null.</exception>
    public static string? GetMostRecentMigrationTimestamp(this MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        string? mostRecent = null;

        if (diff.OnlyInSource.Count > 0)
        {
            var sourceTimestamp = diff.OnlyInSource.Max(m => m.Timestamp);
            mostRecent = mostRecent is null
                ? sourceTimestamp
                : string.Compare(sourceTimestamp, mostRecent, StringComparison.Ordinal) > 0
                    ? sourceTimestamp
                    : mostRecent;
        }

        if (diff.OnlyInTarget.Count > 0)
        {
            var targetTimestamp = diff.OnlyInTarget.Max(m => m.Timestamp);
            mostRecent = mostRecent is null
                ? targetTimestamp
                : string.Compare(targetTimestamp, mostRecent, StringComparison.Ordinal) > 0
                    ? targetTimestamp
                    : mostRecent;
        }

        if (diff.InBoth.Count > 0)
        {
            var commonTimestamp = diff.InBoth.Max(m => m.Timestamp);
            mostRecent = mostRecent is null
                ? commonTimestamp
                : string.Compare(commonTimestamp, mostRecent, StringComparison.Ordinal) > 0
                    ? commonTimestamp
                    : mostRecent;
        }

        return mostRecent;
    }

    /// <summary>
    /// Checks if the migration diff has any destructive changes.
    /// </summary>
    /// <param name="diff">The migration diff instance.</param>
    /// <returns>True if destructive changes exist, otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="diff"/> is null.</exception>
    public static bool HasDestructiveChanges(this MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        return diff.GetDestructiveChanges().Count > 0;
    }

    /// <summary>
    /// Gets a summary of the diff as a formatted string.
    /// </summary>
    /// <param name="diff">The migration diff instance.</param>
    /// <returns>A formatted summary string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="diff"/> is null.</exception>
    public static string GetFormattedSummary(this MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        return $"""
Migration Diff Summary:
====================
Source Branch: {diff.SourceBranchId}
Target Branch: {diff.TargetBranchId}
Result: {diff.GetResultDescription()}

Migrations:
- Only in Source: {diff.OnlyInSource.Count}
- Only in Target: {diff.OnlyInTarget.Count}
- Common: {diff.InBoth.Count}
- Total: {diff.GetTotalMigrations()}

Schema Changes:
- Source: {diff.SourceSchemaChanges.Count}
- Target: {diff.TargetSchemaChanges.Count}
- Total: {diff.GetTotalSchemaChanges()}

Conflicts:
- Total: {diff.Conflicts.Count}
- Blocking: {diff.GetBlockingConflicts()}
- Destructive: {diff.GetDestructiveChanges().Count}

Common Migration Percentage: {diff.GetCommonMigrationPercentage():F2}%
Most Recent Migration: {diff.GetMostRecentMigrationTimestamp() ?? "N/A"}
""";
    }
}