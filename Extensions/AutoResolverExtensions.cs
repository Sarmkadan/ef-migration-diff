#nullable enable
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EfMigrationDiff.Extensions;

/// <summary>
/// Extension methods for registering the migration auto-resolver and for working
/// with <see cref="MergeResult"/> and <see cref="ConflictInfo"/> collections.
/// </summary>
public static class AutoResolverExtensions
{
    /// <summary>
    /// Registers <see cref="MigrationAutoResolverService"/> as a singleton in the
    /// dependency-injection container. Logging infrastructure is added automatically
    /// if it has not already been registered.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <paramref name="services"/> instance for fluent chaining.</returns>
    public static IServiceCollection AddMigrationAutoResolver(this IServiceCollection services)
    {
        services.AddLogging();
        services.AddSingleton<MigrationAutoResolverService>();
        return services;
    }

    /// <summary>
    /// Filters a collection of conflicts to those eligible for automatic resolution:
    /// conflicts that are not yet resolved and have a severity below
    /// <see cref="ConflictSeverity.Error"/>.
    /// </summary>
    /// <param name="conflicts">Source conflict collection to filter.</param>
    /// <returns>An enumerable of auto-resolvable candidates.</returns>
    public static IEnumerable<ConflictInfo> GetAutoResolvableCandidates(
        this IEnumerable<ConflictInfo> conflicts)
    {
        return conflicts.Where(c =>
            !c.IsResolved &&
            c.Severity < ConflictSeverity.Error);
    }

    /// <summary>
    /// Returns a multiline text summary of a <see cref="MergeResult"/> suitable for
    /// console output or structured log entries.
    /// </summary>
    /// <param name="result">The merge result to describe.</param>
    /// <returns>A formatted, human-readable summary string.</returns>
    public static string ToDetailedSummary(this MergeResult result)
    {
        var lines = new List<string>
        {
            $"Merge Result [{result.Id[..8]}]  {result.ResolvedAt:u}",
            $"  Total      : {result.TotalConflicts}",
            $"  Resolved   : {result.ResolvedCount}",
            $"  Unresolved : {result.UnresolvedCount}",
            $"  Blocking   : {(result.HasBlockingConflicts ? "YES — deployment blocked" : "none")}",
        };

        if (result.Attempts.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("  Attempts:");
            foreach (var attempt in result.Attempts)
                lines.Add($"    {attempt}");
        }

        if (result.UnresolvedConflicts.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("  Requires manual review:");
            foreach (var conflict in result.UnresolvedConflicts)
                lines.Add($"    {conflict}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Groups unresolved conflicts in a <see cref="MergeResult"/> by conflict type,
    /// making it straightforward to prioritise manual triage by category.
    /// </summary>
    /// <param name="result">The merge result whose unresolved conflicts to group.</param>
    /// <returns>A dictionary keyed by <see cref="ConflictType"/>.</returns>
    public static IReadOnlyDictionary<ConflictType, List<ConflictInfo>> GroupUnresolvedByType(
        this MergeResult result)
    {
        return result.UnresolvedConflicts
            .GroupBy(c => c.ConflictType)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Returns <c>true</c> if the <see cref="MergeResult"/> is safe to proceed without
    /// any manual intervention (fully resolved and no blocking conflicts remain).
    /// </summary>
    /// <param name="result">The merge result to evaluate.</param>
    public static bool IsSafeToMerge(this MergeResult result) =>
        result.IsFullyResolved && !result.HasBlockingConflicts;
}
