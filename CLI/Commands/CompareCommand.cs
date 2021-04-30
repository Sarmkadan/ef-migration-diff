#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Configuration;
using EfMigrationDiff.Repositories;
using EfMigrationDiff.Services;
using EfMigrationDiff.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EfMigrationDiff.CLI.Commands;

/// <summary>
/// Implements the compare command to analyze migration differences between two branches.
/// Compares schema changes, detects conflicts, and generates reports in multiple formats.
/// </summary>
public class CompareCommand : ICommand
{
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

        // Display summary
        context.WriteColoredOutput($"\n✓ Comparison completed", ConsoleColor.Green);
        context.WriteOutput($"  Conflicts detected: {diff.Conflicts.Count}");
        context.WriteOutput($"  Schema changes: {diff.GetTotalSchemaChanges()}");
        context.WriteOutput($"  Result: {diff.Result}");

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

        gitRepo.Dispose();

        // Exit with error if blocking conflicts exist
        if (diff.HasBlockingConflicts())
        {
            context.WriteColoredOutput("⚠️  Blocking conflicts detected - deployment would be blocked", ConsoleColor.Yellow);
            return CommandResult.Error("Blocking conflicts detected", 1);
        }

        return CommandResult.Ok("Migration comparison completed successfully");
    }
}
