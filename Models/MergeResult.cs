#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Models;

/// <summary>
/// Defines the strategy applied when automatically resolving a migration conflict.
/// </summary>
public enum MergeStrategy
{
    /// <summary>The target-branch (most recent) operation takes precedence over the source.</summary>
    LastWins = 0,

    /// <summary>The source-branch (original) operation takes precedence over the target.</summary>
    FirstWins = 1,

    /// <summary>Both operations are emitted sequentially in the merged output.</summary>
    Combine = 2,

    /// <summary>The duplicate operation is dropped as a safe no-op.</summary>
    Skip = 3
}

/// <summary>
/// Records the outcome of a single automated conflict resolution attempt.
/// </summary>
public class MergeAttempt
{
    /// <summary>Identifier of the conflict this attempt addresses.</summary>
    public string ConflictId { get; set; } = string.Empty;

    /// <summary>The conflict type that was processed.</summary>
    public ConflictType ConflictType { get; set; }

    /// <summary>Merge strategy that was applied during this attempt.</summary>
    public MergeStrategy StrategyApplied { get; set; }

    /// <summary>Whether the resolution attempt succeeded.</summary>
    public bool Succeeded { get; set; }

    /// <summary>Human-readable reason for failure when <see cref="Succeeded"/> is <c>false</c>.</summary>
    public string? FailureReason { get; set; }

    /// <summary>Merged SQL fragment produced by the resolution, if applicable.</summary>
    public string? MergedContent { get; set; }

    /// <summary>UTC timestamp when this attempt was made.</summary>
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Returns a concise, one-line description of the attempt outcome.
    /// </summary>
    public override string ToString() =>
        Succeeded
            ? $"[OK]   {ConflictType} — resolved via {StrategyApplied}"
            : $"[FAIL] {ConflictType} — {FailureReason}";
}

/// <summary>
/// Aggregates the outcome of an automated batch conflict-resolution run,
/// separating successfully merged conflicts from those that need manual review.
/// </summary>
public class MergeResult
{
    /// <summary>Unique identifier for this merge result.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>UTC timestamp when the resolution run completed.</summary>
    public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Individual resolution attempts, one per conflict processed.</summary>
    public List<MergeAttempt> Attempts { get; set; } = [];

    /// <summary>Conflicts that could not be auto-resolved and require manual intervention.</summary>
    public List<ConflictInfo> UnresolvedConflicts { get; set; } = [];

    /// <summary>Whether every conflict in the batch was successfully resolved.</summary>
    public bool IsFullyResolved => UnresolvedConflicts.Count == 0 && Attempts.All(a => a.Succeeded);

    /// <summary>Total number of conflicts that were processed.</summary>
    public int TotalConflicts => Attempts.Count + UnresolvedConflicts.Count;

    /// <summary>Number of conflicts that were successfully auto-resolved.</summary>
    public int ResolvedCount => Attempts.Count(a => a.Succeeded);

    /// <summary>Number of conflicts that could not be auto-resolved.</summary>
    public int UnresolvedCount => UnresolvedConflicts.Count + Attempts.Count(a => !a.Succeeded);

    /// <summary>
    /// Whether any unresolved conflict is blocking and would prevent deployment.
    /// </summary>
    public bool HasBlockingConflicts => UnresolvedConflicts.Any(c => c.IsBlocking());

    /// <summary>
    /// Returns a brief human-readable summary of the overall merge outcome.
    /// </summary>
    public string GetSummary()
    {
        if (TotalConflicts == 0)
            return "No conflicts to resolve.";

        return IsFullyResolved
            ? $"All {ResolvedCount} conflict(s) auto-resolved successfully."
            : $"{ResolvedCount}/{TotalConflicts} conflict(s) auto-resolved; {UnresolvedCount} require manual intervention.";
    }
}
