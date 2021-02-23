#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.CLI;

namespace EfMigrationDiff.Middleware;

/// <summary>
/// Middleware that wraps command execution in try-catch and provides centralized error handling.
/// Converts exceptions to structured CommandResult with appropriate error codes and messages.
/// </summary>
public class ErrorHandlingMiddleware : ICommandMiddleware
{
    private readonly bool _includeStackTrace;

    public ErrorHandlingMiddleware(bool includeStackTrace = false)
    {
        _includeStackTrace = includeStackTrace;
    }

    public async Task<MiddlewareResult> InvokeAsync(CommandContext context)
    {
        // This middleware doesn't short-circuit; it's meant to be used with exception handling in CommandExecutor
        return MiddlewareResult.Continue();
    }

    /// <summary>
    /// Handles exceptions and converts them to CommandResult with proper formatting and exit codes.
    /// </summary>
    public CommandResult HandleException(Exception exception, CommandContext context)
    {
        var result = exception switch
        {
            ArgumentException or ArgumentNullException => CreateResult(400, "Invalid argument", exception),
            InvalidOperationException => CreateResult(500, "Operation failed", exception),
            TimeoutException => CreateResult(504, "Operation timed out", exception),
            _ => CreateResult(500, "An unexpected error occurred", exception)
        };

        if (_includeStackTrace)
        {
            context.WriteError(exception.StackTrace ?? "No stack trace available");
        }

        return result;
    }

    /// <summary>
    /// Creates a CommandResult from an exception with appropriate exit code.
    /// </summary>
    private CommandResult CreateResult(int exitCode, string message, Exception exception)
    {
        return CommandResult.Error(
            $"{message}: {exception.Message}",
            exitCode);
    }
}
