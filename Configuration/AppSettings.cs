#nullable enable
using System.ComponentModel.DataAnnotations;

namespace EfMigrationDiff.Configuration;

/// <summary>
/// Application settings and configuration options.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets the path to the repository.
    /// </summary>
    [Required]
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the migrations directory.
    /// </summary>
    [Required]
    public string MigrationsPath { get; set; } = "Migrations";

    /// <summary>
    /// Gets or sets the path to the output directory for reports.
    /// </summary>
    [Required]
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
    public string[] DbContextNames { get; set; } = [];

    /// <summary>
    /// Gets or sets the source branch name.
    /// </summary>
    [Required]
    public string SourceBranch { get; set; } = "develop";

    /// <summary>
    /// Gets or sets the target branch name.
    /// </summary>
    [Required]
    public string TargetBranch { get; set; } = "main";

    /// <summary>
    /// Gets or sets the schema diff options.
    /// </summary>
    public SchemaDiffOptions SchemaDiff { get; set; } = SchemaDiffOptions.Default;

    /// <summary>
    /// Validates the settings configuration.
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

        if (!Directory.Exists(RepositoryPath))
            errors.Add($"RepositoryPath does not exist: {RepositoryPath}");

        if (MaxConcurrentAnalysis < 1)
            errors.Add("MaxConcurrentAnalysis must be at least 1");

        return errors;
    }

    /// <summary>
    /// Gets the full path to the migrations directory.
    /// </summary>
    /// <returns>The full path to the migrations directory.</returns>
    public string GetMigrationsDirectory()
    {
        return Path.Combine(RepositoryPath, MigrationsPath);
    }

    /// <summary>
    /// Gets the full path to the output directory.
    /// </summary>
    /// <returns>The full path to the output directory.</returns>
    public string GetOutputDirectory()
    {
        var path = Path.IsPathRooted(OutputPath) ? OutputPath : Path.Combine(RepositoryPath, OutputPath);
        return path;
    }

    /// <summary>
    /// Creates output directory if it doesn't exist.
    /// </summary>
    public void EnsureOutputDirectory()
    {
        var outputDir = GetOutputDirectory();
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
    }

    /// <summary>
    /// Gets a report filename based on the format.
    /// </summary>
    /// <param name="baseName">The base name for the report.</param>
    /// <returns>The generated report filename.</returns>
    public string GetReportFilename(string baseName = "migration-diff")
    {
        var extension = ReportFormat.ToLowerInvariant() switch
        {
            "json" => ".json",
            "html" => ".html",
            _ => ".txt"
        };

        return $"{baseName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}{extension}";
    }

    /// <summary>
    /// Gets the configured <see cref="SchemaDiffOptions"/>.
    /// </summary>
    public SchemaDiffOptions GetSchemaDiffOptions() => SchemaDiff ?? SchemaDiffOptions.Default;

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
    /// Gets the <see cref="EfMigrationDiffOptions"/> instance.
    /// </summary>
    public EfMigrationDiffOptions Value => ToEfMigrationDiffOptions();

    /// <summary>
    /// Converts to <see cref="EfMigrationDiffOptions"/>.
    /// </summary>
    /// <returns>The converted options.</returns>
    public EfMigrationDiffOptions ToEfMigrationDiffOptions()
    {
        return new EfMigrationDiffOptions
        {
            RepositoryPath = RepositoryPath,
            MigrationsPath = MigrationsPath,
            OutputPath = OutputPath,
            ReportFormat = ReportFormat,
            EnableDetailedLogging = EnableDetailedLogging,
            MaxConcurrentAnalysis = MaxConcurrentAnalysis,
            GenerateHtmlReport = GenerateHtmlReport,
            GenerateJsonReport = GenerateJsonReport,
            DbContextNames = DbContextNames,
            SourceBranch = SourceBranch,
            TargetBranch = TargetBranch,
            SchemaDiff = SchemaDiff
        };
    }

    /// <summary>
    /// Implicit conversion to SchemaDiffOptions for backward compatibility.
    /// </summary>
    public static implicit operator SchemaDiffOptions(AppSettings settings) =>
        settings?.SchemaDiff ?? SchemaDiffOptions.Default;
}
