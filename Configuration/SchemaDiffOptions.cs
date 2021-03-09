#nullable enable
namespace EfMigrationDiff.Configuration;

/// <summary>
/// Immutable configuration options that control how schema diffs and merge operations are computed
/// and rendered by <c>SchemaDiffEngine</c> and <c>VisualDiffFormatter</c>.
/// Defined as a record to allow non-destructive <c>with</c>-expression copies inside the engine.
/// </summary>
public sealed record SchemaDiffOptions
{
    /// <summary>
    /// Display label for the common ancestor (base) branch.
    /// Used in three-way diff output and merge editor HTML rendering.
    /// Defaults to <c>"base"</c>.
    /// </summary>
    public string BaseLabel { get; init; } = "base";

    /// <summary>
    /// Display label for the source branch (left pane in side-by-side views).
    /// Defaults to <c>"source"</c>.
    /// </summary>
    public string SourceLabel { get; init; } = "source";

    /// <summary>
    /// Display label for the target branch (right pane in side-by-side views).
    /// Defaults to <c>"target"</c>.
    /// </summary>
    public string TargetLabel { get; init; } = "target";

    /// <summary>
    /// Number of unchanged lines to show before and after each changed region
    /// in unified and side-by-side views. Defaults to <c>3</c>.
    /// </summary>
    public int ContextLines { get; init; } = 3;

    /// <summary>
    /// When <c>true</c>, the raw SQL statement for each schema change is included
    /// as a rendered line in the diff output. Defaults to <c>true</c>.
    /// </summary>
    public bool IncludeSqlContent { get; init; } = true;

    /// <summary>
    /// When <c>true</c>, old-value and new-value metadata lines are appended beneath
    /// each schema change entry. Defaults to <c>false</c>.
    /// </summary>
    public bool IncludeMetadata { get; init; } = false;

    /// <summary>
    /// When <c>true</c>, leading and trailing whitespace differences are normalised
    /// before line comparison, reducing cosmetic noise in the diff. Defaults to <c>false</c>.
    /// </summary>
    public bool IgnoreWhitespace { get; init; } = false;

    /// <summary>
    /// Maximum number of content lines a single hunk may contain before it is split
    /// at the nearest unchanged boundary. A value of <c>0</c> disables splitting.
    /// Defaults to <c>0</c>.
    /// </summary>
    public int MaxHunkLines { get; init; } = 0;

    /// <summary>
    /// Default options instance with production-safe values.
    /// </summary>
    public static SchemaDiffOptions Default => new();

    /// <summary>
    /// Creates options pre-configured for a named two-way branch comparison.
    /// </summary>
    /// <param name="source">Source branch display name.</param>
    /// <param name="target">Target branch display name.</param>
    /// <returns>A new <see cref="SchemaDiffOptions"/> with the specified labels.</returns>
    public static SchemaDiffOptions ForBranches(string source, string target) => new()
    {
        SourceLabel = source,
        TargetLabel = target
    };

    /// <summary>
    /// Creates options pre-configured for a three-way merge scenario.
    /// </summary>
    /// <param name="baseBranch">Common ancestor branch display name.</param>
    /// <param name="source">Source branch display name.</param>
    /// <param name="target">Target branch display name.</param>
    /// <returns>A new <see cref="SchemaDiffOptions"/> with all three labels set.</returns>
    public static SchemaDiffOptions ForMerge(string baseBranch, string source, string target) => new()
    {
        BaseLabel   = baseBranch,
        SourceLabel = source,
        TargetLabel = target
    };
}
