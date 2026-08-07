#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EfMigrationDiff.Configuration;

namespace EfMigrationDiff.CLI;

/// <summary>
/// Parses raw command-line arguments into structured options and positional arguments.
/// Supports both short (-f) and long (--flag) format options, with value separation by '=' or space.
/// </summary>
public class CommandParser
{
    private const int MaxArgumentLength = 32 * 1024; // 32KB maximum argument length
    private readonly Dictionary<string, CommandOptionDefinition> _knownOptions = new();
    private readonly HashSet<string> _flagOptions = new();
    private readonly StringBuilder _usageBuilder = new();

    public CommandParser()
    {
        // Register the common "format" option used by ReportEngine.
        // Allows callers to specify: --format=json, -f csv, etc.
        RegisterOption("f", "format", "Specifies output format (json, csv, text, html, markdown)", isFlag: false);
        RegisterOption(string.Empty, "dot", "Exports migration dependency graph to a DOT file", isFlag: false);
        RegisterOption(string.Empty, "summary", "Display summary statistics instead of full report", isFlag: true);
    }

    /// <summary>
    /// Registers a known option with its definition. Allows validation and help generation.
    /// </summary>
    /// <param name="shortName">Short option name (e.g., "f" for -f)</param>
    /// <param name="longName">Long option name (e.g., "format" for --format)</param>
    /// <param name="description">Description for help text</param>
    /// <param name="isFlag">Whether this is a flag option that doesn't take a value</param>
    /// <returns>The parser instance for method chaining</returns>
    public CommandParser RegisterOption(string shortName, string longName, string description, bool isFlag = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(shortName);
        ArgumentException.ThrowIfNullOrEmpty(longName);
        ArgumentException.ThrowIfNullOrEmpty(description);

        var definition = new CommandOptionDefinition
        {
            ShortName = shortName,
            LongName = longName,
            Description = description,
            IsFlag = isFlag
        };

        if (!string.IsNullOrEmpty(shortName))
            _knownOptions[shortName] = definition;

        if (!string.IsNullOrEmpty(longName))
            _knownOptions[longName] = definition;

        if (isFlag)
            _flagOptions.Add(longName);

        return this;
    }

    /// <summary>
    /// Parses raw command-line arguments into a CommandContext with structured options and positional args.
    /// Handles various formats: --option=value, --option value, -o value, --flag
    /// </summary>
    /// <param name="commandName">Name of the command being executed. Cannot be null or whitespace.</param>
    /// <param name="args">Raw command line arguments. Cannot be null.</param>
    /// <param name="serviceProvider">Service provider for dependency injection. Cannot be null.</param>
    /// <param name="output">Output writer (defaults to Console.Out)</param>
    /// <param name="errorOutput">Error output writer (defaults to Console.Error)</param>
    /// <returns>Parsed command context</returns>
    /// <exception cref="ArgumentNullException">Thrown when commandName, args, or serviceProvider is null.</exception>
    /// <exception cref="ArgumentException">Thrown when commandName is empty or whitespace, or when any argument exceeds the maximum allowed length.</exception>
    public CommandContext Parse(
        string commandName,
        string[] args,
        IServiceProvider serviceProvider,
        TextWriter? output = null,
        TextWriter? errorOutput = null)
    {
        ArgumentNullException.ThrowIfNull(commandName);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName, nameof(commandName));
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        // Validate argument count
        if (args.Length == 0)
            return new CommandContext(commandName, args, serviceProvider, output, errorOutput);

        // Validate each argument for length and content
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Length > MaxArgumentLength)
            {
                throw new ArgumentException(
                    $"Argument at position {i} exceeds maximum allowed length of {MaxArgumentLength} characters. Length: {args[i].Length} characters.",
                    nameof(args));
            }

            if (string.IsNullOrWhiteSpace(args[i]))
            {
                throw new ArgumentException(
                    $"Argument at position {i} cannot be null, empty, or whitespace.",
                    nameof(args));
            }
        }

        var context = new CommandContext(commandName, args, serviceProvider, output, errorOutput);

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            // Handle long options (--option or --option=value)
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                i = ParseLongOption(arg, args, i, context);
            }
            // Handle short options (-o or -ovalue)
            else if (arg.StartsWith("-", StringComparison.Ordinal) && arg.Length > 1 && arg[1] != '-')
            {
                i = ParseShortOption(arg, args, i, context);
            }
            // Positional argument
            else
            {
                context.ParsedArguments.Add(arg);
            }
        }

        return context;
    }

    /// <summary>
    /// Parses a long-format option (--name or --name=value). Returns the updated argument index.
    /// </summary>
    /// <param name="arg">The raw argument string</param>
    /// <param name="args">All arguments for potential value extraction</param>
    /// <param name="currentIndex">Current position in argument array</param>
    /// <param name="context">Command context to populate</param>
    /// <returns>Updated argument index after parsing</returns>
    private int ParseLongOption(string arg, string[] args, int currentIndex, CommandContext context)
    {
        string optionName;
        string? optionValue = null;

        // Check for --option=value format
        if (arg.Contains("=", StringComparison.Ordinal))
        {
            var parts = arg.Substring(2).Split('=', 2);
            optionName = parts[0];
            optionValue = parts.Length > 1 ? parts[1] : null;
        }
        else
        {
            optionName = arg.Substring(2);
        }

        // Check if this is a flag option that doesn't take a value
        if (_flagOptions.Contains(optionName))
        {
            context.ParsedOptions[optionName] = "true";
        }
        else if (optionValue is not null)
        {
            context.ParsedOptions[optionName] = optionValue;
        }
        else if (currentIndex + 1 < args.Length && !args[currentIndex + 1].StartsWith("-", StringComparison.Ordinal))
        {
            // Next arg is the value for this option
            optionValue = args[currentIndex + 1];
            context.ParsedOptions[optionName] = optionValue;
            return currentIndex + 1;
        }
        else
        {
            context.ParsedOptions[optionName] = "true"; // Treat as flag if no value follows
        }

        return currentIndex;
    }

    /// <summary>
    /// Parses a short-format option (-f or -fvalue). Returns the updated argument index.
    /// </summary>
    /// <param name="arg">The raw argument string</param>
    /// <param name="args">All arguments for potential value extraction</param>
    /// <param name="currentIndex">Current position in argument array</param>
    /// <param name="context">Command context to populate</param>
    /// <returns>Updated argument index after parsing</returns>
    private int ParseShortOption(string arg, string[] args, int currentIndex, CommandContext context)
    {
        var optionChars = arg.Substring(1);

        // -o value format
        if (optionChars.Length == 1)
        {
            var optionName = optionChars;

            if (_flagOptions.Contains(optionName))
            {
                context.ParsedOptions[optionName] = "true";
            }
            else if (currentIndex + 1 < args.Length && !args[currentIndex + 1].StartsWith("-", StringComparison.Ordinal))
            {
                context.ParsedOptions[optionName] = args[currentIndex + 1];
                return currentIndex + 1;
            }
        }
        else
        {
            // -ovalue format
            context.ParsedOptions[optionChars[0].ToString()] = optionChars.Substring(1);
        }

        return currentIndex;
    }

    /// <summary>
    /// Validates parsed arguments and options, returning error messages if validation fails.
    /// This method delegates all validation to <see cref="EfMigrationDiffOptions.ValidateCliConstraints"/>
    /// to ensure consistent validation between CLI parsing and direct configuration usage.
    /// </summary>
    /// <param name="context">The parsed command context. Cannot be null.</param>
    /// <param name="commandName">The command name for usage generation. Cannot be null or empty.</param>
    /// <param name="args">The original arguments for error context. Cannot be null.</param>
    /// <returns>Error message if validation fails, null otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when context, commandName, or args is null.</exception>
    /// <exception cref="ArgumentException">Thrown when commandName is empty.</exception>
    public string? Validate(CommandContext context, string commandName, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(commandName);
        ArgumentNullException.ThrowIfNull(args);

        // Extract validation parameters from parsed context and arguments
        var formatOption = context.GetOption("format");
        var hasSummary = context.HasOption("summary");
        var hasDot = context.HasOption("dot");
        var dotPath = context.GetOption("dot");

        // Collect unknown options for validation
        var knownOptions = _knownOptions.Keys.ToHashSet(StringComparer.Ordinal);
        var unknownOptions = new List<string>();

        foreach (var arg in args)
        {
            if (arg.StartsWith("--", StringComparison.Ordinal) && arg.Length > 2)
            {
                var optionName = arg.Substring(2);
                // Handle --option=value format
                var equalsIndex = optionName.IndexOf('=');
                if (equalsIndex >= 0)
                {
                    optionName = optionName.Substring(0, equalsIndex);
                }

                if (!knownOptions.Contains(optionName) && !unknownOptions.Contains(optionName))
                {
                    unknownOptions.Add(optionName);
                }
            }
            else if (arg.StartsWith("-", StringComparison.Ordinal) && arg.Length > 1 && arg[1] != '-')
            {
                // Handle short options like -f or -ovalue
                var optionChar = arg[1].ToString();
                if (!knownOptions.Contains(optionChar) && !unknownOptions.Contains(optionChar))
                {
                    unknownOptions.Add(optionChar);
                }
            }
        }

        // Collect duplicate flags for validation
        var flagOptionNames = _flagOptions.ToHashSet();
        var seenFlags = new HashSet<string>();
        var duplicateFlags = new List<string>();

        foreach (var arg in args)
        {
            if (arg.StartsWith("--", StringComparison.Ordinal) && arg.Length > 2)
            {
                var optionName = arg.Substring(2);
                // Handle --option=value format
                var equalsIndex = optionName.IndexOf('=');
                if (equalsIndex >= 0)
                {
                    optionName = optionName.Substring(0, equalsIndex);
                }

                if (flagOptionNames.Contains(optionName))
                {
                    if (seenFlags.Contains(optionName))
                    {
                        if (!duplicateFlags.Contains(optionName))
                        {
                            duplicateFlags.Add(optionName);
                        }
                    }
                    seenFlags.Add(optionName);
                }
            }
        }

        // Delegate all validation to EfMigrationDiffOptions for consistency
        var options = new EfMigrationDiffOptions();
        var validationErrors = options.ValidateCliConstraints(
            reportFormat: formatOption,
            enableSummaryMode: hasSummary,
            dotExportPath: dotPath,
            unknownOptions: unknownOptions.Count > 0 ? unknownOptions : null,
            duplicateFlags: duplicateFlags.Count > 0 ? duplicateFlags : null,
            positionalArgumentCount: context.ParsedArguments.Count
        );

        // Return first error for CLI consistency, or null if validation passed
        return validationErrors.Count > 0 ? validationErrors[0] : null;
    }

    /// <summary>
    /// Gets all registered options for help text generation.
    /// </summary>
    /// <returns>Collection of registered option definitions</returns>
    public IEnumerable<CommandOptionDefinition> GetRegisteredOptions()
    {
        return _knownOptions.Values.Distinct(new OptionDefinitionComparer());
    }

    /// <summary>
    /// Generates usage information for the command.
    /// </summary>
    /// <param name="commandName">The command name to display in usage. Cannot be null or empty.</param>
    /// <returns>Formatted usage string.</returns>
    /// <exception cref="ArgumentException">Thrown when commandName is null or empty.</exception>
    public string GenerateUsage(string commandName)
    {
        ArgumentException.ThrowIfNullOrEmpty(commandName);

        var usage = new StringBuilder();

        usage.AppendLine($"\nUsage: {commandName} <command> [options]");
        usage.AppendLine("\nOptions:");

        foreach (var option in GetRegisteredOptions())
        {
            var shortPart = !string.IsNullOrEmpty(option.ShortName) ? $"-{option.ShortName}, " : " ";
            var longPart = !string.IsNullOrEmpty(option.LongName) ? $"--{option.LongName}" : "";
            var separator = option.IsFlag ? "" : " <value>";

            usage.AppendLine($" {shortPart}{longPart}{separator} - {option.Description}");
        }

        usage.AppendLine("\nExamples:");
        usage.AppendLine($" {commandName} compare develop main");
        usage.AppendLine($" {commandName} compare develop main --format json");
        usage.AppendLine($" {commandName} compare develop main --summary");
        usage.AppendLine($" {commandName} compare develop main --dot output.dot");

        return usage.ToString();
    }

    /// <summary>
    /// Comparer to prevent duplicate option definitions when iterating registered options.
    /// </summary>
    private class OptionDefinitionComparer : IEqualityComparer<CommandOptionDefinition>
    {
        public bool Equals(CommandOptionDefinition? x, CommandOptionDefinition? y)
        {
            return x?.LongName == y?.LongName;
        }

        public int GetHashCode(CommandOptionDefinition obj)
        {
            return obj.LongName.GetHashCode();
        }
    }
}

/// <summary>
/// Defines a command-line option with metadata for parsing and help generation.
/// </summary>
public class CommandOptionDefinition : IEquatable<CommandOptionDefinition>
{
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsFlag { get; set; }

    public bool Equals(CommandOptionDefinition? other)
    {
        if (other is null) return false;
        return ShortName == other.ShortName &&
               LongName == other.LongName &&
               Description == other.Description &&
               IsFlag == other.IsFlag;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CommandOptionDefinition);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ShortName, LongName, Description, IsFlag);
    }

    public static bool operator ==(CommandOptionDefinition? left, CommandOptionDefinition? right)
    {
        return EqualityComparer<CommandOptionDefinition>.Default.Equals(left, right);
    }

    public static bool operator !=(CommandOptionDefinition? left, CommandOptionDefinition? right)
    {
        return !(left == right);
    }
}