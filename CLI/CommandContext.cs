#nullable enable
namespace EfMigrationDiff.CLI;

/// <summary>
/// Encapsulates the context of a CLI command execution, including parsed arguments and execution state.
/// Provides access to configuration, services, and execution metadata throughout the command lifecycle.
/// </summary>
public class CommandContext
{
    private readonly Dictionary<string, object> _metadata = new();

    public string CommandName { get; set; }
    public string[] RawArguments { get; set; }
    public Dictionary<string, string> ParsedOptions { get; set; }
    public List<string> ParsedArguments { get; set; }
    public IServiceProvider ServiceProvider { get; set; }
    public TextWriter Output { get; set; }
    public TextWriter ErrorOutput { get; set; }
    public CancellationToken CancellationToken { get; set; }

    public CommandContext(
        string commandName,
        string[] rawArguments,
        IServiceProvider serviceProvider,
        TextWriter? output = null,
        TextWriter? errorOutput = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(commandName);
        ArgumentNullException.ThrowIfNull(rawArguments);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        CommandName = commandName;
        RawArguments = rawArguments;
        ParsedOptions = new();
        ParsedArguments = new();
        ServiceProvider = serviceProvider;
        Output = output ?? Console.Out;
        ErrorOutput = errorOutput ?? Console.Error;
        CancellationToken = CancellationToken.None;
    }

    /// <summary>
    /// Stores metadata for the command execution. Allows commands and middleware to share state.
    /// </summary>
    public void SetMetadata(string key, object value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);
        _metadata[key] = value;
    }

    /// <summary>
    /// Retrieves metadata by key. Returns null if not found.
    /// </summary>
    public object? GetMetadata(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return _metadata.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Attempts to retrieve metadata with type safety. Returns false if key not found or type mismatch.
    /// </summary>
    public bool TryGetMetadata<T>(string key, out T? value) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        value = null;
        if (_metadata.TryGetValue(key, out var metadata) && metadata is T typedValue)
        {
            value = typedValue;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets an option value. Returns null if option not found.
    /// </summary>
    public string? GetOption(string optionName)
    {
        ArgumentException.ThrowIfNullOrEmpty(optionName);
        return ParsedOptions.TryGetValue(optionName, out var value) ? value : null;
    }

    /// <summary>
    /// Checks if a flag option was provided.
    /// </summary>
    public bool HasOption(string optionName)
    {
        ArgumentException.ThrowIfNullOrEmpty(optionName);
        return ParsedOptions.ContainsKey(optionName);
    }

    /// <summary>
    /// Gets an option or returns a default value.
    /// </summary>
    public string GetOptionOrDefault(string optionName, string defaultValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(optionName);
        ArgumentException.ThrowIfNullOrEmpty(defaultValue);
        return GetOption(optionName) ?? defaultValue;
    }

    /// <summary>
    /// Writes to the standard output with optional formatting.
    /// </summary>
    public void WriteOutput(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        Output.WriteLine(message);
    }

    /// <summary>
    /// Writes to the error output with optional formatting.
    /// </summary>
    public void WriteError(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        ErrorOutput.WriteLine($"ERROR: {message}");
    }

    /// <summary>
    /// Writes a colored message to output (requires console support).
    /// </summary>
    public void WriteColoredOutput(string message, ConsoleColor color)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Output.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    public override string ToString() => $"{nameof(CommandContext)} {{ CommandName = {CommandName}, RawArguments = {RawArguments}, ParsedOptions = {ParsedOptions}, ParsedArguments = {ParsedArguments}, ServiceProvider = {ServiceProvider}, Output = {Output} }}";
}
