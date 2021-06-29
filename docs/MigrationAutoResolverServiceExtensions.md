# MigrationAutoResolverServiceExtensions

Provides extension methods for configuring and invoking a `MigrationAutoResolverService` instance. The methods enable fluent selection of a conflict‑resolution strategy, optional logging, and asynchronous resolution of all pending migrations.

## API

### ConfigureSkipStrategy
```csharp
public static MigrationAutoResolverService ConfigureSkipStrategy(this MigrationAutoResolverService service)
```
**Purpose** – Configures the service to skip conflicting migrations when they are encountered.  
**Parameters**  
- `service` – The `MigrationAutoResolverService` instance to configure.  
**Return value** – The same `MigrationAutoResolverService` instance, allowing further chaining.  
**Exceptions** – Throws `ArgumentNullException` if `service` is `null`.

### ConfigureFirstWinsStrategy
```csharp
public static MigrationAutoResolverService ConfigureFirstWinsStrategy(this MigrationAutoResolverService service)
```
**Purpose** – Configures the service to keep the first migration encountered and discard later conflicting ones.  
**Parameters**  
- `service` – The `MigrationAutoResolverService` instance to configure.  
**Return value** – The same `MigrationAutoResolverService` instance.  
**Exceptions** – Throws `ArgumentNullException` if `service` is `null`.

### ConfigureLastWinsStrategy
```csharp
public static MigrationAutoResolverService ConfigureLastWinsStrategy(this MigrationAutoResolverService service)
```
**Purpose** – Configures the service to keep the last migration encountered and discard earlier conflicting ones.  
**Parameters**  
- `service` – The `MigrationAutoResolverService` instance to configure.  
**Return value** – The same `MigrationAutoResolverService` instance.  
**Exceptions** – Throws `ArgumentNullException` if `service` is `null`.

### ConfigureCombineStrategy
```csharp
public static MigrationAutoResolverService ConfigureCombineStrategy(this MigrationAutoResolverService service)
```
**Purpose** – Configures the service to attempt to combine conflicting migrations when possible.  
**Parameters**  
- `service` – The `MigrationAutoResolverService` instance to configure.  
**Return value** – The same `MigrationAutoResolverService` instance.  
**Exceptions** – Throws `ArgumentNullException` if `service` is `null`.

### TryResolveAllAsync
```csharp
public static async Task<bool> TryResolveAllAsync(this MigrationAutoResolverService service, CancellationToken cancellationToken = default)
```
**Purpose** – Asynchronously attempts to resolve all pending migrations using the currently configured strategy.  
**Parameters**  
- `service` – The `MigrationAutoResolverService` instance to operate on.  
- `cancellationToken` – Optional token to cancel the operation.  
**Return value** – `true` if all migrations were resolved successfully; `false` if any migration could not be resolved according to the strategy.  
**Exceptions** –  
- `ArgumentNullException` if `service` is `null`.  
- `OperationCanceledException` if the operation is cancelled via `cancellationToken`.  
- `InvalidOperationException` if the service has not been configured with a strategy prior to invocation.

### GetConfiguredStrategy
```csharp
public static MergeStrategy GetConfiguredStrategy(this MigrationAutoResolverService service)
```
**Purpose** – Retrieves the merge strategy currently configured on the service.  
**Parameters**  
- `service` – The `MigrationAutoResolverService` instance to query.  
**Return value** – The `MergeStrategy` enum value representing the active strategy.  
**Exceptions** – Throws `ArgumentNullException` if `service` is `null`.

### ResetToDefaults
```csharp
public static MigrationAutoResolverService ResetToDefaults(this MigrationAutoResolverService service)
```
**Purpose** – Resets the service’s configuration to its default state (no explicit strategy selected).  
**Parameters**  
- `service` – The `MigrationAutoResolverService` instance to reset.  
**Return value** – The same `MigrationAutoResolverService` instance, now with default settings.  
**Exceptions** – Throws `ArgumentNullException` if `service` is `null`.

### CreateWithLogger
```csharp
public static MigrationAutoResolverService CreateWithLogger(this MigrationAutoResolverService service, ILogger logger)
```
**Purpose** – Associates an `ILogger` instance with the service for diagnostic output during resolution.  
**Parameters**  
- `service` – The `MigrationAutoResolverService` instance to configure.  
- `logger` – The logger to use; may be `null` to disable logging.  
**Return value** – The same `MigrationAutoResolverService` instance with the logger applied.  
**Exceptions** – Throws `ArgumentNullException` if `service` is `null`.

## Usage

### Example 1: Simple skip strategy
```csharp
using EfMigrationDiff;
using Microsoft.Extensions.Logging;

var resolver = new MigrationAutoResolverService();
resolver.ConfigureSkipStrategy()
        .CreateWithLogger(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MigrationAutoResolverService>());

bool resolved = await resolver.TryResolveAllAsync();
if (!resolved)
{
    // Handle unresolved migrations
    Log.Warning("Some migrations were skipped due to conflicts.");
}
```

### Example 2: Combine strategy with custom cancellation
```csharp
using EfMigrationDiff;
using System.Threading;
using System.Threading.Tasks;

var resolver = new MigrationAutoResolverService()
                .ConfigureCombineStrategy();

var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
try
{
    bool success = await resolver.TryResolveAllAsync(cts.Token);
    if (success)
    {
        Log.Information("All migrations resolved using combine strategy.");
    }
    else
    {
        Log.Warning("Combine strategy could not resolve all migrations.");
    }
}
catch (OperationCanceledException)
{
    Log.Error("Migration resolution timed out.");
}
```

## Notes

- All extension methods are **pure** with respect to the service instance; they do not create new instances unless explicitly noted (e.g., `CreateWithLogger` attaches a logger but returns the same instance).  
- The service is **not thread‑safe** for concurrent configuration or resolution calls. If multiple threads need to use the same service, external synchronization is required.  
- Calling any configuration method after `TryResolveAllAsync` has begun will result in an `InvalidOperationException` because the strategy is considered locked once resolution starts.  
- `ResetToDefaults` clears any previously set strategy and logger, returning the service to the state it had immediately after construction.  
- Passing `null` for the `service` parameter to any extension method will always throw `ArgumentNullException`; null loggers are accepted by `CreateWithLogger` and simply disable logging.  
- The `TryResolveAllAsync` method respects the supplied `CancellationToken`; if cancellation is triggered, the method throws `OperationCanceledException` and no further migration processing occurs.
