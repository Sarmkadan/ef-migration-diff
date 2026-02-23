#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.CLI;

/// <summary>
/// Executes registered commands with middleware pipeline support.
/// Allows registration of commands, middleware for request processing, and result formatting.
/// </summary>
public class CommandExecutor
{
    private readonly Dictionary<string, ICommand> _commands = new();
    private readonly List<ICommandMiddleware> _middlewares = new();
    private readonly CommandParser _parser;

    public CommandExecutor(CommandParser? parser = null)
    {
        _parser = parser ?? new CommandParser();
    }

    /// <summary>
    /// Registers a command by name. Commands must implement the ICommand interface.
    /// </summary>
    public CommandExecutor RegisterCommand(string name, ICommand command)
    {
        _commands[name.ToLowerInvariant()] = command;
        return this;
    }

    /// <summary>
    /// Registers middleware to be executed before command execution.
    /// Middleware runs in registration order and can modify context or short-circuit execution.
    /// </summary>
    public CommandExecutor RegisterMiddleware(ICommandMiddleware middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Executes a command by name with the given arguments.
    /// Returns a CommandResult indicating success/failure and any output.
    /// </summary>
    public async Task<CommandResult> ExecuteAsync(
        string commandName,
        string[] args,
        IServiceProvider serviceProvider,
        TextWriter? output = null,
        TextWriter? errorOutput = null)
    {
        var context = _parser.Parse(commandName, args, serviceProvider, output, errorOutput);

        try
        {
            // Execute middleware pipeline
            foreach (var middleware in _middlewares)
            {
                var middlewareResult = await middleware.InvokeAsync(context);
                if (middlewareResult.IsShortCircuited)
                {
                    return middlewareResult.Result ?? new CommandResult
                    {
                        Success = false,
                        Message = "Command execution short-circuited by middleware"
                    };
                }
            }

            // Execute the actual command
            if (!_commands.TryGetValue(commandName.ToLowerInvariant(), out var command))
            {
                return new CommandResult
                {
                    Success = false,
                    Message = $"Unknown command: {commandName}",
                    ExitCode = 1
                };
            }

            var result = await command.ExecuteAsync(context);
            return result;
        }
        catch (Exception ex)
        {
            context.WriteError(ex.Message);
            return new CommandResult
            {
                Success = false,
                Message = ex.Message,
                ExitCode = 1
            };
        }
    }

    /// <summary>
    /// Gets the count of registered commands.
    /// </summary>
    public int GetRegisteredCommandCount()
    {
        return _commands.Count;
    }

    /// <summary>
    /// Gets all registered command names.
    /// </summary>
    public IEnumerable<string> GetRegisteredCommandNames()
    {
        return _commands.Keys;
    }
}

/// <summary>
/// Interface for command implementations. Commands handle specific CLI operations.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Executes the command with the provided context and returns a result.
    /// </summary>
    Task<CommandResult> ExecuteAsync(CommandContext context);

    /// <summary>
    /// Gets a description of the command for help text.
    /// </summary>
    string GetDescription();
}

/// <summary>
/// Interface for middleware that processes command context before execution.
/// Allows validation, logging, authorization, and request transformation.
/// </summary>
public interface ICommandMiddleware
{
    /// <summary>
    /// Processes the command context. Can modify context or return early with a result.
    /// </summary>
    Task<MiddlewareResult> InvokeAsync(CommandContext context);
}

/// <summary>
/// Represents the result of a command execution.
/// </summary>
public class CommandResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ExitCode { get; set; } = 0;
    public object? Data { get; set; }

    /// <summary>
    /// Creates a successful result with optional data.
    /// </summary>
    public static CommandResult Ok(string message = "Success", object? data = null)
    {
        return new CommandResult
        {
            Success = true,
            Message = message,
            Data = data,
            ExitCode = 0
        };
    }

    /// <summary>
    /// Creates a failure result with exit code.
    /// </summary>
    public static CommandResult Error(string message, int exitCode = 1)
    {
        return new CommandResult
        {
            Success = false,
            Message = message,
            ExitCode = exitCode
        };
    }
}

/// <summary>
/// Result from middleware processing. Controls whether command execution continues.
/// </summary>
public class MiddlewareResult
{
    public bool IsShortCircuited { get; set; }
    public CommandResult? Result { get; set; }

    public static MiddlewareResult Continue() => new MiddlewareResult { IsShortCircuited = false };

    public static MiddlewareResult ShortCircuit(CommandResult result) =>
        new MiddlewareResult { IsShortCircuited = true, Result = result };
}
