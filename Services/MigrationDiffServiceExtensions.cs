#nullable enable

using EfMigrationDiff.Models;
using Microsoft.Extensions.Logging;

namespace EfMigrationDiff.Services;

/// <summary>
/// Extension methods for <see cref="MigrationDiffService"/> providing additional functionality
/// for migration comparison and analysis.
/// </summary>
public static class MigrationDiffServiceExtensions
{
    /// <summary>
    /// Creates a quick comparison report that summarizes the differences between branches.
    /// This is a lightweight version of <see cref="MigrationDiffService.GenerateReport"/> that
    /// provides essential information without detailed migration listings.
    /// </summary>
    /// <param name="service">The migration diff service instance.</param>
    /// <param name="diff">The migration diff result to summarize.</param>
    /// <returns>A concise multi-line string with key comparison metrics.</returns>
    public static string GenerateQuickReport(this MigrationDiffService service, MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(diff);

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Quick Migration Diff Summary ===");
        report.AppendLine($"Branches: {diff.SourceBranchId} → {diff.TargetBranchId}");
        report.AppendLine($"Result: {diff.GetResultDescription()}");
        report.AppendLine($"Created: {diff.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        report.AppendLine("Migration Counts:");
        report.AppendLine($"  Source Only: {diff.OnlyInSource.Count}");
        report.AppendLine($"  Target Only: {diff.OnlyInTarget.Count}");
        report.AppendLine($"  Common: {diff.InBoth.Count}");
        report.AppendLine();

        report.AppendLine("Schema Changes:");
        report.AppendLine($"  Total: {diff.GetTotalSchemaChanges()}");
        report.AppendLine($"  Destructive: {diff.GetDestructiveChanges().Count}");
        report.AppendLine();

        report.AppendLine("Conflicts:");
        report.AppendLine($"  Total: {diff.Conflicts.Count}");
        report.AppendLine($"  Blocking: {diff.GetBlockingConflicts()}");
        report.AppendLine();

        report.AppendLine("Recommendation:");
        report.AppendLine(GetRecommendation(diff));

        return report.ToString();
    }

    /// <summary>
    /// Checks if the migration diff has any destructive changes that could cause data loss.
    /// Destructive changes include dropping tables, columns, indexes, or other operations
    /// that permanently remove data.
    /// </summary>
    /// <param name="service">The migration diff service instance.</param>
    /// <param name="diff">The migration diff result to check.</param>
    /// <returns>True if destructive changes exist, otherwise false.</returns>
    public static bool HasDestructiveChanges(this MigrationDiffService service, MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(diff);

        return diff.GetDestructiveChanges().Count > 0;
    }

    /// <summary>
    /// Gets a list of all migration names that exist in both branches.
    /// Useful for identifying which migrations are shared between branches.
    /// </summary>
    /// <param name="service">The migration diff service instance.</param>
    /// <param name="diff">The migration diff result to analyze.</param>
    /// <returns>A list of migration names present in both branches.</returns>
    public static List<string> GetCommonMigrationNames(this MigrationDiffService service, MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(diff);

        return diff.InBoth.Select(m => m.Name).ToList();
    }

    /// <summary>
    /// Creates a conflict summary report that lists all conflicts with their severity and type.
    /// Useful for CI/CD pipelines to determine if a merge should be blocked.
    /// </summary>
    /// <param name="service">The migration diff service instance.</param>
    /// <param name="diff">The migration diff result to analyze.</param>
    /// <returns>A formatted string containing conflict details.</returns>
    public static string GenerateConflictReport(this MigrationDiffService service, MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(diff);

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Conflict Analysis Report ===");
        report.AppendLine($"Total Conflicts: {diff.Conflicts.Count}");
        report.AppendLine($"Blocking Conflicts: {diff.GetBlockingConflicts()}");
        report.AppendLine();

        if (diff.Conflicts.Count == 0)
        {
            report.AppendLine("✓ No conflicts detected - safe to merge");
            return report.ToString();
        }

        report.AppendLine("Conflict Details:");
        foreach (var conflict in diff.Conflicts.OrderByDescending(c => c.Severity))
        {
            report.AppendLine($"- {conflict.GetTitle()}: {conflict.Description}");
            report.AppendLine($"  Severity: {conflict.Severity}");
            report.AppendLine($"  Blocking: {conflict.IsBlocking()}");
            report.AppendLine($"  Migrations: {conflict.FirstMigrationId} ↔ {conflict.SecondMigrationId}");
            report.AppendLine();
        }

        report.AppendLine("=== Merge Recommendation ===");
        report.AppendLine(GetMergeRecommendation(diff));

        return report.ToString();
    }

    /// <summary>
    /// Determines if the branches can be safely merged based on the migration diff.
    /// </summary>
    /// <param name="service">The migration diff service instance.</param>
    /// <param name="diff">The migration diff result to evaluate.</param>
    /// <returns>True if safe to merge, false if conflicts or destructive changes exist.</returns>
    public static bool CanMergeSafely(this MigrationDiffService service, MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(diff);

        return !diff.HasBlockingConflicts() && !service.HasDestructiveChanges(diff);
    }

    /// <summary>
    /// Gets a human-readable recommendation based on the migration diff result.
    /// </summary>
    /// <param name="diff">The migration diff result.</param>
    /// <returns>A recommendation string.</returns>
    private static string GetRecommendation(MigrationDiff diff)
    {
        if (diff.Result == ComparisonResult.Identical)
        {
            return "✓ Migrations are identical - safe to merge";
        }

        if (diff.Result == ComparisonResult.Similar)
        {
            return "✓ Minor differences detected - review schema changes before merging";
        }

        if (diff.Result == ComparisonResult.Different && !diff.HasConflicts())
        {
            return "⚠ Significant differences detected - manual review recommended";
        }

        if (diff.HasBlockingConflicts())
        {
            return "✗ BLOCKING CONFLICTS DETECTED - DO NOT MERGE";
        }

        if (diff.HasDestructiveChanges())
        {
            return "✗ DESTRUCTIVE CHANGES DETECTED - manual data backup required before merge";
        }

        return "⚠ Review required - conflicts or significant differences detected";
    }

    /// <summary>
    /// Gets a merge recommendation based on conflict severity.
    /// </summary>
    /// <param name="diff">The migration diff result.</param>
    /// <returns>A recommendation string for merge decisions.</returns>
    private static string GetMergeRecommendation(MigrationDiff diff)
    {
        var blockingCount = diff.GetBlockingConflicts();
        var destructiveCount = diff.GetDestructiveChanges().Count;

        if (blockingCount > 0)
        {
            return "BLOCK merge until conflicts are resolved. Critical conflicts detected.";
        }

        if (destructiveCount > 0)
        {
            return "REVIEW merge carefully. Destructive changes require data backup and manual intervention.";
        }

        if (diff.Conflicts.Count > 3)
        {
            return "Multiple conflicts detected. Manual merge and extensive testing required.";
        }

        return "Conflicts present but may be resolvable. Manual review recommended before merging.";
    }
}