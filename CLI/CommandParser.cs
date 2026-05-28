#nullable enable
namespace EfMigrationDiff.CLI;

/// <summary>
/// Parses raw command-line arguments into structured options and positional arguments.
/// Supports both short (-f) and long (--flag) format options, with value separation by '=' or space.
/// </summary>
public class CommandParser
{
    private readonly Dictionary<string, CommandOptionDefinition> _knownOptions = new();
    private readonly HashSet<string> _flagOptions = new();

    public CommandParser()
    {
    }

    /// <summary>
    /// Registers a known option with its definition. Allows validation and help generation.
    /// </summary>
    public CommandParser RegisterOption(string shortName, string longName, string description, bool isFlag = false)
    {
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
    public CommandContext Parse(
        string commandName,
        string[] args,
        IServiceProvider serviceProvider,
        TextWriter? output = null,
        TextWriter? errorOutput = null)
    {
        var context = new CommandContext(commandName, args, serviceProvider, output, errorOutput);

        if (args.Length == 0)
            return context;

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
    /// Gets all registered options for help text generation.
    /// </summary>
    public IEnumerable<CommandOptionDefinition> GetRegisteredOptions()
    {
        return _knownOptions.Values.Distinct(new OptionDefinitionComparer());
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
public class CommandOptionDefinition
{
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsFlag { get; set; }
}
