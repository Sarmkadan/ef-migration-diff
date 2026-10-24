#nullable enable
using EfMigrationDiff.Configuration;
using EfMigrationDiff.Models;
using EfMigrationDiff.Repositories;
using EfMigrationDiff.Services;
using EfMigrationDiff.Exceptions;
using EfMigrationDiff.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;

namespace EfMigrationDiff.CLI.Commands;

/// <summary>
/// Implements the compare command to analyze migration differences between two branches.
/// Compares schema changes, detects conflicts, and generates reports in multiple formats.
/// </summary>
public class CompareCommand : ICommand
{
    private static class OptionNames
    {
        public const string Format = "format";
        public const string DotExport = "dot";
        public const string Summary = "summary";
    }

    private static class ReportNames
    {
        public const string ComparisonBase = "migration-comparison";
    }

    private static class PathRules
    {
        public const string ParentDirectorySequence = "..";
        public const string HomeDirectoryPrefix = "~";
        public const string ForwardSlashPrefix = "/";
        public const string BackslashPrefix = "\\";
    }

    private const string DefaultDbContextName = "DefaultContext";
    private const string MigrationFileExtension = ".cs";
    private const string DesignerFileSuffix = ".Designer.cs";

    private static class Messages
    {
        public const string ExportPathEmpty = "The export path cannot be null or whitespace.";
        public const string ExportPathInvalidChars = "The export path '{0}' contains invalid path characters.";
        public const string ExportPathInvalid = "The export path '{0}' is invalid: {1}";
        public const string ExportPathNotResolved = "The export path '{0}' could not be resolved to an absolute path.";
        public const string ExportPathOutsideRepository =
            "The export path '{0}' would write outside the repository directory '{1}'. " +
            "Use a relative path within the working directory.";
        public const string ExportPathTraversal =
            "The export path '{0}' contains directory traversal sequences or absolute paths. " +
            "Use a relative path within the working directory.";

        public const string AppSettingsNotFound = "AppSettings not found in service provider";
        public const string DiffServiceNotFound = "MigrationDiffService not found";
        public const string ReportServiceNotFound = "ReportGenerationService not found";
        public const string GraphServiceNotFound = "MigrationDependencyGraphService not found";
        public const string MigrationRepositoryNotFound = "MigrationRepository not found";
        public const string ParserServiceNotFound = "MigrationParserService not found";

        public const string Comparing = "Comparing {0} → {1}";
        public const string RepositoryInitFailed = "Failed to initialize git repository at {0}";
        public const string SourceBranchNotFound = "Source branch not found: {0}";
        public const string TargetBranchNotFound = "Target branch not found: {0}";
        public const string DotExported = "✓ DOT graph exported to: {0}";
        public const string ComparisonCompleted = "\n✓ Comparison completed";
        public const string ConflictsDetected = " Conflicts detected: {0}";
        public const string SchemaChanges = " Schema changes: {0}";
        public const string Result = " Result: {0}";
        public const string TablesStatistics = " Tables: added {0}, removed {1}, modified {2}";
        public const string ColumnsStatistics = " Columns: added {0}, removed {1}, modified {2}";
        public const string IndexesStatistics = " Indexes: changed {0}";
        public const string SummaryLine = " Summary: Added: {0}, Removed: {1}, Conflicts: {2}";
        public const string ReportSaved = "✓ Report saved to: {0}";
        public const string BlockingConflictsWarning = "⚠️ Blocking conflicts detected - deployment would be blocked";
        public const string BlockingConflictsError = "Blocking conflicts detected";
        public const string BreakingChangesWarning = "⚠️ Breaking schema changes detected - review before merging";
        public const string BreakingChangesError = "Breaking schema changes detected";
        public const string NonBreakingChanges = "Non-breaking schema changes detected";
        public const string NoChanges = "No schema changes between the branches";
    }

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
            throw new ArgumentException(Messages.ExportPathEmpty, nameof(path));
        }

        // Check for invalid path characters
        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            throw new ArgumentException(
                string.Format(Messages.ExportPathInvalidChars, path),
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
                string.Format(Messages.ExportPathInvalid, path, ex.Message),
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
                string.Format(Messages.ExportPathNotResolved, path));
        }

        // Ensure the resolved path stays within the repository directory
        // Use OrdinalIgnoreCase for cross-platform compatibility
        if (!fullPath.StartsWith(repositoryPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                string.Format(Messages.ExportPathOutsideRepository, path, repositoryPath));
        }

        // Check if the path contains directory traversal sequences that could bypass our validation
        // This is a defense-in-depth check
        if (path.Contains(PathRules.ParentDirectorySequence)
            || path.Contains(PathRules.HomeDirectoryPrefix)
            || path.StartsWith(PathRules.ForwardSlashPrefix)
            || path.StartsWith(PathRules.BackslashPrefix))
        {
            throw new ArgumentException(
                string.Format(Messages.ExportPathTraversal, path));
        }

        return fullPath;
    }

    public string GetDescription() => "Compare migrations between two branches and detect conflicts";

    /// <summary>
    /// Maps a branch comparison onto the compare exit code contract. Checks run from most to least severe:
    /// blocking conflicts, breaking schema changes, non-breaking changes, then no changes.
    /// Only the schema changes introduced by source-only and target-only migrations are considered.
    /// </summary>
    /// <param name="diff">The comparison result. Cannot be null.</param>
    /// <returns>
    /// <see cref="Constants.ExitCodes.ConflictsFound"/>, <see cref="Constants.ExitCodes.BreakingChangesFound"/>,
    /// <see cref="Constants.ExitCodes.DiffFound"/> or <see cref="Constants.ExitCodes.NoDiff"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="diff"/> is null.</exception>
    public static int DetermineExitCode(MigrationDiff diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        if (diff.HasBlockingConflicts())
            return Constants.ExitCodes.ConflictsFound;

        var changes = GetDiffScopedSchemaChanges(diff);
        var classifications = new BreakingChangeDetector(NullLogger<BreakingChangeDetector>.Instance)
            .ClassifyChanges(changes);

        if (classifications.Any(classification => classification.Severity == BreakingChangeSeverity.Breaking))
            return Constants.ExitCodes.BreakingChangesFound;

        var hasChanges = changes.Count > 0 || diff.GetMigrationsNeedingAttention() > 0 || diff.HasConflicts();
        return hasChanges ? Constants.ExitCodes.DiffFound : Constants.ExitCodes.NoDiff;
    }

    /// <summary>
    /// Collects the schema changes that belong to migrations present on only one of the two branches.
    /// </summary>
    /// <param name="diff">The comparison result. Cannot be null.</param>
    /// <returns>The source-only and target-only schema changes.</returns>
    private static List<SchemaChange> GetDiffScopedSchemaChanges(MigrationDiff diff)
    {
        var sourceOnlyIds = diff.OnlyInSource.Select(migration => migration.Id).ToHashSet();
        var targetOnlyIds = diff.OnlyInTarget.Select(migration => migration.Id).ToHashSet();

        return diff.SourceSchemaChanges.Where(change => sourceOnlyIds.Contains(change.MigrationId))
            .Concat(diff.TargetSchemaChanges.Where(change => targetOnlyIds.Contains(change.MigrationId)))
            .ToList();
    }

    /// <summary>
    /// Reads the migration files committed on a branch's tip, parses them and registers them so the
    /// diff service can see them. Migrations already registered under the same id are reused.
    /// </summary>
    /// <param name="gitRepo">An initialized git repository. Cannot be null.</param>
    /// <param name="branch">The branch whose migrations are loaded. Cannot be null.</param>
    /// <param name="migrationsPath">Repository-relative migrations directory. Cannot be null or empty.</param>
    /// <param name="parser">Parser used for migration source text. Cannot be null.</param>
    /// <param name="repository">Store the parsed migrations are registered in. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when any reference argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="migrationsPath"/> is null or empty.</exception>
    private static void LoadBranchMigrations(
        GitRepository gitRepo,
        BranchInfo branch,
        string migrationsPath,
        MigrationParserService parser,
        MigrationRepository repository)
    {
        ArgumentNullException.ThrowIfNull(gitRepo);
        ArgumentNullException.ThrowIfNull(branch);
        ArgumentException.ThrowIfNullOrEmpty(migrationsPath);
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(repository);

        var directory = migrationsPath.Replace('\\', '/').TrimEnd('/');

        foreach (var path in gitRepo.ListFiles(branch.CommitHash, directory))
        {
            var fileName = Path.GetFileName(path);
            if (!fileName.EndsWith(MigrationFileExtension, StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(DesignerFileSuffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var content = gitRepo.GetFileContent(branch.CommitHash, path);
            if (content is null)
                continue;

            var migration = parser.ParseMigrationFile(new MigrationFile
            {
                FilePath = path,
                FileName = fileName,
                DirectoryPath = directory,
                FileSize = content.Length,
                Content = content,
                DbContextName = DefaultDbContextName
            });

            if (migration is null)
                continue;

            if (!repository.Exists(migration.Id))
                repository.Add(migration);

            branch.AddMigration(migration.Id);
        }
    }

    /// <summary>
    /// Builds the command result for a compare exit code. Codes 0 and 1 are successful outcomes; the rest are failures.
    /// </summary>
    /// <param name="exitCode">The exit code to report.</param>
    /// <returns>The command result carrying the exit code.</returns>
    private static CommandResult CreateExitResult(int exitCode) => exitCode switch
    {
        Constants.ExitCodes.NoDiff => CommandResult.Ok(Messages.NoChanges),
        Constants.ExitCodes.DiffFound => new CommandResult
        {
            Success = true,
            Message = Messages.NonBreakingChanges,
            ExitCode = exitCode
        },
        Constants.ExitCodes.BreakingChangesFound => CommandResult.Error(Messages.BreakingChangesError, exitCode),
        _ => CommandResult.Error(Messages.BlockingConflictsError, exitCode)
    };

    /// <summary>
    /// Executes migration comparison between source and target branches.
    /// Validates arguments, initializes repositories, performs diff analysis, and generates reports.
    /// </summary>
    public async Task<CommandResult> ExecuteAsync(CommandContext context)
    {
        var appSettings = context.ServiceProvider.GetService<AppSettings>()
        ?? throw new InvalidOperationException(Messages.AppSettingsNotFound);

        var diffService = context.ServiceProvider.GetService<MigrationDiffService>()
        ?? throw new InvalidOperationException(Messages.DiffServiceNotFound);

        var reportService = context.ServiceProvider.GetService<ReportGenerationService>()
        ?? throw new InvalidOperationException(Messages.ReportServiceNotFound);

        // Get source and target branches from arguments
        string sourceBranch = context.ParsedArguments.Count > 0
            ? context.ParsedArguments[0]
            : appSettings.SourceBranch;

        string targetBranch = context.ParsedArguments.Count > 1
            ? context.ParsedArguments[1]
            : appSettings.TargetBranch;

        // Check for optional format override
        string? format = context.GetOption(OptionNames.Format);

        context.WriteColoredOutput(string.Format(Messages.Comparing, sourceBranch, targetBranch), ConsoleColor.Cyan);

        appSettings.RepositoryPath = Environment.CurrentDirectory;

        // Initialize and validate repository
        var gitRepo = new GitRepository(appSettings.RepositoryPath);
        if (!gitRepo.Initialize())
        {
            return CommandResult.Error(string.Format(Messages.RepositoryInitFailed, appSettings.RepositoryPath));
        }

        var source = gitRepo.GetBranch(sourceBranch);
        var target = gitRepo.GetBranch(targetBranch);

        if (source is null)
            return CommandResult.Error(
                string.Format(Messages.SourceBranchNotFound, sourceBranch),
                Constants.ExitCodes.UsageError);

        if (target is null)
            return CommandResult.Error(
                string.Format(Messages.TargetBranchNotFound, targetBranch),
                Constants.ExitCodes.UsageError);

        // Perform comparison
        var migrationRepository = context.ServiceProvider.GetService<MigrationRepository>()
        ?? throw new InvalidOperationException(Messages.MigrationRepositoryNotFound);

        var parserService = context.ServiceProvider.GetService<MigrationParserService>()
        ?? throw new InvalidOperationException(Messages.ParserServiceNotFound);

        LoadBranchMigrations(gitRepo, source, appSettings.MigrationsPath, parserService, migrationRepository);
        LoadBranchMigrations(gitRepo, target, appSettings.MigrationsPath, parserService, migrationRepository);
        var diff = diffService.CompareBranches(source, target);

        // Optional DOT graph export
        string? dotExportPath = context.GetOption(OptionNames.DotExport);
        if (!string.IsNullOrEmpty(dotExportPath))
        {
            var graphService = context.ServiceProvider.GetService<MigrationDependencyGraphService>()
            ?? throw new InvalidOperationException(Messages.GraphServiceNotFound);

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
            context.WriteColoredOutput(string.Format(Messages.DotExported, resolvedPath), ConsoleColor.Green);
        }

        // Check for summary mode
        bool isSummary = context.GetOption(OptionNames.Summary) is not null;

        // Display summary
        context.WriteColoredOutput(Messages.ComparisonCompleted, ConsoleColor.Green);
        context.WriteOutput(string.Format(Messages.ConflictsDetected, diff.Conflicts.Count));
        context.WriteOutput(string.Format(Messages.SchemaChanges, diff.GetTotalSchemaChanges()));
        context.WriteOutput(string.Format(Messages.Result, diff.Result));

        var statistics = diffService.GetDiffStatistics(diff);
        context.WriteOutput(string.Format(
            Messages.TablesStatistics,
            statistics.TablesAdded,
            statistics.TablesRemoved,
            statistics.TablesModified));
        context.WriteOutput(string.Format(
            Messages.ColumnsStatistics,
            statistics.ColumnsAdded,
            statistics.ColumnsRemoved,
            statistics.ColumnsModified));
        context.WriteOutput(string.Format(Messages.IndexesStatistics, statistics.IndexesChanged));

        if (isSummary)
        {
            context.WriteOutput(string.Format(
                Messages.SummaryLine,
                diff.OnlyInSource.Count,
                diff.OnlyInTarget.Count,
                diff.Conflicts.Count));
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
                appSettings.GetReportFilename(ReportNames.ComparisonBase));

            var reportContent = appSettings.ReportFormat switch
            {
                Constants.ReportFormats.Json => reportService.GenerateJsonReport(diff),
                Constants.ReportFormats.Html => reportService.GenerateHtmlReport(diff),
                _ => reportService.GenerateTextReport(diff)
            };

            File.WriteAllText(reportPath, reportContent);

            // When JSON format is requested, emit the report to stdout so callers can pipe
            // or capture it directly without locating the output file. Status messages are
            // sent to stderr to keep stdout clean for machine consumption.
            if (appSettings.ReportFormat == Constants.ReportFormats.Json)
            {
                context.ErrorOutput.WriteLine(string.Format(Messages.ReportSaved, reportPath));
                context.WriteOutput(reportContent);
            }
            else
            {
                context.WriteColoredOutput(string.Format(Messages.ReportSaved, reportPath), ConsoleColor.Green);
            }
        }

        gitRepo.Dispose();

        var exitCode = DetermineExitCode(diff);
        switch (exitCode)
        {
            case Constants.ExitCodes.ConflictsFound:
                context.WriteColoredOutput(Messages.BlockingConflictsWarning, ConsoleColor.Yellow);
                break;
            case Constants.ExitCodes.BreakingChangesFound:
                context.WriteColoredOutput(Messages.BreakingChangesWarning, ConsoleColor.Yellow);
                break;
        }

        return CreateExitResult(exitCode);
    }
}
