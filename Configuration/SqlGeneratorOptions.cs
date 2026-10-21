#nullable enable

using System.Text.RegularExpressions;
using EfMigrationDiff.Exceptions;

namespace EfMigrationDiff.Configuration;

/// <summary>
/// Options that control SQL script generation in <c>SqlGenerator</c>.
/// Validated when the generator is constructed and at CLI startup, so misconfiguration fails before any work begins.
/// </summary>
public sealed record SqlGeneratorOptions
{
    // Identifier: letter or underscore first, then letters/digits/underscores, at most 128 characters in total.
    private static readonly Regex IdentifierPattern =
        new(@"\A[A-Za-z_][A-Za-z0-9_]{0,127}\z", RegexOptions.Compiled);

    /// <summary>
    /// SQL dialects the generator can emit. Matching is case-insensitive.
    /// </summary>
    public static IReadOnlyList<string> SupportedDialects { get; } = ["SqlServer"];

    /// <summary>
    /// Gets the default options.
    /// </summary>
    public static SqlGeneratorOptions Default => new();

    /// <summary>
    /// Gets or sets the target SQL dialect. Must be one of <see cref="SupportedDialects"/>.
    /// </summary>
    public string Dialect { get; init; } = "SqlServer";

    /// <summary>
    /// Gets or sets the schema name used to qualify generated objects. <see langword="null"/> means unqualified.
    /// </summary>
    public string? SchemaName { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the script is split into batches using <see cref="BatchSeparator"/>.
    /// </summary>
    public bool EnableBatching { get; init; } = true;

    /// <summary>
    /// Gets or sets the statement that separates batches when <see cref="EnableBatching"/> is <see langword="true"/>.
    /// </summary>
    public string BatchSeparator { get; init; } = "GO";

    /// <summary>
    /// Gets or sets the file the script is written to. <see langword="null"/> means no file output.
    /// The directory must already exist; relative paths resolve against the current directory.
    /// </summary>
    public string? OutputFilePath { get; init; }

    /// <summary>
    /// Validates the options and collects every violation.
    /// </summary>
    /// <returns>A list of violation messages; empty if the options are valid.</returns>
    public List<string> Validate()
    {
        var violations = new List<string>();

        if (string.IsNullOrWhiteSpace(Dialect))
        {
            violations.Add("Dialect is required.");
        }
        else if (!SupportedDialects.Contains(Dialect, StringComparer.OrdinalIgnoreCase))
        {
            violations.Add($"Dialect '{Dialect}' is not supported. Supported dialects: {string.Join(", ", SupportedDialects)}.");
        }

        if (SchemaName is not null && !IdentifierPattern.IsMatch(SchemaName))
        {
            violations.Add(
                $"SchemaName '{SchemaName}' is not a valid identifier. Use letters, digits and underscores, " +
                $"starting with a letter or underscore (max 128 characters).");
        }

        if (EnableBatching && string.IsNullOrWhiteSpace(BatchSeparator))
        {
            violations.Add("BatchSeparator must not be empty when EnableBatching is true.");
        }

        if (OutputFilePath is not null)
        {
            ValidateOutputFilePath(OutputFilePath, violations);
        }

        return violations;
    }

    /// <summary>
    /// Validates the options and throws if any rule is violated.
    /// </summary>
    /// <exception cref="ConfigurationValidationException">Thrown with all violations when the options are invalid.</exception>
    public void ValidateAndThrow()
    {
        var violations = Validate();
        if (violations.Count > 0)
        {
            throw new ConfigurationValidationException(violations);
        }
    }

    private static void ValidateOutputFilePath(string outputFilePath, List<string> violations)
    {
        if (string.IsNullOrWhiteSpace(outputFilePath))
        {
            violations.Add("OutputFilePath must not be blank when set.");
            return;
        }

        string? directory;
        try
        {
            directory = Path.GetDirectoryName(Path.GetFullPath(outputFilePath));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            violations.Add($"OutputFilePath '{outputFilePath}' is not a valid path.");
            return;
        }

        if (directory is null)
        {
            violations.Add($"OutputFilePath '{outputFilePath}' is not a valid path.");
        }
        else if (!Directory.Exists(directory))
        {
            violations.Add($"OutputFilePath '{outputFilePath}': directory '{directory}' does not exist.");
        }
    }
}
