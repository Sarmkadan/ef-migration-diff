#nullable enable

namespace EfMigrationDiff.CLI;

/// <summary>
/// Extension methods for CommandExecutor providing convenient utility operations.
/// </summary>
public static class CommandExecutorExtensions
{
    /// <summary>
    /// Checks if a command with the given name is registered.
    /// </summary>
    /// <param name="executor">The command executor instance.</param>
    /// <param name="commandName">Name of the command to check.</param>
    /// <returns>True if the command is registered; otherwise, false.</returns>
    public static bool IsCommandRegistered(this CommandExecutor executor, string commandName)
    {
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        if (string.IsNullOrWhiteSpace(commandName))
        {
            throw new ArgumentException("Command name cannot be null or whitespace.", nameof(commandName));
        }

        return executor.GetRegisteredCommandNames()
            .Contains(commandName.ToLowerInvariant());
    }

    /// <summary>
    /// Executes a command and returns the result. If the command fails, throws an exception.
    /// </summary>
    /// <param name="executor">The command executor instance.</param>
    /// <param name="commandName">Name of the command to execute.</param>
    /// <param name="args">Command arguments.</param>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <returns>The command result.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the command execution fails.</exception>
    public static async Task<CommandResult> ExecuteOrThrowAsync(
        this CommandExecutor executor,
        string commandName,
        string[] args,
        IServiceProvider serviceProvider)
    {
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        var result = await executor.ExecuteAsync(commandName, args, serviceProvider);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Command '{commandName}' failed with exit code {result.ExitCode}: {result.Message}");
        }

        return result;
    }

    /// <summary>
    /// Executes a command with the given arguments and returns the data payload.
    /// </summary>
    /// <typeparam name="T">Type of the data payload.</typeparam>
    /// <param name="executor">The command executor instance.</param>
    /// <param name="commandName">Name of the command to execute.</param>
    /// <param name="args">Command arguments.</param>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <returns>The data payload from the command result.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the command fails or data is not of type T.</exception>
    public static async Task<T> ExecuteAndGetDataAsync<T>(
        this CommandExecutor executor,
        string commandName,
        string[] args,
        IServiceProvider serviceProvider)
    {
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        var result = await executor.ExecuteAsync(commandName, args, serviceProvider);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Command '{commandName}' failed: {result.Message}");
        }

        if (result.Data is not T data)
        {
            throw new InvalidOperationException(
                $"Command '{commandName}' did not return data of type {typeof(T).Name}");
        }

        return data;
    }

    /// <summary>
    /// Executes a command and returns the result. If the command fails, returns the error result.
    /// </summary>
    /// <param name="executor">The command executor instance.</param>
    /// <param name="commandName">Name of the command to execute.</param>
    /// <param name="args">Command arguments.</param>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <returns>The command result, successful or failed.</returns>
    public static async Task<CommandResult> ExecuteWithFallbackAsync(
        this CommandExecutor executor,
        string commandName,
        string[] args,
        IServiceProvider serviceProvider)
    {
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        return await executor.ExecuteAsync(commandName, args, serviceProvider);
    }
}
