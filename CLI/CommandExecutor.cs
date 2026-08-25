#nullable enable
using EfMigrationDiff.Utilities;
using System.Text.Json;

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
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(command);
        _commands[name.ToLowerInvariant()] = command;
        return this;
    }

    /// <summary>
    /// Registers middleware to be executed before command execution.
    /// Middleware runs in registration order and can modify context or short-circuit execution.
    /// </summary>
    public CommandExecutor RegisterMiddleware(ICommandMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Executes a command by name with the given arguments.
    /// Returns a CommandResult indicating success/failure and any output.
    /// </summary>
    /// <param name="commandName">Name of the command to execute. Cannot be null or whitespace.</param>
    /// <param name="args">Command arguments. Can be empty but not null.</param>
    /// <param name="serviceProvider">Service provider for dependency injection. Cannot be null.</param>
    /// <param name="output">Standard output writer. If null, uses Console.Out.</param>
    /// <param name="errorOutput">Error output writer. If null, uses Console.Error.</param>
    /// <param name="verbose">If true, includes detailed stack traces in error output.</param>
    /// <returns>A CommandResult indicating success/failure and any output.</returns>
    /// <exception cref="ArgumentNullException">Thrown if commandName or serviceProvider is null.</exception>
    /// <exception cref="ArgumentException">Thrown if commandName is empty or whitespace.</exception>
    public async Task<CommandResult> ExecuteAsync(string commandName, string[] args, IServiceProvider serviceProvider, TextWriter? output = null, TextWriter? errorOutput = null, bool verbose = false)
    {
        ArgumentNullException.ThrowIfNull(commandName);
        ArgumentException.ThrowIfNullOrEmpty(commandName);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var context = _parser.Parse(commandName, args, serviceProvider, output, errorOutput);

        try
        {
            // Validate parsed arguments before execution
            var validationError = _parser.Validate(context, commandName, args);
            if (validationError is not null)
            {
                context.WriteError(validationError);
                context.WriteOutput(_parser.GenerateUsage(commandName));
                return CommandResult.Error(validationError, Constants.ExitCodes.Error);
            }

            // Execute middleware pipeline
            foreach (var middleware in _middlewares)
            {
                var middlewareResult = await middleware.InvokeAsync(context);
                if (middlewareResult.IsShortCircuited)
                {
                    return middlewareResult.Result ?? new CommandResult
                    {
                        Success = false,
                        Message = "Command execution short-circuited by middleware",
                        ExitCode = Constants.ExitCodes.Error
                    };
                }
            }

            // Execute the actual command
            if (!_commands.TryGetValue(commandName.ToLowerInvariant(), out var command))
            {
                var errorMsg = $"Unknown command: {commandName}. Use --help for available commands.";
                context.WriteError(errorMsg);
                context.WriteOutput(_parser.GenerateUsage(commandName));
                return CommandResult.Error(errorMsg, Constants.ExitCodes.Error);
            }

            var result = await command.ExecuteAsync(context);
            return result;
        }
        catch (ArgumentNullException ex)
        {
            var errorMsg = $"Invalid argument: {ex.ParamName}";
            context.WriteError(errorMsg);
            if (verbose && ex.StackTrace is not null)
            {
                context.WriteError($"Stack trace:\n{ex.StackTrace}");
            }
            return new CommandResult
            {
                Success = false,
                Message = errorMsg,
                ExitCode = Constants.ExitCodes.Error
            };
        }
        catch (ArgumentException ex)
        {
            var errorMsg = $"Invalid argument: {ex.Message}";
            context.WriteError(errorMsg);
            if (verbose && ex.StackTrace is not null)
            {
                context.WriteError($"Stack trace:\n{ex.StackTrace}");
            }
            return new CommandResult
            {
                Success = false,
                Message = errorMsg,
                ExitCode = Constants.ExitCodes.Error
            };
        }
        catch (JsonException ex)
        {
            var errorMsg = $"JSON processing error: {ex.Message}";
            context.WriteError(errorMsg);
            if (verbose && ex.StackTrace is not null)
            {
                context.WriteError($"Stack trace:\n{ex.StackTrace}");
            }
            return new CommandResult
            {
                Success = false,
                Message = errorMsg,
                ExitCode = Constants.ExitCodes.Error
            };
        }
        catch (IOException ex)
        {
            var errorMsg = $"File I/O error: {ex.Message}";
            context.WriteError(errorMsg);
            if (verbose && ex.StackTrace is not null)
            {
                context.WriteError($"Stack trace:\n{ex.StackTrace}");
            }
            return new CommandResult
            {
                Success = false,
                Message = errorMsg,
                ExitCode = Constants.ExitCodes.Error
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            var errorMsg = $"Access denied: {ex.Message}";
            context.WriteError(errorMsg);
            if (verbose && ex.StackTrace is not null)
            {
                context.WriteError($"Stack trace:\n{ex.StackTrace}");
            }
            return new CommandResult
            {
                Success = false,
                Message = errorMsg,
                ExitCode = Constants.ExitCodes.Error
            };
        }
        catch (OperationCanceledException ex)
        {
            var errorMsg = $"Operation cancelled: {ex.Message}";
            context.WriteError(errorMsg);
            if (verbose && ex.StackTrace is not null)
            {
                context.WriteError($"Stack trace:\n{ex.StackTrace}");
            }
            return new CommandResult
            {
                Success = false,
                Message = errorMsg,
                ExitCode = Constants.ExitCodes.Error
            };
        }
        catch (InvalidOperationException ex)
        {
            var errorMsg = $"Invalid operation: {ex.Message}";
            context.WriteError(errorMsg);
            if (verbose && ex.StackTrace is not null)
            {
                context.WriteError($"Stack trace:\n{ex.StackTrace}");
            }
            return new CommandResult
            {
                Success = false,
                Message = errorMsg,
                ExitCode = Constants.ExitCodes.Error
            };
        }
        catch (Exception ex)
        {
            var errorMsg = $"Unexpected error: {ex.Message}";
            context.WriteError(errorMsg);
            if (verbose && ex.StackTrace is not null)
            {
                context.WriteError($"Stack trace:\n{ex.StackTrace}");
            }
            return new CommandResult
            {
                Success = false,
                Message = errorMsg,
                ExitCode = Constants.ExitCodes.Error
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
            ExitCode = Constants.ExitCodes.NoDiff
        };
    }

    /// <summary>
    /// Creates a failure result with exit code.
    /// </summary>
    public static CommandResult Error(string message, int exitCode = Constants.ExitCodes.Error)
    {
        return new CommandResult
        {
            Success = false,
            Message = message,
            ExitCode = exitCode
        };
    }

    public override string ToString()
    {
        return $"CommandResult {{ Success = {Success}, Message = {Message}, ExitCode = {ExitCode}, Data = {Data} }}";
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