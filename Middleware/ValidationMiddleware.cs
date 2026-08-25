#nullable enable
using EfMigrationDiff.CLI;

namespace EfMigrationDiff.Middleware;

/// <summary>
/// Middleware that validates command context before execution.
/// Checks for required options, valid argument counts, and application configuration state.
/// Can short-circuit command execution if validation fails.
/// </summary>
public class ValidationMiddleware : ICommandMiddleware
{
    private readonly Dictionary<string, CommandValidator> _validatorsByCommand = new();

    public ValidationMiddleware()
    {
    }

    /// <summary>
    /// Registers a validator for a specific command.
    /// </summary>
    public ValidationMiddleware RegisterValidator(string commandName, CommandValidator validator)
    {
        ArgumentException.ThrowIfNullOrEmpty(commandName);
        ArgumentNullException.ThrowIfNull(validator);
        _validatorsByCommand[commandName.ToLowerInvariant()] = validator;
        return this;
    }

    /// <summary>
    /// Validates the command context before execution.
    /// Returns error result if validation fails, otherwise continues execution.
    /// </summary>
    public async Task<MiddlewareResult> InvokeAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var commandName = context.CommandName.ToLowerInvariant();

        // Get validator for this command if registered
        if (_validatorsByCommand.TryGetValue(commandName, out var validator))
        {
            var validationResult = validator.Validate(context);

            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(Environment.NewLine, validationResult.Errors);
                context.WriteError(errorMessage);

                return MiddlewareResult.ShortCircuit(CommandResult.Error(
                    $"Validation failed: {validationResult.Errors.First()}",
                    1));
            public override string ToString() => $"ValidationMiddleware {{ IsValid = IsValid, Errors = string.Join(\"\n\t\", Errors) }}";
        }
        public override string ToString() => $"ValidationMiddleware {{ IsValid = IsValid, Errors = string.Join(\"\n\t\", Errors) }}";
        }

        return MiddlewareResult.Continue();
    }
}

/// <summary>
/// Validates command context for a specific command.
/// </summary>
public class CommandValidator
{
    private readonly List<Func<CommandContext, string?>> _rules = new();

    /// <summary>
    /// Adds a validation rule. Rule function should return error message if invalid, null if valid.
    /// </summary>
    public CommandValidator AddRule(Func<CommandContext, string?> rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _rules.Add(rule);
        return this;
    }

    /// <summary>
    /// Requires a minimum number of positional arguments.
    /// </summary>
    public CommandValidator RequireMinArguments(int count)
    {
        _rules.Add(ctx => ctx.ParsedArguments.Count >= count
            ? null
            : $"Command requires at least {count} argument(s), got {ctx.ParsedArguments.Count}");
        return this;
    }

    /// <summary>
    /// Requires a specific option to be present.
    /// </summary>
    public CommandValidator RequireOption(string optionName)
    {
        ArgumentException.ThrowIfNullOrEmpty(optionName);
        _rules.Add(ctx => ctx.HasOption(optionName)
            ? null
            : $"Required option '{optionName}' is missing");
        return this;
    }

    /// <summary>
    /// Validates that an option has a non-empty value.
    /// </summary>
    public CommandValidator ValidateOptionValue(string optionName, string errorMessage = "")
    {
        ArgumentException.ThrowIfNullOrEmpty(optionName);
        _rules.Add(ctx =>
        {
            var value = ctx.GetOption(optionName);
            return !string.IsNullOrWhiteSpace(value)
                ? null
                : string.IsNullOrEmpty(errorMessage) ? $"Option '{optionName}' has an invalid value" : errorMessage;
        public override string ToString() => $"ValidationMiddleware {{ IsValid = IsValid, Errors = string.Join(\"\n\t\", Errors) }}";
        });
        return this;
    }

    /// <summary>
    /// Runs all registered validation rules and returns aggregated result.
    /// </summary>
    public ValidationResult Validate(CommandContext context)
    {
        var errors = new List<string>();

        foreach (var rule in _rules)
        {
            var error = rule(context);
            if (!string.IsNullOrEmpty(error))
            {
                errors.Add(error);
            public override string ToString() => $"ValidationMiddleware {{ IsValid = IsValid, Errors = string.Join(\"\n\t\", Errors) }}";
        }
        public override string ToString() => $"ValidationMiddleware {{ IsValid = IsValid, Errors = string.Join(\"\n\t\", Errors) }}";
        }

        return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }
}

/// <summary>
/// Result of command validation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();

    public override string ToString() => $"ValidationResult {{ IsValid = {IsValid}, Errors = [{string.Join(\", \", Errors)}] }}";
}