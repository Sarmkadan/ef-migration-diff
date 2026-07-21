#nullable enable

using System.ComponentModel.DataAnnotations;

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
}
