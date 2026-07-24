#nullable enable
using EfMigrationDiff.Configuration;
using EfMigrationDiff.Repositories;
using EfMigrationDiff.Services;
using EfMigrationDiff.Exceptions;
using EfMigrationDiff.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace EfMigrationDiff.CLI.Commands;

/// <summary>
/// Implements the compare command to analyze migration differences between two branches.
/// Compares schema changes, detects conflicts, and generates reports in multiple formats.
/// </summary>
public class CompareCommand : ICommand
{
    /// <summary>
    /// Validates and resolves a file path to ensure it stays within the repository directory.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="repositoryPath">The repository root directory.</param>
    /// <returns>The resolved absolute path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when path or repositoryPath is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the path is invalid, contains directory traversal sequences,
    /// is absolute, or would write outside the repository directory.</exception>
    public static string ValidateAndResolvePath(string path, string repositoryPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(repositoryPath);

        // Check for null or whitespace after trimming
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("The export path cannot be null or whitespace.", nameof(path));
        }

        // Check for invalid path characters
        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            throw new ArgumentException(
                $"The export path '{path}' contains invalid path characters.",
                nameof(path));
        }

        // Get the full absolute path relative to the repository
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path, repositoryPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new ArgumentException(
                $"The export path '{path}' is invalid: {ex.Message}",
                nameof(path),
                ex);
        }

        // Normalize path separators for consistent comparison
        fullPath = fullPath.Replace('\\', Path.DirectorySeparatorChar);
        repositoryPath = repositoryPath.Replace('\\', Path.DirectorySeparatorChar);

        // Normalize both paths to end with directory separator for reliable comparison
        fullPath = Path.GetFullPath(fullPath);  // Normalize again to handle any remaining issues
        repositoryPath = Path.GetFullPath(repositoryPath);
        if (!repositoryPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            repositoryPath += Path.DirectorySeparatorChar;
        }

        // Check if the resolved path is absolute (shouldn't be after GetFullPath with base path)
        if (!Path.IsPathRooted(fullPath))
        {
            // This shouldn't happen with GetFullPath, but let's be defensive
            throw new ArgumentException(
                $"The export path '{path}' could not be resolved to an absolute path.");
        }

        // Ensure the resolved path stays within the repository directory
        // Use OrdinalIgnoreCase for cross-platform compatibility
        if (!fullPath.StartsWith(repositoryPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"The export path '{path}' would write outside the repository directory '{repositoryPath}'. " +
                "Use a relative path within the working directory.");
        }

        // Check if the path contains directory traversal sequences that could bypass our validation
        // This is a defense-in-depth check
        if (path.Contains("..") || path.Contains("~") || path.StartsWith("/") || path.StartsWith("\\"))
        {
            throw new ArgumentException(
                $"The export path '{path}' contains directory traversal sequences or absolute paths. " +
                "Use a relative path within the working directory.");
        }

        return fullPath;
    }

    public string GetDescription() => "Compare migrations between two branches and detect conflicts";

    /// <summary>
    /// Executes migration comparison between source and target branches.
    /// Validates arguments, initializes repositories, performs diff analysis, and generates reports.
    /// </summary>
    public async Task<CommandResult> ExecuteAsync(CommandContext context)
    {
        var appSettings = context.ServiceProvider.GetService<AppSettings>()
        ?? throw new InvalidOperationException("AppSettings not found in service provider");

        var diffService = context.ServiceProvider.GetService<MigrationDiffService>()
        ?? throw new InvalidOperationException("MigrationDiffService not found");

        var reportService = context.ServiceProvider.GetService<ReportGenerationService>()
        ?? throw new InvalidOperationException("ReportGenerationService not found");

        // Get source and target branches from arguments
        string sourceBranch = context.ParsedArguments.Count > 0
            ? context.ParsedArguments[0]
            : appSettings.SourceBranch;

        string targetBranch = context.ParsedArguments.Count > 1
            ? context.ParsedArguments[1]
            : appSettings.TargetBranch;

        // Check for optional format override
        string? format = context.GetOption("format");

        context.WriteColoredOutput($"Comparing {sourceBranch} → {targetBranch}", ConsoleColor.Cyan);

        appSettings.RepositoryPath = Environment.CurrentDirectory;

        // Initialize and validate repository
        var gitRepo = new GitRepository(appSettings.RepositoryPath);
        if (!gitRepo.Initialize())
        {
            return CommandResult.Error($"Failed to initialize git repository at {appSettings.RepositoryPath}");
        }

        var source = gitRepo.GetBranch(sourceBranch);
        var target = gitRepo.GetBranch(targetBranch);

        if (source is null)
            return CommandResult.Error($"Source branch not found: {sourceBranch}");

        if (target is null)
            return CommandResult.Error($"Target branch not found: {targetBranch}");

        // Perform comparison
        var diff = diffService.CompareBranches(source, target);

        // Optional DOT graph export
        string? dotExportPath = context.GetOption("dot");
        if (!string.IsNullOrEmpty(dotExportPath))
        {
            var graphService = context.ServiceProvider.GetService<MigrationDependencyGraphService>()
            ?? throw new InvalidOperationException("MigrationDependencyGraphService not found");

            // Validate and resolve the export path to prevent directory traversal attacks
            string resolvedPath;
            try
            {
                resolvedPath = ValidateAndResolvePath(dotExportPath, appSettings.RepositoryPath);
            }
            catch (ArgumentException ex)
            {
                context.WriteError(ex.Message);
                return CommandResult.Error(ex.Message, Constants.ExitCodes.Error);
            }

            // Ensure the output directory exists
            var outputDirectory = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Combine all migrations to build the graph
            var allMigrations = diff.OnlyInSource.Concat(diff.OnlyInTarget).Concat(diff.InBoth);
            var graph = graphService.Build(allMigrations);
            var dotContent = graphService.RenderDot(graph);
            File.WriteAllText(resolvedPath, dotContent);
            context.WriteColoredOutput($"✓ DOT graph exported to: {resolvedPath}", ConsoleColor.Green);
        }

        // Check for summary mode
        bool isSummary = context.GetOption("summary") is not null;

        // Display summary
        context.WriteColoredOutput($"\n✓ Comparison completed", ConsoleColor.Green);
        context.WriteOutput($" Conflicts detected: {diff.Conflicts.Count}");
        context.WriteOutput($" Schema changes: {diff.GetTotalSchemaChanges()}");
        context.WriteOutput($" Result: {diff.Result}");

        if (isSummary)
        {
            context.WriteOutput($" Summary: Added: {diff.OnlyInSource.Count}, Removed: {diff.OnlyInTarget.Count}, Conflicts: {diff.Conflicts.Count}");
        }
        else
        {
            // Generate report
            appSettings.EnsureOutputDirectory();

            if (!string.IsNullOrEmpty(format))
            {
                appSettings.ReportFormat = format;
            }

            var reportPath = Path.Combine(
                appSettings.GetOutputDirectory(),
                appSettings.GetReportFilename("migration-comparison"));

            var reportContent = appSettings.ReportFormat switch
            {
                "json" => reportService.GenerateJsonReport(diff),
                "html" => reportService.GenerateHtmlReport(diff),
                _ => reportService.GenerateTextReport(diff)
            };

            File.WriteAllText(reportPath, reportContent);

            // When JSON format is requested, emit the report to stdout so callers can pipe
            // or capture it directly without locating the output file. Status messages are
            // sent to stderr to keep stdout clean for machine consumption.
            if (appSettings.ReportFormat == "json")
            {
                context.ErrorOutput.WriteLine($"✓ Report saved to: {reportPath}");
                context.WriteOutput(reportContent);
            }
            else
            {
                context.WriteColoredOutput($"✓ Report saved to: {reportPath}", ConsoleColor.Green);
            }
        }

        gitRepo.Dispose();

        // Exit with error if blocking conflicts exist
        if (diff.HasBlockingConflicts())
        {
            context.WriteColoredOutput("⚠️ Blocking conflicts detected - deployment would be blocked", ConsoleColor.Yellow);
            return CommandResult.Error("Blocking conflicts detected", 1);
        }

        return CommandResult.Ok("Migration comparison completed successfully");
    }
}