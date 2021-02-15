// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Configuration;

/// <summary>
/// Application settings and configuration options.
/// </summary>
public class AppSettings
{
    public string RepositoryPath { get; set; } = string.Empty;
    public string MigrationsPath { get; set; } = "Migrations";
    public string OutputPath { get; set; } = "./reports";
    public string ReportFormat { get; set; } = "text";
    public bool EnableDetailedLogging { get; set; } = false;
    public int MaxConcurrentAnalysis { get; set; } = 4;
    public bool GenerateHtmlReport { get; set; } = true;
    public bool GenerateJsonReport { get; set; } = true;
    public string[] DbContextNames { get; set; } = [];
    public string SourceBranch { get; set; } = "develop";
    public string TargetBranch { get; set; } = "main";

    /// <summary>
    /// Validates the settings configuration.
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(RepositoryPath))
            errors.Add("RepositoryPath is required");

        if (!Directory.Exists(RepositoryPath))
            errors.Add($"RepositoryPath does not exist: {RepositoryPath}");

        if (string.IsNullOrWhiteSpace(MigrationsPath))
            errors.Add("MigrationsPath is required");

        if (MaxConcurrentAnalysis < 1)
            errors.Add("MaxConcurrentAnalysis must be at least 1");

        if (string.IsNullOrWhiteSpace(SourceBranch))
            errors.Add("SourceBranch is required");

        if (string.IsNullOrWhiteSpace(TargetBranch))
            errors.Add("TargetBranch is required");

        return errors;
    }

    /// <summary>
    /// Gets the full path to the migrations directory.
    /// </summary>
    public string GetMigrationsDirectory()
    {
        return Path.Combine(RepositoryPath, MigrationsPath);
    }

    /// <summary>
    /// Gets the full path to the output directory.
    /// </summary>
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
}
