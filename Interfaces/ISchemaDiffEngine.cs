#nullable enable
using EfMigrationDiff.Configuration;
using EfMigrationDiff.Models;

namespace EfMigrationDiff.Interfaces;

/// <summary>
/// Core engine for computing visual schema diffs and three‑way merge operations between
/// Entity Framework migration branches.
/// </summary>
/// <remarks>
/// Implementations of this interface provide the logic to compare schema changes,
/// generate diff results, and apply merge resolutions.
/// </remarks>
public interface ISchemaDiffEngine
{
    /// <summary>
    /// Computes a two‑way visual diff between schema changes from two branches.
    /// </summary>
    /// <param name="sourceChanges">Schema changes detected in the source branch.</param>
    /// <param name="targetChanges">Schema changes detected in the target branch.</param>
    /// <param name="options">Optional configuration for the diff computation.</param>
    /// <returns>
    /// A <see cref="SchemaDiffResult"/> containing hunks, added, removed, and modified entries.
    /// </returns>
    SchemaDiffResult ComputeDiff(
        IReadOnlyList<SchemaChange> sourceChanges,
        IReadOnlyList<SchemaChange> targetChanges,
        SchemaDiffOptions? options = null);

    /// <summary>
    /// Computes a three‑way diff using a common ancestor (base) alongside source and target branches.
    /// </summary>
    /// <param name="baseChanges">Schema changes in the common ancestor branch.</param>
    /// <param name="sourceChanges">Schema changes in the source branch.</param>
    /// <param name="targetChanges">Schema changes in the target branch.</param>
    /// <param name="options">Optional configuration for the diff computation.</param>
    /// <returns>A <see cref="ThreeWayDiffResult"/> with identified conflict regions.</returns>
    ThreeWayDiffResult ComputeThreeWayDiff(
        IReadOnlyList<SchemaChange> baseChanges,
        IReadOnlyList<SchemaChange> sourceChanges,
        IReadOnlyList<SchemaChange> targetChanges,
        SchemaDiffOptions? options = null);

    /// <summary>
    /// Applies a merge resolution plan to a three‑way diff, producing a merged schema.
    /// </summary>
    /// <param name="diff">The three‑way diff with identified conflict regions.</param>
    /// <param name="plan">User‑ or system‑provided resolution decisions for each conflict region.</param>
    /// <returns>A <see cref="SchemaMergeResult"/> describing the outcome and resolved changes.</returns>
    SchemaMergeResult ApplyMergeResolution(ThreeWayDiffResult diff, MergeResolutionPlan plan);
}

/// <summary>
/// Provides HTML rendering of schema diff results for visual inspection and merge editing.
/// </summary>
/// <remarks>
/// Implementations should generate self‑contained HTML documents that can be displayed
/// in a browser without external dependencies.
/// </remarks>
public interface IVisualDiffRenderer
{
    /// <summary>
    /// Renders a schema diff as a side‑by‑side HTML document with source on the left
    /// and target on the right.
    /// </summary>
    /// <param name="diff">The computed diff to render.</param>
    /// <returns>A complete, self‑contained HTML document string.</returns>
    string RenderSideBySide(SchemaDiffResult diff);

    /// <summary>
    /// Renders a schema diff in unified format, interleaving additions and removals.
    /// </summary>
    /// <param name="diff">The computed diff to render.</param>
    /// <returns>A complete, self‑contained HTML document string.</returns>
    string RenderUnified(SchemaDiffResult diff);

    /// <summary>
    /// Renders an interactive three‑way merge editor view with per‑region conflict controls.
    /// </summary>
    /// <param name="diff">The three‑way diff to render.</param>
    /// <returns>A complete HTML document string representing the merge editor.</returns>
    string RenderMergeEditor(ThreeWayDiffResult diff);
}

/// <summary>
/// Constructs and validates merge resolution plans for three‑way diffs.
/// </summary>
/// <remarks>
/// Implementations should provide strategies for automatically or manually resolving
/// conflicts identified by a <see cref="ThreeWayDiffResult"/>.
/// </remarks>
public interface IMergeEditor
{
    /// <summary>
    /// Builds a resolution plan that accepts all source‑branch changes for every conflict region.
    /// </summary>
    /// <param name="diff">The three‑way diff containing conflict regions.</param>
    /// <returns>A fully resolved <see cref="MergeResolutionPlan"/>.</returns>
    MergeResolutionPlan AcceptSource(ThreeWayDiffResult diff);

    /// <summary>
    /// Builds a resolution plan that accepts all target‑branch changes for every conflict region.
    /// </summary>
    /// <param name="diff">The three‑way diff containing conflict regions.</param>
    /// <returns>A fully resolved <see cref="MergeResolutionPlan"/>.</returns>
    MergeResolutionPlan AcceptTarget(ThreeWayDiffResult diff);

    /// <summary>
    /// Attempts to combine non‑conflicting source and target changes automatically.
    /// Regions where both sides differ from each other remain unresolved.
    /// </summary>
    /// <param name="diff">The three‑way diff containing conflict regions.</param>
    /// <returns>A <see cref="MergeResolutionPlan"/> with trivially resolvable conflicts auto‑resolved.</returns>
    MergeResolutionPlan AutoMerge(ThreeWayDiffResult diff);

    /// <summary>
    /// Validates a resolution plan for logical consistency and completeness.
    /// </summary>
    /// <param name="plan">The plan to validate.</param>
    /// <param name="context">The three‑way diff the plan is intended to resolve.</param>
    /// <returns>A read‑only list of validation error messages; empty when the plan is valid.</returns>
    IReadOnlyList<string> ValidateResolution(MergeResolutionPlan plan, ThreeWayDiffResult context);
}
