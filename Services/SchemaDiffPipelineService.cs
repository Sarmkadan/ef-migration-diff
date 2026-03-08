// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Configuration;
using EfMigrationDiff.Interfaces;
using EfMigrationDiff.Models;

namespace EfMigrationDiff.Services;

/// <summary>
/// End-to-end pipeline orchestrator that bridges the v1 git and migration infrastructure
/// with the v2 visual diff engine, producing schema comparison reports from
/// <see cref="BranchInfo"/> objects alone.
/// </summary>
/// <remarks>
/// The pipeline delegates schema-change extraction to the existing v1
/// <see cref="MigrationDiffService"/> and then feeds the resulting
/// <see cref="SchemaChange"/> collections into the v2 <see cref="ISchemaDiffEngine"/>.
/// This layered design keeps v1 and v2 logic fully decoupled while still sharing
/// all migration-parsing infrastructure.
/// </remarks>
public sealed class SchemaDiffPipelineService
{
    private readonly MigrationDiffService _migrationDiffService;
    private readonly ISchemaDiffEngine    _diffEngine;
    private readonly IVisualDiffRenderer  _renderer;

    /// <summary>
    /// Initialises the pipeline with v1 and v2 service dependencies.
    /// </summary>
    /// <param name="migrationDiffService">v1 service that collects schema changes per branch.</param>
    /// <param name="diffEngine">v2 engine that computes two-way and three-way diffs.</param>
    /// <param name="renderer">Renderer that produces HTML output from diff results.</param>
    public SchemaDiffPipelineService(
        MigrationDiffService migrationDiffService,
        ISchemaDiffEngine    diffEngine,
        IVisualDiffRenderer  renderer)
    {
        _migrationDiffService = migrationDiffService;
        _diffEngine           = diffEngine;
        _renderer             = renderer;
    }

    // =========================================================================
    // Two-way diff
    // =========================================================================

    /// <summary>
    /// Runs a two-way schema diff between a source and target branch.
    /// Schema changes are collected via the v1 <see cref="MigrationDiffService"/> and
    /// then processed by the v2 diff engine to produce visual output.
    /// </summary>
    /// <param name="sourceBranch">Source branch whose migrations are compared.</param>
    /// <param name="targetBranch">Target branch whose migrations are compared.</param>
    /// <param name="options">
    /// Optional diff configuration; source and target labels default to branch names.
    /// </param>
    /// <returns>
    /// A <see cref="SchemaDiffPipelineResult"/> containing the computed diff, side-by-side HTML,
    /// and unified HTML representations.
    /// </returns>
    public SchemaDiffPipelineResult RunTwoWayDiff(
        BranchInfo       sourceBranch,
        BranchInfo       targetBranch,
        SchemaDiffOptions? options = null)
    {
        var effectiveOptions = options ?? SchemaDiffOptions.ForBranches(
            sourceBranch.BranchName,
            targetBranch.BranchName);

        var migrationDiff = _migrationDiffService.CompareBranches(sourceBranch, targetBranch);
        var diff = _diffEngine.ComputeDiff(
            migrationDiff.SourceSchemaChanges,
            migrationDiff.TargetSchemaChanges,
            effectiveOptions);

        return new SchemaDiffPipelineResult
        {
            Diff           = diff,
            MigrationDiff  = migrationDiff,
            SideBySideHtml = _renderer.RenderSideBySide(diff),
            UnifiedHtml    = _renderer.RenderUnified(diff),
            SourceBranch   = sourceBranch.BranchName,
            TargetBranch   = targetBranch.BranchName
        };
    }

    // =========================================================================
    // Three-way diff
    // =========================================================================

    /// <summary>
    /// Runs a three-way schema diff using a common ancestor base branch.
    /// Two <see cref="MigrationDiffService"/> calls collect the base, source, and target schema
    /// changes; the v2 engine then identifies conflict regions that require explicit resolution.
    /// </summary>
    /// <param name="baseBranch">Common ancestor branch.</param>
    /// <param name="sourceBranch">Source feature branch.</param>
    /// <param name="targetBranch">Target integration branch.</param>
    /// <param name="options">
    /// Optional diff configuration; labels default to the supplied branch names.
    /// </param>
    /// <returns>
    /// A <see cref="SchemaDiffPipelineResult"/> containing the three-way diff result and
    /// a merge editor HTML document.
    /// </returns>
    public SchemaDiffPipelineResult RunThreeWayDiff(
        BranchInfo        baseBranch,
        BranchInfo        sourceBranch,
        BranchInfo        targetBranch,
        SchemaDiffOptions? options = null)
    {
        var effectiveOptions = options ?? SchemaDiffOptions.ForMerge(
            baseBranch.BranchName,
            sourceBranch.BranchName,
            targetBranch.BranchName);

        // Two base-relative comparisons give us all three change sets.
        // SourceSchemaChanges = changes from the first (base) argument.
        // TargetSchemaChanges = changes from the second argument.
        var baseToSource = _migrationDiffService.CompareBranches(baseBranch, sourceBranch);
        var baseToTarget = _migrationDiffService.CompareBranches(baseBranch, targetBranch);

        var threeWayDiff = _diffEngine.ComputeThreeWayDiff(
            baseToSource.SourceSchemaChanges,   // base
            baseToSource.TargetSchemaChanges,   // source
            baseToTarget.TargetSchemaChanges,   // target
            effectiveOptions);

        return new SchemaDiffPipelineResult
        {
            ThreeWayDiff    = threeWayDiff,
            MergeEditorHtml = _renderer.RenderMergeEditor(threeWayDiff),
            BaseBranch      = baseBranch.BranchName,
            SourceBranch    = sourceBranch.BranchName,
            TargetBranch    = targetBranch.BranchName
        };
    }

    // =========================================================================
    // Auto-merge
    // =========================================================================

    /// <summary>
    /// Attempts to auto-resolve all trivially resolvable conflict regions in a three-way diff.
    /// Irreconcilable regions remain <see cref="MergeResolutionStrategy.Unresolved"/> in the
    /// returned <see cref="SchemaMergeResult"/>.
    /// </summary>
    /// <param name="baseBranch">Common ancestor branch.</param>
    /// <param name="sourceBranch">Source feature branch.</param>
    /// <param name="targetBranch">Target integration branch.</param>
    /// <param name="mergeEditor">The editor used to build the auto-resolution plan.</param>
    /// <param name="options">Optional diff configuration.</param>
    /// <returns>
    /// A <see cref="SchemaMergeResult"/> containing resolved schema changes and any
    /// remaining warnings for unresolved regions.
    /// </returns>
    public SchemaMergeResult TryAutoMerge(
        BranchInfo        baseBranch,
        BranchInfo        sourceBranch,
        BranchInfo        targetBranch,
        IMergeEditor      mergeEditor,
        SchemaDiffOptions? options = null)
    {
        var result = RunThreeWayDiff(baseBranch, sourceBranch, targetBranch, options);

        if (result.ThreeWayDiff is null)
            throw new InvalidOperationException(
                "Three-way diff was not produced; ensure the pipeline ran in three-way mode.");

        var plan = mergeEditor.AutoMerge(result.ThreeWayDiff);
        return _diffEngine.ApplyMergeResolution(result.ThreeWayDiff, plan);
    }
}

/// <summary>
/// The aggregate output of a <see cref="SchemaDiffPipelineService"/> run, containing the
/// computed diff result(s) alongside all rendered HTML documents.
/// </summary>
public sealed class SchemaDiffPipelineResult
{
    /// <summary>
    /// Two-way diff result; populated for two-way runs, <see langword="null"/> otherwise.
    /// </summary>
    public SchemaDiffResult? Diff { get; init; }

    /// <summary>
    /// Three-way diff result; populated for three-way runs, <see langword="null"/> otherwise.
    /// </summary>
    public ThreeWayDiffResult? ThreeWayDiff { get; init; }

    /// <summary>
    /// The underlying v1 <see cref="Models.MigrationDiff"/> that drove the two-way run;
    /// <see langword="null"/> for three-way runs.
    /// </summary>
    public MigrationDiff? MigrationDiff { get; init; }

    /// <summary>Side-by-side HTML document for two-way runs; empty string otherwise.</summary>
    public string SideBySideHtml { get; init; } = string.Empty;

    /// <summary>Unified HTML document for two-way runs; empty string otherwise.</summary>
    public string UnifiedHtml { get; init; } = string.Empty;

    /// <summary>Merge editor HTML document for three-way runs; empty string otherwise.</summary>
    public string MergeEditorHtml { get; init; } = string.Empty;

    /// <summary>
    /// Name of the common ancestor branch; <see langword="null"/> for two-way runs.
    /// </summary>
    public string? BaseBranch { get; init; }

    /// <summary>Name of the source branch.</summary>
    public required string SourceBranch { get; init; }

    /// <summary>Name of the target branch.</summary>
    public required string TargetBranch { get; init; }

    /// <summary>Returns <c>true</c> when this result came from a three-way pipeline run.</summary>
    public bool IsThreeWay => ThreeWayDiff is not null;

    /// <summary>
    /// Returns <c>true</c> when the diff contains no conflicts or differences —
    /// identical schemas for two-way runs, or zero conflict regions for three-way runs.
    /// </summary>
    public bool IsClean => IsThreeWay
        ? ThreeWayDiff!.ConflictCount == 0
        : Diff?.IsIdentical ?? true;

    /// <summary>
    /// Returns <c>true</c> when at least one side contains a destructive schema change
    /// such as <c>DROP TABLE</c> or <c>DROP COLUMN</c>. Only meaningful for two-way runs.
    /// </summary>
    public bool HasDestructiveChanges => Diff?.HasDestructiveChanges ?? false;
}
