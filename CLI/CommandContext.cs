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
        _metadata[key] = value;
    }

    /// <summary>
    /// Retrieves metadata by key. Returns null if not found.
    /// </summary>
    public object? GetMetadata(string key)
    {
        return _metadata.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Attempts to retrieve metadata with type safety. Returns false if key not found or type mismatch.
    /// </summary>
    public bool TryGetMetadata<T>(string key, out T? value) where T : class
    {
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
        return ParsedOptions.TryGetValue(optionName, out var value) ? value : null;
    }

    /// <summary>
    /// Checks if a flag option was provided.
    /// </summary>
    public bool HasOption(string optionName)
    {
        return ParsedOptions.ContainsKey(optionName);
    }

    /// <summary>
    /// Gets an option or returns a default value.
    /// </summary>
    public string GetOptionOrDefault(string optionName, string defaultValue)
    {
        return GetOption(optionName) ?? defaultValue;
    }

    /// <summary>
    /// Writes to the standard output with optional formatting.
    /// </summary>
    public void WriteOutput(string message)
    {
        Output.WriteLine(message);
    }

    /// <summary>
    /// Writes to the error output with optional formatting.
    /// </summary>
    public void WriteError(string message)
    {
        ErrorOutput.WriteLine($"ERROR: {message}");
    }

    /// <summary>
    /// Writes a colored message to output (requires console support).
    /// </summary>
    public void WriteColoredOutput(string message, ConsoleColor color)
    {
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
}
