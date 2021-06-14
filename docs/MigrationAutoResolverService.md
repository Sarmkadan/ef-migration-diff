# MigrationAutoResolverService

Central service for automatically resolving differences between Entity Framework Core migrations. It analyzes pending migrations, applies conflict resolution strategies, and produces a merge result indicating whether migrations can be applied directly or require manual intervention.

## API

### `public MigrationAutoResolverService`

Initializes a new instance of the resolver service. The service is configured with default merge strategies and ready for use.

### `public async Task<MergeResult> ResolveAsync`

Resolves migration differences using the current strategy configuration. The operation is asynchronous to allow for I/O-bound conflict detection and resolution.

- **Returns**: `Task<MergeResult>` – A task that completes with a `MergeResult` indicating the outcome of the resolution (e.g., `MergeStrategyResult.Success`, `MergeStrategyResult.Conflict`, etc.).
- **Throws**: `InvalidOperationException` – If no merge strategy is configured via `ConfigureStrategy` and the default strategy is unavailable.

### `public void ConfigureStrategy(MergeStrategy? strategy)`

Sets the merge resolution strategy used by `ResolveAsync`. Passing `null` disables automatic resolution, forcing manual handling of migration conflicts.

- **Parameters**:
  - `strategy` (`MergeStrategy?`) – The strategy to use for resolving migration conflicts, or `null` to disable automatic resolution.

### `public MergeStrategy? GetStrategy()`

Retrieves the currently configured merge strategy.

- **Returns**: `MergeStrategy?` – The currently active strategy, or `null` if none is set.

## Usage
