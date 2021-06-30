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
    private const string DefaultSourceLabel = "source";
    private const string DefaultTargetLabel = "target";
    private const string DefaultBaseLabel = "base";
    /// <summary>
    /// Creates a <see cref="SchemaDiffResult"/> with the specified changes for testing purposes.
    /// </summary>
    /// <param name="engine">The test engine instance.</param>
    /// <param name="changes">The schema changes to include in the diff result.</param>
    /// <param name="sourceLabel">Label for the source side.</param>
    /// <param name="targetLabel">Label for the target side.</param>
    /// <returns>A new <see cref="SchemaDiffResult"/> populated with the provided changes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="engine"/> or <paramref name="changes"/> is <see langword="null"/></exception>
    public static SchemaDiffResult CreateDiffResult(
        this VisualDiffOutputTests engine,
        IReadOnlyList<SchemaChange> changes,
        string sourceLabel = DefaultSourceLabel,
        string targetLabel = DefaultTargetLabel)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(changes);

        var hunks = new List<DiffHunk>();
        foreach (var change in changes)
        {
            hunks.Add(new DiffHunk(1, 1, [
                new DiffLine(
                    change.ChangeType is SqlChangeType.DropTable or SqlChangeType.DropColumn
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
            SourceOnlyChanges = changes.Where(c => c.ChangeType is SqlChangeType.CreateTable or SqlChangeType.AddColumn).ToList(),
            TargetOnlyChanges = changes.Where(c => c.ChangeType is SqlChangeType.DropTable or SqlChangeType.DropColumn).ToList(),
            ModifiedChanges = changes.Where(c => c.ChangeType is SqlChangeType.ModifyColumn or SqlChangeType.AlterTable).ToList(),
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
    /// <exception cref="ArgumentNullException"><paramref name="engine"/> or <paramref name="baseToSource"/> or <paramref name="baseToTarget"/> is <see langword="null"/></exception>
    public static ThreeWayDiffResult CreateThreeWayDiff(
        this VisualDiffOutputTests engine,
        SchemaDiffResult baseToSource,
        SchemaDiffResult baseToTarget,
        IReadOnlyList<MergeConflictRegion>? conflictRegions = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(baseToSource);
        ArgumentNullException.ThrowIfNull(baseToTarget);

        return new ThreeWayDiffResult
        {
            Id = Guid.NewGuid(),
            BaseLabel = DefaultBaseLabel,
            SourceLabel = DefaultSourceLabel,
            TargetLabel = DefaultTargetLabel,
            BaseToSource = baseToSource,
            BaseToTarget = baseToTarget,
            ConflictRegions = conflictRegions ?? []
        };
    }

    /// <summary>
    /// Creates a simple merge conflict region for testing resolution strategies.
    /// </summary>
    /// <param name="engine">The test engine instance.</param>
    /// <param name="description">Description of the conflict.</param>
    /// <param name="sourceContent">Content for the source side.</param>
    /// <param name="targetContent">Content for the target side.</param>
    /// <param name="conflictId">Optional conflict ID (will generate one if null).</param>
    /// <returns>A new <see cref="MergeConflictRegion"/> with the specified content.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="engine"/> is <see langword="null"/></exception>
    public static MergeConflictRegion CreateConflictRegion(
        this VisualDiffOutputTests engine,
        string? description = null,
        string? sourceContent = null,
        string? targetContent = null,
        Guid? conflictId = null)
    {
        ArgumentNullException.ThrowIfNull(engine);

        var id = conflictId ?? Guid.NewGuid();
        var content = string.IsNullOrEmpty(sourceContent) && string.IsNullOrEmpty(targetContent)
            ? "shared content"
            : sourceContent ?? targetContent ?? "shared content";

        var sharedLine = new DiffLine(DiffLineKind.Unchanged, 1, content);

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
    /// <exception cref="ArgumentNullException"><paramref name="engine"/> is <see langword="null"/></exception>
    public static MergeResolutionPlan CreateResolutionPlan(
        this VisualDiffOutputTests engine,
        MergeResolutionStrategy strategy,
        params Guid[] conflictIds)
    {
        ArgumentNullException.ThrowIfNull(engine);

        var resolutions = new Dictionary<Guid, MergeResolutionStrategy>(conflictIds.Length);
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
    /// <exception cref="ArgumentNullException"><paramref name="engine"/> or <paramref name="result"/> is <see langword="null"/></exception>
    public static void ShouldBeIdentical(
        this VisualDiffOutputTests engine,
        SchemaDiffResult result,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(result);

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
    /// <exception cref="ArgumentNullException"><paramref name="engine"/> or <paramref name="result"/> is <see langword="null"/></exception>
    public static void ShouldHaveChanges(
        this VisualDiffOutputTests engine,
        SchemaDiffResult result,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(result);

        result.IsIdentical.Should().BeFalse(because ?? "Expected diff result to have changes");
        result.HasChanges().Should().BeTrue(because ?? "Expected diff result to have changes");
    }

    /// <summary>
    /// Asserts that a <see cref="ThreeWayDiffResult"/> has no unresolved conflicts.
    /// </summary>
    /// <param name="engine">The test engine instance.</param>
    /// <param name="result">The three-way diff result to assert.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <exception cref="ArgumentNullException"><paramref name="engine"/> or <paramref name="result"/> is <see langword="null"/></exception>
    public static void ShouldBeFullyResolved(
        this VisualDiffOutputTests engine,
        ThreeWayDiffResult result,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(result);

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
    /// <exception cref="ArgumentNullException"><paramref name="engine"/> or <paramref name="plan"/> is <see langword="null"/></exception>
    public static void ShouldResolveWithStrategy(
        this VisualDiffOutputTests engine,
        MergeResolutionPlan plan,
        MergeResolutionStrategy strategy,
        Guid conflictId,
        string? because = null)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(plan);

        plan.Resolutions.Should().ContainKey(conflictId, because ?? "Expected resolution plan to contain conflict ID");
        plan.Resolutions[conflictId].Should().Be(strategy, because ?? "Expected specific resolution strategy");
    }

    /// <summary>
    /// Gets the total number of changes in a <see cref="SchemaDiffResult"/>.
    /// </summary>
    /// <param name="result">The diff result.</param>
    /// <returns>Total number of changes (added + removed + modified).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/></exception>
    public static int TotalChanges(this SchemaDiffResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.TotalAdded + result.TotalRemoved + result.ModifiedChanges.Count;
    }

    /// <summary>
    /// Determines whether a <see cref="SchemaDiffResult"/> has any changes at all.
    /// </summary>
    /// <param name="result">The diff result.</param>
    /// <returns>True if the result has any changes; otherwise false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/></exception>
    public static bool HasChanges(this SchemaDiffResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return !result.IsIdentical;
    }

    /// <summary>
    /// Gets the count of conflict regions with a specific resolution strategy.
    /// </summary>
    /// <param name="plan">The resolution plan.</param>
    /// <param name="strategy">The strategy to count.</param>
    /// <returns>Number of regions resolved with the specified strategy.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plan"/> is <see langword="null"/></exception>
    public static int CountResolvedWithStrategy(
        this MergeResolutionPlan plan,
        MergeResolutionStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return plan.CountByStrategy(strategy);
    }
}
