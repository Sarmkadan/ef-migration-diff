#nullable enable
using EfMigrationDiff.Models;
using Microsoft.Extensions.Logging;

namespace EfMigrationDiff.Services;

/// <summary>
/// Applies configurable merge strategies to automatically resolve migration conflicts
/// wherever safe to do so, surfacing the remainder for manual review.
/// </summary>
/// <remarks>
/// Only conflicts with severity below <see cref="ConflictSeverity.Error"/> are eligible
/// for auto-resolution. Higher-severity conflicts are always forwarded to
/// <see cref="MergeResult.UnresolvedConflicts"/> so that destructive or ambiguous changes
/// are never silently discarded.
/// </remarks>
public class MigrationAutoResolverService
{
    private readonly ILogger<MigrationAutoResolverService> _logger;
    private readonly Dictionary<ConflictType, MergeStrategy> _strategyMap;

    /// <summary>
    /// Initialises the service with the default strategy map.
    /// </summary>
    public MigrationAutoResolverService(ILogger<MigrationAutoResolverService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _strategyMap = BuildDefaultStrategyMap();
    }

    /// <summary>
    /// Attempts to auto-resolve each conflict in <paramref name="conflicts"/> using
    /// the registered merge strategy for its type.
    /// </summary>
    /// <param name="conflicts">The conflicts to process.</param>
    /// <param name="cancellationToken">Token to observe for cooperative cancellation.</param>
    /// <returns>
    /// A <see cref="MergeResult"/> that separates successfully merged conflicts from
    /// those that still require manual intervention.
    /// </returns>
    public async Task<MergeResult> ResolveAsync(
        IEnumerable<ConflictInfo> conflicts,
        CancellationToken cancellationToken = default)
    {
        if (conflicts == null)
        {
            throw new ArgumentNullException(nameof(conflicts));
        }
        var result = new MergeResult();
        var conflictList = conflicts.ToList();

        _logger.LogInformation("Starting auto-resolution for {Count} conflict(s).", conflictList.Count);

        foreach (var conflict in conflictList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var attempt = await ResolveConflictAsync(conflict, cancellationToken);
            result.Attempts.Add(attempt);

            if (attempt.Succeeded)
            {
                conflict.MarkResolved(attempt.StrategyApplied.ToString());
                _logger.LogDebug("Conflict {Id} ({Type}) resolved via {Strategy}.",
                    conflict.Id, conflict.ConflictType, attempt.StrategyApplied);
            }
            else
            {
                result.UnresolvedConflicts.Add(conflict);
                _logger.LogDebug("Conflict {Id} ({Type}) deferred for manual review: {Reason}.",
                    conflict.Id, conflict.ConflictType, attempt.FailureReason);
            }
        }

        _logger.LogInformation(
            "Auto-resolution complete — resolved: {Resolved}, unresolved: {Unresolved}, blocking: {Blocking}.",
            result.ResolvedCount, result.UnresolvedCount, result.HasBlockingConflicts);

        return result;
    }

    /// <summary>
    /// Overrides the default merge strategy for a specific conflict type.
    /// Call this before invoking <see cref="ResolveAsync"/> to customise behaviour.
    /// </summary>
    /// <param name="conflictType">The conflict type whose strategy should be overridden.</param>
    /// <param name="strategy">The strategy to apply for that type.</param>
    public void ConfigureStrategy(ConflictType conflictType, MergeStrategy strategy)
    {
        _strategyMap[conflictType] = strategy;
        _logger.LogDebug("Merge strategy for {Type} updated to {Strategy}.", conflictType, strategy);
    }

    /// <summary>
    /// Returns the currently configured strategy for the given conflict type,
    /// or <c>null</c> if none is registered.
    /// </summary>
    /// <param name="conflictType">The conflict type to query.</param>
    public MergeStrategy? GetStrategy(ConflictType conflictType) =>
        _strategyMap.TryGetValue(conflictType, out var s) ? s : null;

    // -------------------------------------------------------------------------
    // Private resolution pipeline
    // -------------------------------------------------------------------------

    /// <summary>
    /// Evaluates eligibility and applies the registered strategy for a single conflict.
    /// </summary>
    private async Task<MergeAttempt> ResolveConflictAsync(
        ConflictInfo conflict,
        CancellationToken cancellationToken)
    {
        var attempt = new MergeAttempt
        {
            ConflictId = conflict.Id,
            ConflictType = conflict.ConflictType
        };

        if (conflict.IsResolved)
        {
            attempt.Succeeded = false;
            attempt.FailureReason = "Conflict is already marked as resolved.";
            return attempt;
        }

        // Refuse to auto-resolve anything that could cause data loss or deployment failures
        if (conflict.Severity == ConflictSeverity.Error || conflict.Severity == ConflictSeverity.Critical)
        {
            attempt.Succeeded = false;
            attempt.FailureReason = $"Severity '{conflict.Severity}' requires manual resolution.";
            return attempt;
        }

        if (!_strategyMap.TryGetValue(conflict.ConflictType, out var strategy))
        {
            attempt.Succeeded = false;
            attempt.FailureReason = $"No merge strategy registered for conflict type '{conflict.ConflictType}'.";
            return attempt;
        }

        attempt.StrategyApplied = strategy;

        try
        {
            attempt.MergedContent = await ApplyStrategyAsync(conflict, strategy, cancellationToken);
            attempt.Succeeded = true;
        }
        catch (Exception ex)
        {
            attempt.Succeeded = false;
            attempt.FailureReason = ex.Message;
            _logger.LogWarning(ex,
                "Strategy {Strategy} threw an exception while processing conflict {Id}.",
                strategy, conflict.Id);
        }

        return attempt;
    }

    /// <summary>
    /// Dispatches to the appropriate strategy implementation and returns the
    /// resulting SQL fragment (may be <c>null</c> when the operation is dropped).
    /// </summary>
    private Task<string?> ApplyStrategyAsync(
        ConflictInfo conflict,
        MergeStrategy strategy,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? merged = strategy switch
        {
            MergeStrategy.Skip      => ApplySkip(conflict),
            MergeStrategy.FirstWins => ApplyFirstWins(conflict),
            MergeStrategy.LastWins  => ApplyLastWins(conflict),
            MergeStrategy.Combine   => ApplyCombine(conflict),
            _                       => throw new InvalidOperationException(
                                           $"Unhandled merge strategy '{strategy}'.")
        };

        return Task.FromResult(merged);
    }

    private static string? ApplySkip(ConflictInfo conflict)
    {
        conflict.AddDetail("AutoResolution", "Duplicate operation dropped as a safe no-op.");
        return null;
    }

    private static string? ApplyFirstWins(ConflictInfo conflict)
    {
        var sql = conflict.GetDetail("SourceSql");
        conflict.AddDetail("AutoResolution",
            $"Source migration '{conflict.FirstMigrationId}' takes precedence.");
        return sql.Length > 0 ? sql : null;
    }

    private static string? ApplyLastWins(ConflictInfo conflict)
    {
        var sql = conflict.GetDetail("TargetSql");
        conflict.AddDetail("AutoResolution",
            $"Target migration '{conflict.SecondMigrationId}' takes precedence.");
        return sql.Length > 0 ? sql : null;
    }

    private static string? ApplyCombine(ConflictInfo conflict)
    {
        var sourceSql = conflict.GetDetail("SourceSql");
        var targetSql = conflict.GetDetail("TargetSql");

        var parts = new[] { sourceSql, targetSql }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (parts.Count == 0)
            return null;

        conflict.AddDetail("AutoResolution", "Source and target SQL merged sequentially.");
        return string.Join(Environment.NewLine, parts);
    }

    /// <summary>
    /// Default strategy assignments covering common, low-risk conflict patterns.
    /// Table and column conflicts are intentionally excluded — those always need a human.
    /// </summary>
    private static Dictionary<ConflictType, MergeStrategy> BuildDefaultStrategyMap() =>
        new()
        {
            // Duplicate index operations are idempotent — drop the second one
            [ConflictType.IndexConflict] = MergeStrategy.Skip,

            // When two branches add the same constraint, combine both definitions
            [ConflictType.ConstraintConflict] = MergeStrategy.Combine,

            // Ordering mismatches are resolved by deferring to the integration branch
            [ConflictType.OperationConflict] = MergeStrategy.LastWins,

            // Naming conflicts keep the original (source) definition
            [ConflictType.NameConflict] = MergeStrategy.FirstWins,
        };
}
