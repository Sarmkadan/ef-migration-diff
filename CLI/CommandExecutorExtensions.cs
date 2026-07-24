#nullable enable
namespace EfMigrationDiff.CLI;

/// <summary>
/// Extension methods for <see cref="CommandExecutor"/> providing convenient utility operations
/// for command execution, validation, and result handling.
/// </summary>
public static class CommandExecutorExtensions
{
    /// <summary>
    /// Checks if a command with the given name is registered.
    /// </summary>
    /// <param name="executor">The command executor instance. Cannot be <see langword="null"/>.</param>
    /// <param name="commandName">Name of the command to check. Cannot be <see langword="null"/> or whitespace.</param>
    /// <returns><see langword="true"/> if the command is registered; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="executor"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="commandName"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
    public static bool IsCommandRegistered(this CommandExecutor executor, string commandName)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);

        return executor.GetRegisteredCommandNames()
            .Contains(commandName.ToLowerInvariant());
    }

    /// <summary>
    /// Executes a command and returns the result. If the command fails, throws an exception.
    /// </summary>
    /// <param name="executor">The command executor instance. Cannot be <see langword="null"/>.</param>
    /// <param name="commandName">Name of the command to execute. Cannot be <see langword="null"/> or empty.</param>
    /// <param name="args">Command arguments. Can be empty but not <see langword="null"/>.</param>
    /// <param name="serviceProvider">Service provider for dependency injection. Cannot be <see langword="null"/>.</param>
    /// <param name="verbose">If true, includes detailed stack traces in error output.</param>
    /// <returns>The command result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the command execution fails.</exception>
    public static async Task<CommandResult> ExecuteOrThrowAsync(
        this CommandExecutor executor,
        string commandName,
        string[] args,
        IServiceProvider serviceProvider,
        bool verbose = false)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(commandName);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var result = await executor.ExecuteAsync(commandName, args, serviceProvider, verbose: verbose);

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
    /// <param name="executor">The command executor instance. Cannot be <see langword="null"/>.</param>
    /// <param name="commandName">Name of the command to execute. Cannot be <see langword="null"/> or empty.</param>
    /// <param name="args">Command arguments. Can be empty but not <see langword="null"/>.</param>
    /// <param name="serviceProvider">Service provider for dependency injection. Cannot be <see langword="null"/>.</param>
    /// <param name="verbose">If true, includes detailed stack traces in error output.</param>
    /// <returns>The data payload from the command result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the command fails or data is not of type <typeparamref name="T"/>.</exception>
    public static async Task<T> ExecuteAndGetDataAsync<T>(
        this CommandExecutor executor,
        string commandName,
        string[] args,
        IServiceProvider serviceProvider,
        bool verbose = false)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(commandName);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var result = await executor.ExecuteAsync(commandName, args, serviceProvider, verbose: verbose);

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
    /// This method provides a safe execution path that never throws exceptions.
    /// </summary>
    /// <param name="executor">The command executor instance. Cannot be <see langword="null"/>.</param>
    /// <param name="commandName">Name of the command to execute. Cannot be <see langword="null"/> or empty.</param>
    /// <param name="args">Command arguments. Can be empty but not <see langword="null"/>.</param>
    /// <param name="serviceProvider">Service provider for dependency injection. Cannot be <see langword="null"/>.</param>
    /// <param name="verbose">If true, includes detailed stack traces in error output.</param>
    /// <returns>The command result, successful or failed.</returns>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is <see langword="null"/>.</exception>
    public static async Task<CommandResult> ExecuteWithFallbackAsync(
        this CommandExecutor executor,
        string commandName,
        string[] args,
        IServiceProvider serviceProvider,
        bool verbose = false)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(commandName);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return await executor.ExecuteAsync(commandName, args, serviceProvider, verbose: verbose);
    }
}