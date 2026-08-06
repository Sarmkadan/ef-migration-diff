#nullable enable
using System.Text.Json.Serialization;

namespace EfMigrationDiff.Models;

/// <summary>
/// Classifies how a single line appears in a diff view.
/// </summary>
public enum DiffLineKind
{
    /// <summary>Line is identical in both source and target.</summary>
    Unchanged = 0,

    /// <summary>Line exists only in the source (removed relative to target).</summary>
    Removed = 1,

    /// <summary>Line exists only in the target (added relative to source).</summary>
    Added = 2,

    /// <summary>Line is present on both sides but with different content.</summary>
    Modified = 3,

    /// <summary>Empty placeholder used to vertically align the opposite column in a side-by-side view.</summary>
    Placeholder = 4
}

/// <summary>
/// Specifies how a merge conflict region should be resolved during a three-way merge.
/// </summary>
public enum MergeResolutionStrategy
{
    /// <summary>No resolution has been chosen for this region yet.</summary>
    Unresolved = 0,

    /// <summary>Keep the source-branch version of this region.</summary>
    AcceptSource = 1,

    /// <summary>Keep the target-branch version of this region.</summary>
    AcceptTarget = 2,

    /// <summary>Include both versions, source content first then target content.</summary>
    AcceptBoth = 3,

    /// <summary>Use a manually provided custom value for this region.</summary>
    Custom = 4
}

/// <summary>
/// Represents a single rendered line within a schema diff view.
/// </summary>
/// <param name="Kind">How the line is classified in the diff.</param>
/// <param name="LineNumber">Source or target line number; 0 for placeholder lines.</param>
/// <param name="Content">Textual content of the line.</param>
/// <param name="CorrespondingLineNumber">
/// In side-by-side views, the matching line number on the opposite pane. 0 if no counterpart exists.
/// </param>
public sealed record DiffLine(
    DiffLineKind Kind,
    int LineNumber,
    string Content,
    int CorrespondingLineNumber = 0)
{
    /// <summary>Returns <c>true</c> if this line represents a change from one side to the other.</summary>
    [JsonIgnore]
    public bool IsChanged => Kind is DiffLineKind.Added or DiffLineKind.Removed or DiffLineKind.Modified;

    /// <summary>
    /// Returns the standard unified-diff marker character:
    /// <c>+</c> for additions, <c>-</c> for removals, and a space for unchanged lines.
    /// </summary>
    [JsonIgnore]
    public char UnifiedMarker => Kind switch
    {
        DiffLineKind.Added   => '+',
        DiffLineKind.Removed => '-',
        _                    => ' '
    };
}

/// <summary>
/// A contiguous block of diff lines that together form one logical change region (hunk).
/// </summary>
/// <param name="SourceStart">One-based starting line number on the source side.</param>
/// <param name="TargetStart">One-based starting line number on the target side.</param>
/// <param name="Lines">Ordered lines that make up this hunk, including any context lines.</param>
public sealed record DiffHunk(int SourceStart, int TargetStart, IReadOnlyList<DiffLine> Lines)
{
    /// <summary>Number of lines added (target-only) in this hunk.</summary>
    [JsonIgnore]
    public int AddedCount => Lines.Count(l => l.Kind == DiffLineKind.Added);

    /// <summary>Number of lines removed (source-only) in this hunk.</summary>
    [JsonIgnore]
    public int RemovedCount => Lines.Count(l => l.Kind == DiffLineKind.Removed);

    /// <summary>Returns <c>true</c> when the hunk contains at least one changed line.</summary>
    [JsonIgnore]
    public bool HasChanges => Lines.Any(l => l.IsChanged);
}

/// <summary>
/// Represents a region within a three-way diff where source and target both diverged from
/// the common ancestor base, creating a merge conflict that must be explicitly resolved.
/// </summary>
public sealed class MergeConflictRegion
{
    /// <summary>Unique identifier for this conflict region, used in resolution plans.</summary>
    public required Guid Id { get; init; }

    /// <summary>Zero-based index of the first conflicting hunk in the overall diff sequence.</summary>
    public required int HunkIndex { get; init; }

    /// <summary>Human-readable description of what schema elements are in conflict.</summary>
    public required string Description { get; init; }

    /// <summary>Lines representing the source-branch version of the conflicting content.</summary>
    public IReadOnlyList<DiffLine> SourceLines { get; init; } = [];

    /// <summary>Lines representing the target-branch version of the conflicting content.</summary>
    public IReadOnlyList<DiffLine> TargetLines { get; init; } = [];

    /// <summary>Lines representing the common ancestor (base) state for reference context.</summary>
    public IReadOnlyList<DiffLine> BaseLines { get; init; } = [];

    /// <summary>The resolution strategy chosen for this region.</summary>
    public MergeResolutionStrategy Resolution { get; set; } = MergeResolutionStrategy.Unresolved;

    /// <summary>
    /// Custom merged content when <see cref="Resolution"/> is
    /// <see cref="MergeResolutionStrategy.Custom"/>.
    /// </summary>
    public string? CustomContent { get; set; }

    /// <summary>Returns <c>true</c> when a resolution strategy has been chosen.</summary>
    [JsonIgnore]
    public bool IsResolved => Resolution != MergeResolutionStrategy.Unresolved;

    /// <summary>
    /// Returns <c>true</c> if both sides contain identical content despite diverging from base,
    /// meaning the conflict can be auto-resolved without any ambiguity.
    /// </summary>
    [JsonIgnore]
    public bool IsTriviallyResolvable =>
        SourceLines.Select(l => l.Content).SequenceEqual(TargetLines.Select(l => l.Content));
}

/// <summary>
/// Encapsulates the resolution decisions for every conflict region in a three-way diff.
/// Passed to <c>ISchemaDiffEngine.ApplyMergeResolution</c> to produce a merged schema.
/// </summary>
public sealed class MergeResolutionPlan
{
    /// <summary>
    /// Maps each <see cref="MergeConflictRegion.Id"/> to its chosen
    /// <see cref="MergeResolutionStrategy"/>.
    /// </summary>
    public Dictionary<Guid, MergeResolutionStrategy> Resolutions { get; init; } = new();

    /// <summary>
    /// Stores custom content for regions resolved with <see cref="MergeResolutionStrategy.Custom"/>,
    /// keyed by <see cref="MergeConflictRegion.Id"/>.
    /// </summary>
    public Dictionary<Guid, string> CustomContent { get; init; } = new();

    /// <summary>
    /// Returns <c>true</c> when every region in <paramref name="regions"/> has a non-unresolved entry.
    /// </summary>
    /// <param name="regions">The conflict regions to check against.</param>
    public bool IsComplete(IEnumerable<MergeConflictRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(regions);
        return regions.All(r => Resolutions.TryGetValue(r.Id, out var s) && s != MergeResolutionStrategy.Unresolved);
    }

    /// <summary>Counts how many regions were resolved with the given strategy.</summary>
    /// <param name="strategy">The strategy to count.</param>
    public int CountByStrategy(MergeResolutionStrategy strategy) =>
        Resolutions.Values.Count(v => v == strategy);
}

/// <summary>
/// The output of a two-way schema diff computation between a source and a target branch.
/// </summary>
public sealed class SchemaDiffResult
{
    /// <summary>Unique identifier assigned to this diff computation.</summary>
    public required Guid Id { get; init; }

    /// <summary>Display label for the left (source) side of the diff.</summary>
    public required string SourceLabel { get; init; }

    /// <summary>Display label for the right (target) side of the diff.</summary>
    public required string TargetLabel { get; init; }

    /// <summary>Ordered diff hunks that together cover all changed regions.</summary>
    public IReadOnlyList<DiffHunk> Hunks { get; init; } = [];

    /// <summary>Schema changes that are present only in the source branch.</summary>
    public IReadOnlyList<SchemaChange> SourceOnlyChanges { get; init; } = [];

    /// <summary>Schema changes that are present only in the target branch.</summary>
    public IReadOnlyList<SchemaChange> TargetOnlyChanges { get; init; } = [];

    /// <summary>Schema changes present in both branches but with differing SQL content.</summary>
    public IReadOnlyList<SchemaChange> ModifiedChanges { get; init; } = [];

    /// <summary>UTC timestamp when this diff was computed.</summary>
    public DateTime ComputedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Total number of added lines across all hunks.</summary>
    [JsonIgnore]
    public int TotalAdded => Hunks.Sum(h => h.AddedCount);

    /// <summary>Total number of removed lines across all hunks.</summary>
    [JsonIgnore]
    public int TotalRemoved => Hunks.Sum(h => h.RemovedCount);

    /// <summary>Returns <c>true</c> when no hunks contain any changed lines.</summary>
    [JsonIgnore]
    public bool IsIdentical => !Hunks.Any(h => h.HasChanges);

    /// <summary>
    /// Returns <c>true</c> if any change on either side is a destructive operation
    /// such as dropping a table or column.
    /// </summary>
    [JsonIgnore]
    public bool HasDestructiveChanges =>
        SourceOnlyChanges.Any(c => c.IsDestructive()) ||
        TargetOnlyChanges.Any(c => c.IsDestructive());
}

/// <summary>
/// The output of a three-way diff computation using a common ancestor base plus source and target.
/// </summary>
public sealed class ThreeWayDiffResult
{
    /// <summary>Unique identifier assigned to this computation.</summary>
    public required Guid Id { get; init; }

    /// <summary>Label for the common ancestor (base) branch.</summary>
    public required string BaseLabel { get; init; }

    /// <summary>Label for the source branch.</summary>
    public required string SourceLabel { get; init; }

    /// <summary>Label for the target branch.</summary>
    public required string TargetLabel { get; init; }

    /// <summary>Two-way diff between the base branch and the source branch.</summary>
    public required SchemaDiffResult BaseToSource { get; init; }

    /// <summary>Two-way diff between the base branch and the target branch.</summary>
    public required SchemaDiffResult BaseToTarget { get; init; }

    /// <summary>
    /// Conflict regions where both source and target diverged differently from the base,
    /// requiring explicit resolution before a clean merge is possible.
    /// </summary>
    public IReadOnlyList<MergeConflictRegion> ConflictRegions { get; init; } = [];

    /// <summary>UTC timestamp of when this computation was performed.</summary>
    public DateTime ComputedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Returns <c>true</c> when any conflict region has not yet been resolved.</summary>
    [JsonIgnore]
    public bool HasUnresolvedConflicts => ConflictRegions.Any(r => !r.IsResolved);

    /// <summary>Total number of conflict regions detected.</summary>
    [JsonIgnore]
    public int ConflictCount => ConflictRegions.Count;

    /// <summary>
    /// Returns <c>true</c> when all conflicts can be auto-resolved without ambiguity
    /// because both sides made identical changes in every conflicting region.
    /// </summary>
    [JsonIgnore]
    public bool IsAutoMergeable => ConflictRegions.All(r => r.IsTriviallyResolvable);
}

/// <summary>
/// The result of applying a <see cref="MergeResolutionPlan"/> to a <see cref="ThreeWayDiffResult"/>.
/// </summary>
/// <param name="IsSuccessful">Whether all conflict regions were fully resolved.</param>
/// <param name="ResolvedChanges">The final merged set of schema changes.</param>
/// <param name="UnresolvedCount">Number of regions that had no valid resolution in the plan.</param>
/// <param name="Warnings">Non-fatal issues encountered during merge application.</param>
public sealed record SchemaMergeResult(
    bool IsSuccessful,
    IReadOnlyList<SchemaChange> ResolvedChanges,
    int UnresolvedCount,
    IReadOnlyList<string> Warnings)
{
    /// <summary>Returns <c>true</c> when the merge was fully successful and produced no warnings.</summary>
    [JsonIgnore]
    public bool IsClean => IsSuccessful && Warnings.Count == 0;

    /// <summary>Number of schema changes in the merged output.</summary>
    [JsonIgnore]
    public int ChangeCount => ResolvedChanges.Count;
}
