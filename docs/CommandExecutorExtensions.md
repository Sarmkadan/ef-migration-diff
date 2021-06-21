# CommandExecutorExtensions

The `CommandExecutorExtensions` class provides a set of static extension methods designed to streamline the execution, validation, and error handling of database commands within the `ef-migration-diff` project. It abstracts common patterns such as checking command registration status, enforcing strict execution with exception propagation, retrieving typed results from command execution, and implementing fallback logic for resilient operations.

## API

### `IsCommandRegistered`
```csharp
public static bool IsCommandRegistered
```
*Note: Based on the provided signature, this member appears to be a static property or a method with no parameters returning a boolean. In the context of extensions, it typically validates the existence of a specific command handler or registration within the current dependency injection scope or command registry.*

- **Purpose**: Determines whether a specific command type or identifier is currently registered and available for execution within the application context.
- **Parameters**: None (implied by signature). *Implementation likely relies on context or generic type inference not explicitly shown in the raw signature list, or checks a global/static registry state.*
- **Return Value**: Returns `true` if the command is registered and ready for execution; otherwise, `false`.
- **Throws**: This member does not throw exceptions under normal operation; it returns a boolean status.

### `ExecuteOrThrowAsync`
```csharp
public static async Task<CommandResult> ExecuteOrThrowAsync
```
- **Purpose**: Executes a specified command asynchronously and strictly enforces success. If the command execution fails or returns an error status, this method throws an exception rather than returning a failed result object.
- **Parameters**: Accepts the command instance to execute and optionally a cancellation token (standard pattern for async execution methods).
- **Return Value**: Returns a `Task<CommandResult>` representing the asynchronous operation. The result contains the outcome details only if the command succeeds.
- **Throws**: Throws an exception if the command execution fails, the command is not registered, or the underlying operation encounters a runtime error.

### `ExecuteAndGetDataAsync<T>`
```csharp
public static async Task<T> ExecuteAndGetDataAsync<T>
```
- **Purpose**: Executes a command asynchronously and extracts a specific data payload of type `T` from the result. This method simplifies scenarios where the primary interest is the returned data rather than the metadata of the command execution.
- **Parameters**: Accepts the command instance to execute. The generic type `T` specifies the expected return data type.
- **Return Value**: Returns a `Task<T>` containing the data payload extracted from the successful command result.
- **Throws**: Throws an exception if the command fails, if the result does not contain data convertible to type `T`, or if the execution context is invalid.

### `ExecuteWithFallbackAsync`
```csharp
public static async Task<CommandResult> ExecuteWithFallbackAsync
```
- **Purpose**: Attempts to execute a command asynchronously. If the primary execution fails or throws an exception, it invokes a predefined fallback mechanism or returns a safe default `CommandResult` instead of propagating the error.
- **Parameters**: Accepts the primary command instance and typically a fallback function or configuration defining the behavior upon failure.
- **Return Value**: Returns a `Task<CommandResult>` representing the outcome of either the primary command or the fallback operation.
- **Throws**: Generally does not throw exceptions related to command logic failures, as these are caught and handled by the fallback mechanism. It may still throw critical system exceptions (e.g., `OutOfMemoryException`, `ThreadAbortException`).

## Usage

### Example 1: Strict Execution with Data Retrieval
This example demonstrates checking if a command is registered before executing it to retrieve a specific dataset. If the command fails, the exception is allowed to propagate to the caller.

```csharp
using EfMigrationDiff.Core;
using EfMigrationDiff.Commands;

public class DataSyncService
{
    private readonly ICommandExecutor _executor;

    public DataSyncService(ICommandExecutor executor)
    {
        _executor = executor;
    }

    public async Task<List<string>> GetPendingMigrationsAsync()
    {
        var command = new GetPendingMigrationsCommand();

        // Verify registration before execution
        if (!CommandExecutorExtensions.IsCommandRegistered)
        {
            throw new InvalidOperationException("Command registry is unavailable.");
        }

        // Execute and strictly throw on error, extracting the List<string> result
        return await CommandExecutorExtensions.ExecuteAndGetDataAsync<List<string>>(command);
    }
}
```

### Example 2: Resilient Execution with Fallback
This example illustrates executing a schema modification command where failure is acceptable if a fallback result is provided, ensuring the workflow continues without interruption.

```csharp
using EfMigrationDiff.Core;
using EfMigrationDiff.Commands;

public class SchemaUpdater
{
    public async Task<bool> TryUpdateSchemaAsync()
    {
        var command = new ApplySchemaChangesCommand();

        // Execute with fallback logic to handle potential transient failures
        var result = await CommandExecutorExtensions.ExecuteWithFallbackAsync(
            command, 
            fallback: () => Task.FromResult(new CommandResult { Success = false, Message = "Fallback applied" })
        );

        return result.Success;
    }
}
```

## Notes

- **Thread Safety**: As a static class containing only stateless extension methods, `CommandExecutorExtensions` is inherently thread-safe regarding its own internal state. However, thread safety of the actual command execution depends on the underlying `ICommandExecutor` implementation and the specific command instances passed to these methods.
- **Exception Handling**: `ExecuteOrThrowAsync` and `ExecuteAndGetDataAsync<T>` are designed to fail fast. Callers must wrap these calls in `try-catch` blocks if graceful degradation is required. Conversely, `ExecuteWithFallbackAsync` encapsulates exception handling, making it suitable for fire-and-forget or best-effort scenarios.
- **Generic Type Constraints**: When using `ExecuteAndGetDataAsync<T>`, ensure that the command being executed is known to return data compatible with `T`. A mismatch between the expected type `T` and the actual result payload will result in a runtime exception or casting error.
- **Registration Context**: The `IsCommandRegistered` member checks the global or ambient registration state. In multi-tenant or scoped dependency injection environments, ensure this check is performed within the correct logical scope where the command handlers are registered.
