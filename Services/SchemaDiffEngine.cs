#nullable enable
using EfMigrationDiff.Configuration;
using EfMigrationDiff.Interfaces;
using EfMigrationDiff.Models;
using Microsoft.Extensions.Logging;

namespace EfMigrationDiff.Services;

/// <summary>
/// Computes visual schema diffs and orchestrates three-way merge operations between
/// Entity Framework migration branches.
/// Implements both <see cref="ISchemaDiffEngine"/> for diff computation and
/// <see cref="IMergeEditor"/> for resolution planning, keeping all merge logic cohesive.
/// </summary>
public sealed class SchemaDiffEngine : ISchemaDiffEngine, IMergeEditor
{
    private readonly ConflictDetectionService _conflictDetection;
    private readonly ILogger<SchemaDiffEngine> _logger;

    /// <summary>
    /// Initialises a new instance with the required conflict detection dependency.
    /// </summary>
    /// <param name="conflictDetection">Service used to detect conflicts between schema changes.</param>
    /// <param name="logger">Logger instance.</param>
    public SchemaDiffEngine(ConflictDetectionService conflictDetection, ILogger<SchemaDiffEngine> logger)
    {
        ArgumentNullException.ThrowIfNull(conflictDetection);
        ArgumentNullException.ThrowIfNull(logger);
        _conflictDetection = conflictDetection;
        _logger = logger;
    }

    // =========================================================================
    // ISchemaDiffEngine
    // =========================================================================

    /// <inheritdoc />
    public SchemaDiffResult ComputeDiff(
        IReadOnlyList<SchemaChange> sourceChanges,
        IReadOnlyList<SchemaChange> targetChanges,
        SchemaDiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(sourceChanges);
        ArgumentNullException.ThrowIfNull(targetChanges);
        ArgumentNullException.ThrowIfNull(options);
        _logger.LogInformation("Computing schema diff with {SourceChangeCount} source changes and {TargetChangeCount} target changes", sourceChanges.Count, targetChanges.Count);;
        options ??= SchemaDiffOptions.Default;

        var sourceLines = ProjectToLines(sourceChanges, options);
        var targetLines = ProjectToLines(targetChanges, options);
        var hunks       = BuildHunks(sourceLines, targetLines);

        // Key: "TableName|ChangeType|ColumnName"
        var targetIndex = targetChanges
            .GroupBy(t => ChangeKey(t))
            .ToDictionary(g => g.Key, g => g.First());

        var sourceOnly = sourceChanges
            .Where(s => !targetIndex.ContainsKey(ChangeKey(s)))
            .ToList();

        var targetOnly = targetChanges
            .Where(t => !sourceChanges.Any(s =>
                string.Equals(s.TableName, t.TableName, StringComparison.OrdinalIgnoreCase) &&
                s.ChangeType == t.ChangeType))
            .ToList();

        var modified = sourceChanges
            .Where(s => targetIndex.TryGetValue(ChangeKey(s), out var t) &&
                        !string.Equals(s.Sql, t.Sql, StringComparison.Ordinal))
            .ToList();

        return new SchemaDiffResult
        {
            Id                = Guid.NewGuid(),
            SourceLabel       = options.SourceLabel,
            TargetLabel       = options.TargetLabel,
            Hunks             = hunks,
            SourceOnlyChanges = sourceOnly,
            TargetOnlyChanges = targetOnly,
            ModifiedChanges   = modified
        };
    }

    /// <inheritdoc />
    public ThreeWayDiffResult ComputeThreeWayDiff(
        IReadOnlyList<SchemaChange> baseChanges,
        IReadOnlyList<SchemaChange> sourceChanges,
        IReadOnlyList<SchemaChange> targetChanges,
        SchemaDiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(baseChanges);
        ArgumentNullException.ThrowIfNull(sourceChanges);
        ArgumentNullException.ThrowIfNull(targetChanges);
        ArgumentNullException.ThrowIfNull(options);
        options ??= SchemaDiffOptions.Default;

        var baseToSource = ComputeDiff(baseChanges, sourceChanges,
            options with { SourceLabel = options.BaseLabel, TargetLabel = options.SourceLabel });

        var baseToTarget = ComputeDiff(baseChanges, targetChanges,
            options with { SourceLabel = options.BaseLabel });

        var conflictRegions = BuildConflictRegions(baseChanges, sourceChanges, targetChanges);

        return new ThreeWayDiffResult
        {
            Id              = Guid.NewGuid(),
            BaseLabel       = options.BaseLabel,
            SourceLabel     = options.SourceLabel,
            TargetLabel     = options.TargetLabel,
            BaseToSource    = baseToSource,
            BaseToTarget    = baseToTarget,
            ConflictRegions = conflictRegions
        };
    }

    /// <inheritdoc />
    public SchemaMergeResult ApplyMergeResolution(ThreeWayDiffResult diff, MergeResolutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(diff);
        ArgumentNullException.ThrowIfNull(plan);
        var resolved   = new List<SchemaChange>();
        var warnings   = new List<string>();
        int unresolved = 0;

        foreach (var region in diff.ConflictRegions)
        {
            if (!plan.Resolutions.TryGetValue(region.Id, out var strategy) ||
                strategy == MergeResolutionStrategy.Unresolved)
            {
                unresolved++;
                warnings.Add($"No resolution provided for conflict: {region.Description}");
                continue;
            }

            switch (strategy)
            {
                case MergeResolutionStrategy.AcceptSource:
                    resolved.AddRange(LinesToChanges(region.SourceLines, strategy.ToString()));
                    break;

                case MergeResolutionStrategy.AcceptTarget:
                    resolved.AddRange(LinesToChanges(region.TargetLines, strategy.ToString()));
                    break;

                case MergeResolutionStrategy.AcceptBoth:
                    resolved.AddRange(LinesToChanges(region.SourceLines, strategy.ToString()));
                    resolved.AddRange(LinesToChanges(region.TargetLines, strategy.ToString()));
                    break;

                case MergeResolutionStrategy.Custom
                    when plan.CustomContent.TryGetValue(region.Id, out var custom):
                    resolved.Add(new SchemaChange(string.Empty, SqlChangeType.Unknown, custom)
                    {
                        TableName = region.Description
                    });
                    break;

                default:
                    warnings.Add($"Unhandled strategy '{strategy}' for region: {region.Description}");
                    break;
            }
        }

        return new SchemaMergeResult(
            IsSuccessful:    unresolved == 0,
            ResolvedChanges: resolved,
            UnresolvedCount: unresolved,
            Warnings:        warnings);
    }

    // =========================================================================
    // IMergeEditor
    // =========================================================================

    /// <inheritdoc />
    public MergeResolutionPlan AcceptSource(ThreeWayDiffResult diff)
    {
        ArgumentNullException.ThrowIfNull(diff);
        return BuildUniformPlan(diff, MergeResolutionStrategy.AcceptSource);
    }

    /// <inheritdoc />
    public MergeResolutionPlan AcceptTarget(ThreeWayDiffResult diff)
    {
        ArgumentNullException.ThrowIfNull(diff);
        return BuildUniformPlan(diff, MergeResolutionStrategy.AcceptTarget);
    }

    /// <inheritdoc />
    public MergeResolutionPlan AutoMerge(ThreeWayDiffResult diff)
    {
        ArgumentNullException.ThrowIfNull(diff);
        var plan = new MergeResolutionPlan();

        foreach (var region in diff.ConflictRegions)
        {
            plan.Resolutions[region.Id] = region.IsTriviallyResolvable
                ? MergeResolutionStrategy.AcceptSource
                : MergeResolutionStrategy.Unresolved;
        }

        return plan;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ValidateResolution(MergeResolutionPlan plan, ThreeWayDiffResult context)
    {
        var errors = new List<string>();

        foreach (var region in context.ConflictRegions)
        {
            if (!plan.Resolutions.TryGetValue(region.Id, out var strategy))
            {
                errors.Add($"Missing resolution for region [{region.Id}]: {region.Description}");
                continue;
            }

            if (strategy == MergeResolutionStrategy.Unresolved)
                errors.Add($"Region [{region.Id}] is still marked as Unresolved: {region.Description}");

            if (strategy == MergeResolutionStrategy.Custom &&
                !plan.CustomContent.ContainsKey(region.Id))
            {
                errors.Add($"Custom strategy requires custom content for region [{region.Id}].");
            }
        }

        return errors;
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    private static string ChangeKey(SchemaChange c) =>
        $"{c.TableName}|{(int)c.ChangeType}|{c.ColumnName}";

    private static List<string> ProjectToLines(
        IReadOnlyList<SchemaChange> changes,
        SchemaDiffOptions opts)
    {
        var lines = new List<string>(changes.Count * 3);

        foreach (var c in changes.OrderBy(x => x.TableName).ThenBy(x => (int)x.ChangeType))
        {
            var content = opts.IgnoreWhitespace
                ? c.GetDescription().Trim()
                : c.GetDescription();

            lines.Add($"[{c.ChangeType}] {content}");

            if (!string.IsNullOrWhiteSpace(c.ColumnName))
                lines.Add($"  Column: {c.ColumnName}");

            if (opts.IncludeSqlContent && !string.IsNullOrWhiteSpace(c.Sql))
                lines.Add($"  SQL: {(opts.IgnoreWhitespace ? c.Sql.Trim() : c.Sql)}");

            if (opts.IncludeMetadata)
            {
                if (c.OldValue is not null) lines.Add($"  Old: {c.OldValue}");
                if (c.NewValue is not null) lines.Add($"  New: {c.NewValue}");
            }
        }

        return lines;
    }

    private static List<DiffHunk> BuildHunks(List<string> source, List<string> target)
    {
        var edits     = ComputeEditScript(source, target);
        var hunkLines = new List<DiffLine>();
        int? hunkSStart = null, hunkTStart = null;
        int sln = 1, tln = 1;

        foreach (var (kind, content) in edits)
        {
            switch (kind)
            {
                case DiffLineKind.Unchanged:
                    if (hunkLines.Any(l => l.IsChanged))
                        hunkLines.Add(new DiffLine(kind, sln, content, tln));
                    sln++;
                    tln++;
                    break;

                case DiffLineKind.Removed:
                    hunkSStart ??= sln;
                    hunkTStart ??= tln;
                    hunkLines.Add(new DiffLine(kind, sln, content));
                    sln++;
                    break;

                case DiffLineKind.Added:
                    hunkSStart ??= sln;
                    hunkTStart ??= tln;
                    hunkLines.Add(new DiffLine(kind, tln, content));
                    tln++;
                    break;
            }
        }

        return hunkLines.Any(l => l.IsChanged)
            ? [new DiffHunk(hunkSStart ?? 1, hunkTStart ?? 1, hunkLines)]
            : [];
    }

    /// <summary>
    /// Builds an edit script from <paramref name="source"/> to <paramref name="target"/> using
    /// a standard LCS dynamic-programming approach. O(m×n) time and space — acceptable for
    /// migration files which are typically a few hundred lines at most.
    /// </summary>
    private static List<(DiffLineKind Kind, string Content)> ComputeEditScript(
        IReadOnlyList<string> source,
        IReadOnlyList<string> target)
    {
        int m = source.Count, n = target.Count;
        var dp = new int[m + 1, n + 1];

        for (int i = 1; i <= m; i++)
        for (int j = 1; j <= n; j++)
        {
            dp[i, j] = string.Equals(source[i - 1], target[j - 1], StringComparison.Ordinal)
                ? dp[i - 1, j - 1] + 1
                : Math.Max(dp[i - 1, j], dp[i, j - 1]);
        }

        var script = new List<(DiffLineKind, string)>();
        int si = m, ti = n;

        while (si > 0 || ti > 0)
        {
            if (si > 0 && ti > 0 &&
                string.Equals(source[si - 1], target[ti - 1], StringComparison.Ordinal))
            {
                script.Add((DiffLineKind.Unchanged, source[si - 1]));
                si--; ti--;
            }
            else if (ti > 0 && (si == 0 || dp[si, ti - 1] >= dp[si - 1, ti]))
            {
                script.Add((DiffLineKind.Added, target[ti - 1]));
                ti--;
            }
            else
            {
                script.Add((DiffLineKind.Removed, source[si - 1]));
                si--;
            }
        }

        script.Reverse();
        return script;
    }

    private IReadOnlyList<MergeConflictRegion> BuildConflictRegions(
        IReadOnlyList<SchemaChange> baseChanges,
        IReadOnlyList<SchemaChange> sourceChanges,
        IReadOnlyList<SchemaChange> targetChanges)
    {
        var conflicts = _conflictDetection.DetectConflicts(
            sourceChanges.ToList(),
            targetChanges.ToList());

        var regions = new List<MergeConflictRegion>(conflicts.Count);
        int index   = 0;

        foreach (var conflict in conflicts)
        {
            var srcLines = sourceChanges
                .Where(c => c.MigrationId == conflict.FirstMigrationId)
                .Select((c, i) => new DiffLine(DiffLineKind.Added, i + 1, c.GetDescription()))
                .ToList();

            var tgtLines = targetChanges
                .Where(c => c.MigrationId == conflict.SecondMigrationId)
                .Select((c, i) => new DiffLine(DiffLineKind.Added, i + 1, c.GetDescription()))
                .ToList();

            var baseLines = baseChanges
                .Where(c => srcLines.Any(l =>
                    l.Content.Contains(c.TableName, StringComparison.OrdinalIgnoreCase)))
                .Select((c, i) => new DiffLine(DiffLineKind.Unchanged, i + 1, c.GetDescription()))
                .ToList();

            regions.Add(new MergeConflictRegion
            {
                Id          = Guid.NewGuid(),
                HunkIndex   = index++,
                Description = string.IsNullOrWhiteSpace(conflict.Description)
                                  ? conflict.GetTitle()
                                  : conflict.Description,
                SourceLines = srcLines,
                TargetLines = tgtLines,
                BaseLines   = baseLines
            });
        }

        return regions;
    }

    private static MergeResolutionPlan BuildUniformPlan(
        ThreeWayDiffResult diff,
        MergeResolutionStrategy strategy)
    {
        var plan = new MergeResolutionPlan();
        foreach (var region in diff.ConflictRegions)
            plan.Resolutions[region.Id] = strategy;
        return plan;
    }

    private static IEnumerable<SchemaChange> LinesToChanges(
        IReadOnlyList<DiffLine> lines,
        string migrationId) =>
        lines
            .Where(l => l.Kind == DiffLineKind.Added && !string.IsNullOrWhiteSpace(l.Content))
            .Select(l => new SchemaChange(migrationId, SqlChangeType.Unknown, l.Content)
            {
                TableName = l.Content.Trim()
            });
}
