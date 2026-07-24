#nullable enable

using System.ComponentModel.DataAnnotations;
using System.IO;

namespace EfMigrationDiff.Configuration;

/// <summary>
/// Configuration options for the ef-migration-diff tool.
/// This class provides strongly-typed configuration for all application settings.
/// </summary>
public class EfMigrationDiffOptions
{
    /// <summary>
    /// Gets or sets the path to the repository.
    /// </summary>
    [Required(ErrorMessage = "RepositoryPath is required")]
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the migrations directory.
    /// </summary>
    [Required(ErrorMessage = "MigrationsPath is required")]
    public string MigrationsPath { get; set; } = "Migrations";

    /// <summary>
    /// Gets or sets the path to the output directory for reports.
    /// </summary>
    [Required(ErrorMessage = "OutputPath is required")]
    public string OutputPath { get; set; } = "./reports";

    /// <summary>
    /// Gets or sets the report format (text, json, html).
    /// </summary>
    [RegularExpression("^(text|json|html)$", ErrorMessage = "ReportFormat must be 'text', 'json', or 'html'")]
    public string ReportFormat { get; set; } = "text";

    /// <summary>
    /// Gets or sets a value indicating whether detailed logging is enabled.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of concurrent analysis operations.
    /// </summary>
    [Range(1, 16, ErrorMessage = "MaxConcurrentAnalysis must be between 1 and 16")]
    public int MaxConcurrentAnalysis { get; set; } = 4;

    /// <summary>
    /// Gets or sets a value indicating whether HTML reports should be generated.
    /// </summary>
    public bool GenerateHtmlReport { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether JSON reports should be generated.
    /// </summary>
    public bool GenerateJsonReport { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of DbContext names to analyze.
    /// </summary>
    public string[] DbContextNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the source branch name.
    /// </summary>
    [Required(ErrorMessage = "SourceBranch is required")]
    public string SourceBranch { get; set; } = "develop";

    /// <summary>
    /// Gets or sets the target branch name.
    /// </summary>
    [Required(ErrorMessage = "TargetBranch is required")]
    public string TargetBranch { get; set; } = "main";

    /// <summary>
    /// Gets or sets the schema diff options.
    /// </summary>
    public SchemaDiffOptions SchemaDiff { get; set; } = SchemaDiffOptions.Default;

    /// <summary>
    /// Gets or sets the list of migration name globs to ignore.
    /// Migrations matching any of these patterns will be excluded from diff calculations
    /// and reported as skipped.
    /// </summary>
    public string[] IgnoredMigrations { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <returns>A list of validation errors, or empty list if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(this);

        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            errors.AddRange(validationResults.Select(vr => vr.ErrorMessage ?? "Validation error"));
        }

        return errors;
    }

    /// <summary>
    /// Validates the configuration and throws if invalid.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when configuration is invalid.</exception>
    public void ValidateAndThrow()
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            throw new ValidationException(string.Join(Environment.NewLine, errors));
        }
    }

    /// <summary>
    /// Validates CLI-specific constraints that aren't covered by DataAnnotations.
    /// This ensures CLI argument parsing and direct configuration usage have consistent validation.
    /// </summary>
    /// <param name="reportFormat">The report format to validate against CLI constraints.</param>
    /// <param name="enableSummaryMode">Whether summary mode is enabled.</param>
    /// <param name="dotExportPath">The DOT export path if specified.</param>
    /// <param name="unknownOptions">List of unknown options to check for validation.</param>
    /// <param name="duplicateFlags">List of duplicate flags to check for validation.</param>
    /// <param name="positionalArgumentCount">Number of positional arguments provided.</param>
    /// <returns>A list of validation errors, or empty list if valid.</returns>
    public List<string> ValidateCliConstraints(
        string? reportFormat = null,
        bool? enableSummaryMode = null,
        string? dotExportPath = null,
        IReadOnlyCollection<string>? unknownOptions = null,
        IReadOnlyCollection<string>? duplicateFlags = null,
        int? positionalArgumentCount = null)
    {
        var errors = new List<string>();

        // Validate report format if provided
        if (reportFormat is not null)
        {
            if (string.IsNullOrWhiteSpace(reportFormat))
            {
                errors.Add("The --format option cannot be empty or whitespace.");
            }
            else if (reportFormat.Length > 100)
            {
                errors.Add($"The --format option value exceeds maximum length of 100 characters. Length: {reportFormat.Length} characters.");
            }
            else if (!reportFormat.Equals("text", StringComparison.OrdinalIgnoreCase) &&
                    !reportFormat.Equals("json", StringComparison.OrdinalIgnoreCase) &&
                    !reportFormat.Equals("html", StringComparison.OrdinalIgnoreCase) &&
                    !reportFormat.Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("ReportFormat must be 'text', 'json', 'html', or 'csv'.");
            }
        }

        // Validate DOT export path if provided
        if (dotExportPath is not null)
        {
            if (string.IsNullOrWhiteSpace(dotExportPath))
            {
                errors.Add("The --dot option cannot be empty or whitespace.");
            }
            else if (dotExportPath.Length > 32 * 1024) // 32KB maximum argument length
            {
                errors.Add($"The --dot option value exceeds maximum allowed length of {32 * 1024} characters. Length: {dotExportPath.Length} characters.");
            }
            else if (dotExportPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                errors.Add("The --dot option value contains invalid path characters.");
            }
            else if (dotExportPath.Contains("..") || dotExportPath.StartsWith("/") || dotExportPath.StartsWith("\\"))
            {
                errors.Add("The --dot option value contains directory traversal sequences or absolute paths. Use a relative path within the working directory.");
            }
        }

        // Validate mutually exclusive options: summary mode and dot export cannot be used together
        if (enableSummaryMode == true && dotExportPath is not null)
        {
            errors.Add("Options --summary and --dot are mutually exclusive. Choose one or the other.");
        }

        // Validate unknown options if provided
        if (unknownOptions is not null && unknownOptions.Count > 0)
        {
            foreach (var unknownOption in unknownOptions)
            {
                errors.Add($"Unknown option(s) specified: --{unknownOption}. Use --help for available options.");
                break; // Return first unknown option error for consistency
            }
        }

        // Validate duplicate flags if provided
        if (duplicateFlags is not null && duplicateFlags.Count > 0)
        {
            foreach (var duplicateFlag in duplicateFlags)
            {
                errors.Add($"Duplicate flag(s) specified: --{duplicateFlag}. Each flag can only be specified once.");
                break; // Return first duplicate flag error for consistency
            }
        }

        // Validate minimum positional arguments (commands typically need at least 2 positional arguments)
        if (positionalArgumentCount is not null && positionalArgumentCount < 2)
        {
            errors.Add("Missing required arguments. Expected at least 2 positional arguments (source migration/branch and target migration/branch).");
        }

        return errors;
    }

    /// <summary>
    /// Validates CLI-specific constraints and throws if invalid.
    /// </summary>
    /// <param name="reportFormat">The report format to validate against CLI constraints.</param>
    /// <param name="enableSummaryMode">Whether summary mode is enabled.</param>
    /// <param name="dotExportPath">The DOT export path if specified.</param>
    /// <exception cref="ValidationException">Thrown when CLI constraints are violated.</exception>
    public void ValidateCliConstraintsAndThrow(string? reportFormat = null, bool? enableSummaryMode = null, string? dotExportPath = null)
    {
        var errors = ValidateCliConstraints(reportFormat, enableSummaryMode, dotExportPath);
        if (errors.Count > 0)
        {
            throw new ValidationException(string.Join(Environment.NewLine, errors));
        }
    }

    /// <summary>
    /// Validates CLI-specific constraints and throws if invalid, including unknown options, duplicate flags, and positional arguments.
    /// </summary>
    /// <param name="reportFormat">The report format to validate against CLI constraints.</param>
    /// <param name="enableSummaryMode">Whether summary mode is enabled.</param>
    /// <param name="dotExportPath">The DOT export path if specified.</param>
    /// <param name="unknownOptions">List of unknown options to check for validation.</param>
    /// <param name="duplicateFlags">List of duplicate flags to check for validation.</param>
    /// <param name="positionalArgumentCount">Number of positional arguments provided.</param>
    /// <exception cref="ValidationException">Thrown when CLI constraints are violated.</exception>
    public void ValidateCliConstraintsAndThrow(
        string? reportFormat = null,
        bool? enableSummaryMode = null,
        string? dotExportPath = null,
        IReadOnlyCollection<string>? unknownOptions = null,
        IReadOnlyCollection<string>? duplicateFlags = null,
        int? positionalArgumentCount = null)
    {
        var errors = ValidateCliConstraints(reportFormat, enableSummaryMode, dotExportPath, unknownOptions, duplicateFlags, positionalArgumentCount);
        if (errors.Count > 0)
        {
            throw new ValidationException(string.Join(Environment.NewLine, errors));
        }
    }
}