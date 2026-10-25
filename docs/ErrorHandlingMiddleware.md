# ErrorHandlingMiddleware

`Middleware/ErrorHandlingMiddleware.cs` converts exceptions raised while running a command into a structured `CommandResult` with a message and an exit code. It implements `ICommandMiddleware`, but the pipeline hook does not handle errors; the error mapping lives in `HandleException`.

## Purpose

Commands can fail with many exception types (argument errors, invalid operations, timeouts, unexpected failures). `ErrorHandlingMiddleware` maps each class of exception to a fixed message prefix and a numeric exit code, so callers get a `CommandResult` instead of an unhandled exception.

## Current state

- `HandleException` is the only method that does error mapping. Nothing in the repository calls it. The `Program.cs` compare path and `CommandExecutor` handle their own exceptions with `try`/`catch` blocks and return `CommandResult` values directly.
- `InvokeAsync` is a pass-through. It validates that the context is not null and returns `MiddlewareResult.Continue()`. It never short-circuits and never converts an exception.
- `ConfigurationBuilder.AddErrorHandling(bool includeStackTrace = false)` constructs an instance but does not register it. The builder's own comment says error handling "is typically done in CommandExecutor, not as middleware".

Treat this middleware as a reusable mapping component. It does not change how the CLI reports errors today.

## Constructor

```csharp
public ErrorHandlingMiddleware(bool includeStackTrace = false)
```

- `includeStackTrace` (`bool`, default `false`): when `true`, `HandleException` writes the exception's stack trace to the command context's error output. If the stack trace is `null`, it writes `No stack trace available`.

## `InvokeAsync`

```csharp
public async Task<MiddlewareResult> InvokeAsync(CommandContext context)
```

Part of `ICommandMiddleware`, called by `CommandExecutor` for each registered middleware in order.

- **Parameters**: `context` (`CommandContext`), must not be `null`.
- **Returns**: `MiddlewareResult.Continue()` in all cases.
- **Throws**: `ArgumentNullException` if `context` is `null`.

## `HandleException`

```csharp
public CommandResult HandleException(Exception exception, CommandContext context)
```

Converts an exception into a `CommandResult`.

- **Parameters**
  - `exception` (`Exception`): the exception to map. Must not be `null`.
  - `context` (`CommandContext`): the command context. Must not be `null`. Used only when `includeStackTrace` is `true`.
- **Returns**: a failed `CommandResult` (`Success = false`) with the mapped exit code and message.
- **Throws**: `ArgumentNullException` if `exception` or `context` is `null`.

### Mapping

The first matching case wins:

| Exception type | Exit code | Message prefix |
|---|---|---|
| `ArgumentException`, `ArgumentNullException` (and subclasses) | 400 | `Invalid argument` |
| `InvalidOperationException` | 500 | `Operation failed` |
| `TimeoutException` | 504 | `Operation timed out` |
| Any other exception | 500 | `An unexpected error occurred` |

The message is always `"{prefix}: {exception.Message}"`. For example, `new ArgumentException("bad branch")` produces `Invalid argument: bad branch` with exit code 400.

The numeric codes are HTTP-style values passed straight into `CommandResult.ExitCode`. They do not use `Constants.ExitCodes`, which defines the CLI exit codes (`UsageError = 64`, `Error`, and so on). If this middleware is wired into the CLI, the exit codes would need to be reconciled with `Constants.ExitCodes`.

### Stack trace output

When `includeStackTrace` is `true`, the method calls `context.WriteError` with `exception.StackTrace`, or `No stack trace available` if that is `null`. The stack trace is written after the mapping is computed, and it is not added to the returned `CommandResult.Message`. The `verbose` flag of `CommandExecutor.ExecuteAsync` is not consulted here.

## Error response shape

A returned `CommandResult` has:

- `Success`: `false`
- `Message`: `"{prefix}: {exception.Message}"`
- `ExitCode`: the mapped code from the table above

It is built with the `CommandResult.Error(message, exitCode)` factory.

## Example

```csharp
var middleware = new ErrorHandlingMiddleware(includeStackTrace: false);

CommandResult result = middleware.HandleException(
    new TimeoutException("git fetch took too long"),
    context);

// result.Success  == false
// result.ExitCode == 504
// result.Message  == "Operation timed out: git fetch took too long"
```

## Related

- `CLI/CommandExecutor.cs`: runs the middleware pipeline and contains the exception handling that is used today.
- `CLI/CommandContext.cs`: `WriteError` target for stack traces.
- `Utilities/Constants.cs`: `Constants.ExitCodes` for the CLI exit-code contract.
- `Configuration/ConfigurationBuilder.cs`: `AddErrorHandling`, which currently does not register the middleware.
