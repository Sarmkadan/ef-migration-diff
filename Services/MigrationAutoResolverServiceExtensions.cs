#nullable enable

using EfMigrationDiff.Models;
using Microsoft.Extensions.Logging;

namespace EfMigrationDiff.Services;

/// <summary>
/// Provides useful extension methods for <see cref="MigrationAutoResolverService"/> to simplify common scenarios.
/// </summary>
public static class MigrationAutoResolverServiceExtensions
{
    /// <summary>
    /// Configures the service to skip all conflicts of the specified type.
    /// </summary>
    /// <param name="service">The auto-resolver service instance.</param>
    /// <param name="conflictType">The conflict type to skip.</param>
    /// <returns>The service instance for method chaining.</returns>
    public static MigrationAutoResolverService ConfigureSkipStrategy(
        this MigrationAutoResolverService service,
        ConflictType conflictType)
    {
        service.ConfigureStrategy(conflictType, MergeStrategy.Skip);
        return service;
    }

    /// <summary>
    /// Configures the service to use first-wins strategy for the specified conflict type.
    /// </summary>
    /// <param name="service">The auto-resolver service instance.</param>
    /// <param name="conflictType">The conflict type to resolve with first-wins.</param>
    /// <returns>The service instance for method chaining.</returns>
    public static MigrationAutoResolverService ConfigureFirstWinsStrategy(
        this MigrationAutoResolverService service,
        ConflictType conflictType)
    {
        service.ConfigureStrategy(conflictType, MergeStrategy.FirstWins);
        return service;
    }

    /// <summary>
    /// Configures the service to use last-wins strategy for the specified conflict type.
    /// </summary>
    /// <param name="service">The auto-resolver service instance.</param>
    /// <param name="conflictType">The conflict type to resolve with last-wins.</param>
    /// <returns>The service instance for method chaining.</returns>
    public static MigrationAutoResolverService ConfigureLastWinsStrategy(
        this MigrationAutoResolverService service,
        ConflictType conflictType)
    {
        service.ConfigureStrategy(conflictType, MergeStrategy.LastWins);
        return service;
    }

    /// <summary>
    /// Configures the service to combine both source and target SQL for the specified conflict type.
    /// </summary>
    /// <param name="service">The auto-resolver service instance.</param>
    /// <param name="conflictType">The conflict type to resolve with combine strategy.</param>
    /// <returns>The service instance for method chaining.</returns>
    public static MigrationAutoResolverService ConfigureCombineStrategy(
        this MigrationAutoResolverService service,
        ConflictType conflictType)
    {
        service.ConfigureStrategy(conflictType, MergeStrategy.Combine);
        return service;
    }

    /// <summary>
    /// Resolves conflicts asynchronously and returns true if all conflicts were successfully resolved.
    /// </summary>
    /// <param name="service">The auto-resolver service instance.</param>
    /// <param name="conflicts">The collection of conflicts to resolve.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>True if all conflicts were resolved; otherwise false.</returns>
    public static async Task<bool> TryResolveAllAsync(
        this MigrationAutoResolverService service,
        IEnumerable<ConflictInfo> conflicts,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ResolveAsync(conflicts, cancellationToken);
        return !result.HasBlockingConflicts && result.UnresolvedCount == 0;
    }

    /// <summary>
    /// Gets the currently configured strategy for a conflict type, or the default strategy if not configured.
    /// </summary>
    /// <param name="service">The auto-resolver service instance.</param>
    /// <param name="conflictType">The conflict type to query.</param>
    /// <returns>The configured strategy, or the default strategy for the conflict type.</returns>
    public static MergeStrategy GetConfiguredStrategy(
        this MigrationAutoResolverService service,
        ConflictType conflictType)
    {
        return service.GetStrategy(conflictType) ?? GetDefaultStrategy(conflictType);
    }

    /// <summary>
    /// Resets all strategies to their default values.
    /// </summary>
    /// <param name="service">The auto-resolver service instance.</param>
    /// <returns>The service instance for method chaining.</returns>
    public static MigrationAutoResolverService ResetToDefaults(
        this MigrationAutoResolverService service)
    {
        // Rebuild the default strategy map
        var loggerField = typeof(MigrationAutoResolverService)
            .GetField("_logger", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var logger = loggerField?.GetValue(service) as ILogger<MigrationAutoResolverService>;

        var strategyMapField = typeof(MigrationAutoResolverService)
            .GetField("_strategyMap", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        var newStrategyMap = BuildDefaultStrategyMap();
        strategyMapField?.SetValue(service, newStrategyMap);

        logger?.LogInformation("Auto-resolver strategies reset to defaults.");
        return service;
    }

    /// <summary>
    /// Gets the default strategy for a given conflict type based on the service's default mapping.
    /// </summary>
    /// <param name="conflictType">The conflict type.</param>
    /// <returns>The default strategy.</returns>
    private static MergeStrategy GetDefaultStrategy(ConflictType conflictType)
    {
        var defaultMap = BuildDefaultStrategyMap();
        return defaultMap.TryGetValue(conflictType, out var strategy) ? strategy : MergeStrategy.Skip;
    }

    /// <summary>
    /// Builds the default strategy map (copied from MigrationAutoResolverService for extension use).
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

    /// <summary>
    /// Creates a new <see cref="MigrationAutoResolverService"/> with the specified logger factory.
    /// </summary>
    /// <param name="loggerFactory">The logger factory to use.</param>
    /// <returns>A new instance of <see cref="MigrationAutoResolverService"/>.</returns>
    public static MigrationAutoResolverService CreateWithLogger(
        ILoggerFactory loggerFactory)
    {
        return new MigrationAutoResolverService(loggerFactory.CreateLogger<MigrationAutoResolverService>());
    }
}