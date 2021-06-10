# CommandExecutor
The `CommandExecutor` type provides a fluent API for registering command handlers and middleware, executing commands asynchronously, and inspecting the outcome of the last execution. It is intended to encapsulate the invocation logic for migration‑diff operations while allowing extensibility through middleware that can short‑circuit or continue the pipeline.

## API
### CommandExecutor()
Initializes a new instance with an empty command registry and no middleware.

### RegisterCommand
Registers a command handler with the executor.  
- **Purpose:** Associates a command name with a delegate that performs the work when the command is executed.  
- **Parameters:** (as defined by the method signature) – typically a command identifier and a handler delegate.  
- **Return value:** The same `CommandExecutor` instance to allow fluent chaining.  
- **Throws:** `ArgumentNullException` if the command name or handler is null; `InvalidOperationException` if a command with the same name is already registered.

### RegisterMiddleware
Registers a middleware component in the execution pipeline.  
- **Purpose:** Inserts a delegate that can inspect, modify, or short‑circuit command execution.  
- **Parameters:** (as defined by the method signature) – usually a middleware delegate.  
- **Return value:** The same `CommandExecutor` instance for fluent chaining.  
- **Throws:** `ArgumentNullException` if the middleware delegate is null.

### ExecuteAsync
Asynchronously executes a registered command.  
- **Purpose:** Looks up the command by name, invokes any registered middleware in order, then invokes the command handler, returning a result object.  
- **Parameters:** (as defined by the method signature) – typically the command name and optional arguments.  
- **Return value:** A `Task<CommandResult>` that completes with the outcome of the execution.  
- **Throws:**  
  - `KeyNotFoundException` if no command with the given name is registered.  
  - Any exception thrown by the middleware or command handler is propagated wrapped in the returned `CommandResult` (the task itself does not fault unless the delegate throws before the result is constructed).

### GetRegisteredCommandCount
Retrieves the number of commands currently registered.  
- **Purpose:** Provides a quick count for diagnostics or validation.  
- **Parameters:** None.  
- **Return value:** An `int` representing the total distinct command names registered.  
- **Throws:** None.

### GetRegisteredCommandNames
Enumerates the names of all registered commands.  
- **Purpose:** Allows callers to discover available commands.  
- **Parameters:** None.  
- **Return value:** An `IEnumerable<string>` yielding each registered command name.  
- **Throws:** None.

### Success
Gets a value indicating whether the last executed command succeeded.  
- **Purpose:** Reflects the `Success` property of the most recent `CommandResult`.  
- **Return value:** `true` if the last command’s `Success` flag was set; otherwise `false`.  
- **Throws:** None.

### Message
Gets the informational or error message from the last executed command.  
- **Purpose:** Mirrors the `Message` property of the most recent `CommandResult`.  
- **Return value:** A `string` containing the message; may be empty or null if no message was provided.  
- **Throws:** None.

### ExitCode
Gets the exit code from the last executed command.  
- **Purpose:** Mirrors the `ExitCode` property of the most recent `CommandResult`.  
- **Return value:** An `int` exit code; conventionally `0` for success.  
- **Throws:** None.

### Data
Gets optional payload data from the last executed command.  
- **Purpose:** Mirrors the `Data` property of the most recent `CommandResult`.  
- **Return value:** An `object?` that may contain any result data supplied by the command handler; null if no data was provided.  
- **Throws:** None.

### IsShortCircuited
Gets a value indicating whether the last execution was short‑short‑short‑circuited by middleware.  
- **Purpose:** Reflects whether a middleware returned `ShortCircuit`, preventing the command handler from running.  
- **Return value:** `true` if the pipeline was short‑circuited; otherwise `false`.  
- **Throws:** None.

### Result
Gets the full `CommandResult` from the last execution.  
- **Purpose:** Provides access to all result properties (`Success`, `Message`, `ExitCode`, `Data`) in a single object.  
- **Return value:** A `CommandResult?` that is null if no command has been executed yet.  
- **Throws:** None.

### CommandExecutor.Ok
Static factory for a successful `CommandResult`.  
- **Purpose:** Creates a `CommandResult` with `Success = true` and default values for other fields.  
- **Return value:** A `CommandResult` instance representing a successful outcome.  
- **Throws:** None.

### CommandExecutor.Error
Static factory for an error `CommandResult`.  
- **Purpose:** Creates a `CommandResult` with `Success = false` and allows setting a message and exit code.  
- **Return value:** A `CommandResult` instance representing a failed outcome.  
- **Throws:** None.

### MiddlewareResult.Continue
Static value indicating that middleware should allow the pipeline to proceed.  
- **Purpose:** Returned by a middleware delegate to signal normal continuation.  
- **Return value:** A `MiddlewareResult` instance.  
- **Throws:** None.

### MiddlewareResult.ShortCircuit
Static value indicating that middleware should halt further processing.  
- **Purpose:** Returned by a middleware delegate to short‑circuit the pipeline, preventing the command handler from executing.  
- **Return value:** A `MiddlewareResult` instance.  
- **Throws:** None.

## Usage
### Example 1: Registering a simple command and executing it
```csharp
using EfMigrationDiff; // namespace containing CommandExecutor

var executor = new CommandExecutor();

// Register a command named "show-version" that returns a success result.
executor.RegisterCommand("show-version", _ =>
{
    return CommandExecutor.Ok with { Message = "Version 1.0.0", Data = new { Version = "1.0.0" } };
});

// Execute the command.
var result = await executor.ExecuteAsync("show-version");

Console.WriteLine($"Success: {result.Success}");
Console.WriteLine($"Message: {result.Message}");
if (result.Data is { } data)
{
    Console.WriteLine($"Data: {System.Text.Json.JsonSerializer.Serialize(data)}");
}
```

### Example 2: Using middleware to short‑circuit a command based on a condition
```csharp
using EfMigrationDiff;

var executor = new CommandExecutor();

// Middleware that blocks execution if a fake "maintenance mode" flag is true.
executor.RegisterMiddleware(context =>
{
    if (context.Items.TryGetValue("MaintenanceMode", out var flag) && (bool)flag)
    {
        return CommandExecutor.Error with { Message = "System is under maintenance.", ExitCode = 503 };
    }
    return MiddlewareResult.Continue;
});

// A command that would normally run.
executor.RegisterCommand("deploy", _ =>
{
    return CommandExecutor.Ok with { Message = "Deployment completed.", Data = new { Timestamp = DateTime.UtcNow } };
});

// Simulate maintenance mode.
var context = new Dictionary<string, object> { ["MaintenanceMode"] = true };
var result = await executor.ExecuteAsync("deploy", context);

Console.WriteLine($"Success: {result.Success}");   // False
Console.WriteLine($"Message: {result.Message}");   // System is under maintenance.
Console.WriteLine($"IsShortCircuited: {executor.IsShortCircuited}"); // True
```

## Notes
- The executor is **not thread‑safe** for concurrent modifications of the command or middleware collections. Registering commands or middleware while another thread is executing `ExecuteAsync` may lead to undefined behavior. It is safe to call `ExecuteAsync` concurrently on different instances or on the same instance after registration has completed.
- Registering a command with a name that already exists throws an `InvalidOperationException`; attempting to register a null handler or middleware throws an `ArgumentNullException`.
- If a middleware throws an exception, the exception is caught and wrapped in a failing `CommandResult` (the task returned by `ExecuteAsync` does not fault). The `IsShortCircuited` flag will be `false` in this case because the pipeline was not intentionally short‑circuited.
- The `Result` property returns `null` until at least one command has been executed; after execution it reflects the most recent outcome, regardless of whether the call was made via `ExecuteAsync` or a synchronous wrapper (if provided).  
- The static `Ok` and `Error` factories are convenience members; they return new instances each time they are invoked, so mutating the returned `CommandResult` does not affect other callers.  
- The `Data` property can hold any object; callers should perform null checks and type casting as appropriate for their scenario.
