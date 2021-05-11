#nullable enable

using EfMigrationDiff.Models;
using FluentAssertions;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Extension methods for <see cref="VisualDiffOutputTests"/> that provide additional utility
/// for testing schema diff scenarios and merge operations.
/// </summary>
public static class VisualDiffOutputTestsExtensions
{
    /// <summary>
    /// Creates a <see cref="SchemaDiffResult"/> with the specified changes for testing purposes.
    /// </summary>
    /// <param name="engine">The test engine instance.</param>
    /// <param name="changes">The schema changes to include in the diff result.</param>
    /// <param name="sourceLabel">Label for the source side.</param>
    /// <param name="targetLabel">Label for the target side.</param>
    /// <returns>A new <see cref="SchemaDiffResult"/> populated with the provided changes.</returns>
    public static SchemaDiffResult CreateDiffResult(
        this VisualDiffOutputTests engine,
        IReadOnlyList<SchemaChange> changes,
        string sourceLabel = "source",
        string targetLabel = "target")
    {
        var hunks = new List<DiffHunk>();
        foreach (var change in changes)
        {
            hunks.Add(new DiffHunk(1, 1, [
                new DiffLine(
                    change.ChangeType == SqlChangeType.DropTable || change.ChangeType == SqlChangeType.DropColumn
                        ? DiffLineKind.Removed
                        : DiffLineKind.Added,
                    1,
                    change.Sql)
                ]));
        }

        return new SchemaDiffResult
        {
            Id = Guid.NewGuid(),
            SourceLabel = sourceLabel,
            TargetLabel = targetLabel,
            SourceOnlyChanges = changes.Where(c => c.ChangeType == SqlChangeType.CreateTable || c.ChangeType == SqlChangeType.AddColumn).ToList(),
            TargetOnlyChanges = changes.Where(c => c.ChangeType == SqlChangeType.DropTable || c.ChangeType == SqlChangeType.DropColumn).ToList(),
            ModifiedChanges = changes.Where(c => c.ChangeType == SqlChangeType.ModifyColumn || c.ChangeType == SqlChangeType.AlterTable).ToList(),
            Hunks = hunks,
            ComputedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a <see cref="ThreeWayDiffResult"/> with the specified diffs and conflicts for testing.
    /// </summary>
    /// <param name="engine">The test engine instance.</param>
    /// <param name="baseToSource">Diff result between base and source.</param>
    /// <param name="baseToTarget">Diff result between base and target.</param>
    /// <param name="conflictRegions">Optional conflict regions to include.</param>
    /// <returns>A new <see cref="ThreeWayDiffResult"/> with the provided configuration.</returns>
    public static ThreeWayDiffResult CreateThreeWayDiff(
        this VisualDiffOutputTests engine,
        SchemaDiffResult baseToSource,
        SchemaDiffResult baseToTarget,
        IReadOnlyList<MergeConflictRegion>? conflictRegions = null)
    {
        return new ThreeWayDiffResult
        {
            Id = Guid.NewGuid(),
            BaseLabel = "base",
            SourceLabel = "source",
            TargetLabel = "target",
            BaseToSource = baseToSource,
            BaseToTarget = baseToTarget,
            ConflictRegions = conflictRegions ?? []
        };
    }

    /// <summary>
    /// Creates a simple merge conflict region for testing resolution strategies.
    /// </summary>
    /// <param name="engine">The test engine instance.</param>
    /// <param name="conflictId">Optional conflict ID (will generate one if null).</param>
    /// <param name="description">Description of the conflict.</param>
    /// <param name="sourceContent">Content for the source side.</param>
    /// <param name="targetContent">Content for the target side.</param>
    /// <returns>A new <see cref="MergeConflictRegion"/> with the specified content.</returns>
    public static MergeConflictRegion CreateConflictRegion(
        this VisualDiffOutputTests engine,
        string? description = null,
        string? sourceContent = null,
        string? targetContent = null,
        Guid? conflictId = null)
    {
        var id = conflictId ?? Guid.NewGuid();
        var sharedLine = new DiffLine(DiffLineKind.Unchanged, 1, sourceContent ?? targetContent ?? "shared content");

        return new MergeConflictRegion
        {
            Id = id,
            HunkIndex = 0,
            Description = description ?? "Test conflict region",
            SourceLines = [sharedLine],
            TargetLines = [sharedLine],
            BaseLines = []
        };
    }

    /// <summary>
    /// Creates a <see cref="MergeResolutionPlan"/> with the specified resolution strategy.
    /// </summary>
    /// <param name="engine">The test engine instance.</param>
    /// <param name="strategy">The resolution strategy to apply.</param>
    /// <param name="conflictIds">Conflict IDs to include in the plan.</param>
    /// <returns>A new <see cref="MergeResolutionPlan"/> with the specified resolutions.</returns>
    public static MergeResolutionPlan CreateResolutionPlan(
        this VisualDiffOutputTests engine,
        MergeResolutionStrategy strategy,
        params Guid[] conflictIds)
    {
        var resolutions = new Dictionary<Guid, MergeResolutionStrategy>();
        foreach (var id in conflictIds)
        {
            resolutions[id] = strategy;
        }

        return new MergeResolutionPlan
        {
            Resolutions = resolutions,
            CustomContent = new Dictionary<Guid, string>()
        };
    }

    /// <summary>
    /// Asserts that a <see cref="SchemaDiffResult"/> has no changes (is identical).
    /// </summary>
    /// <param name="engine">The test engine instance.</param>
    /// <param name="result">The diff result to assert.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    public static void ShouldBeIdentical(
        this VisualDiffOutputTests engine,
        SchemaDiffResult result,
        string? because = null)
    {
        result.IsIdentical.Should().BeTrue(because ?? "Expected diff result to be identical");
        result.HasDestructiveChanges.Should().BeFalse(because ?? "Expected no destructive changes");
        result.TotalAdded.Should().Be(0, because ?? "Expected no added lines");
        result.TotalRemoved.Should().Be(0, because ?? "Expected no removed lines");
        result.SourceOnlyChanges.Should().BeEmpty(because ?? "Expected no source-only changes");
        result.TargetOnlyChanges.Should().BeEmpty(because ?? "Expected no target-only changes");
        result.ModifiedChanges.Should().BeEmpty(because ?? "Expected no modified changes");
    }

    /// <summary>
    /// Asserts that a <see cref="SchemaDiffResult"/> has changes.
    /// </summary>
    /// <param name="engine">The test engine instance.</param>
    /// <param name="result">The diff result to assert.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    public static void ShouldHaveChanges(
        this VisualDiffOutputTests engine,
        SchemaDiffResult result,
        string? because = null)
    {
        result.IsIdentical.Should().BeFalse(because ?? "Expected diff result to have changes");
        result.HasChanges().Should().BeTrue(because ?? "Expected diff result to have changes");
    }

    /// <summary>
    /// Asserts that a <see cref="ThreeWayDiffResult"/> has no unresolved conflicts.
    /// </summary>
    /// <param name="engine">The test engine instance.</param>
    /// <param name="result">The three-way diff result to assert.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    public static void ShouldBeFullyResolved(
        this VisualDiffOutputTests engine,
        ThreeWayDiffResult result,
        string? because = null)
    {
        result.HasUnresolvedConflicts.Should().BeFalse(because ?? "Expected all conflicts to be resolved");
        result.IsAutoMergeable.Should().BeFalse(because: "ThreeWayDiffResult.IsAutoMergeable should not be set by this assertion");
    }

    /// <summary>
    /// Asserts that a <see cref="MergeResolutionPlan"/> contains a specific resolution strategy.
    /// </summary>
    /// <param name="engine">The test engine instance.</param>
    /// <param name="plan">The resolution plan to assert.</param>
    /// <param name="strategy">The expected strategy.</param>
    /// <param name="conflictId">The conflict ID to check.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    public static void ShouldResolveWithStrategy(
        this VisualDiffOutputTests engine,
        MergeResolutionPlan plan,
        MergeResolutionStrategy strategy,
        Guid conflictId,
        string? because = null)
    {
        plan.Resolutions.Should().ContainKey(conflictId, because ?? "Expected resolution plan to contain conflict ID");
        plan.Resolutions[conflictId].Should().Be(strategy, because ?? "Expected specific resolution strategy");
    }

    /// <summary>
    /// Gets the total number of changes in a <see cref="SchemaDiffResult"/>.
    /// </summary>
    /// <param name="result">The diff result.</param>
    /// <returns>Total number of changes (added + removed + modified).</returns>
    public static int TotalChanges(this SchemaDiffResult result)
    {
        return result.TotalAdded + result.TotalRemoved + result.ModifiedChanges.Count;
    }

    /// <summary>
    /// Determines whether a <see cref="SchemaDiffResult"/> has any changes at all.
    /// </summary>
    /// <param name="result">The diff result.</param>
    /// <returns>True if the result has any changes; otherwise false.</returns>
    public static bool HasChanges(this SchemaDiffResult result)
    {
        return !result.IsIdentical;
    }

    /// <summary>
    /// Gets the count of conflict regions with a specific resolution strategy.
    /// </summary>
    /// <param name="plan">The resolution plan.</param>
    /// <param name="strategy">The strategy to count.</param>
    /// <returns>Number of regions resolved with the specified strategy.</returns>
    public static int CountResolvedWithStrategy(
        this MergeResolutionPlan plan,
        MergeResolutionStrategy strategy)
    {
        return plan.CountByStrategy(strategy);
    }
}
