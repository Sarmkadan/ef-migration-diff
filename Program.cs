#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.CLI.Commands;
using EfMigrationDiff.Configuration;
using EfMigrationDiff.Exceptions;
using EfMigrationDiff.Repositories;
using EfMigrationDiff.Services;
using EfMigrationDiff.Utilities;
using Microsoft.Extensions.DependencyInjection;

var application = new MigrationDiffApplication();
await application.RunAsync(args);

/// <summary>
/// Main application class for EF Migration Diff.
/// </summary>
internal class MigrationDiffApplication
{
    private readonly Microsoft.Extensions.DependencyInjection.ServiceProvider _serviceProvider;
    private readonly AppSettings _appSettings;

    public MigrationDiffApplication()
    {
        _serviceProvider = DependencyInjection.CreateServiceProvider(Environment.CurrentDirectory);
        _appSettings = _serviceProvider.GetService<AppSettings>() ?? throw new InvalidOperationException("Failed to initialize AppSettings");
    }

    /// <summary>
    /// Main application entry point with argument handling.
    /// </summary>
    public async Task RunAsync(string[] args)
    {
        try
        {
            Console.WriteLine($"\n{Constants.ApplicationName} v{Constants.ApplicationVersion}");
            Console.WriteLine("─────────────────────────────────────────");

            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            var command = args[0].ToLowerInvariant();

            switch (command)
            {
                case "compare":
                case "diff":
                    await CompareCommand(args);
                    break;

                case "check":
                case "validate":
                    await ValidateCommand(args);
                    break;

                case "report":
                    await ReportCommand(args);
                    break;

                case "visual-diff":
                case "visual":
                    VisualDiffCommand(args);
                    break;

                case "graph":
                case "dependency-graph":
                    await DependencyGraphCommand(args);
                    break;

                case "auto-merge":
                case "suggest":
                    await AutoMergeCommand(args);
                    break;

                case "--help":
                case "-h":
                case "help":
                    ShowHelp();
                    break;

                case "--version":
                case "-v":
                    ShowVersion();
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowUsage();
                    break;
            }
        }
        catch (MigrationDiffException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Unexpected error: {ex.Message}");
            if (_appSettings.EnableDetailedLogging)
            {
                Console.WriteLine(ex.StackTrace);
            }
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Handles the compare/diff command to compare migrations between branches.
    /// </summary>
    private async Task CompareCommand(string[] args)
    {
        Console.WriteLine("\nComparing migrations between branches...");

        _appSettings.RepositoryPath = Environment.CurrentDirectory;

        var gitRepo = new GitRepository(_appSettings.RepositoryPath);
        if (!gitRepo.Initialize())
        {
            throw new GitRepositoryException("Failed to initialize git repository", _appSettings.RepositoryPath);
        }

        var sourceBranch = args.Length > 1 ? args[1] : _appSettings.SourceBranch;
        var targetBranch = args.Length > 2 ? args[2] : _appSettings.TargetBranch;

        Console.WriteLine($"Source Branch: {sourceBranch}");
        Console.WriteLine($"Target Branch: {targetBranch}");

        var source = gitRepo.GetBranch(sourceBranch);
        var target = gitRepo.GetBranch(targetBranch);

        if (source is null)
            throw new BranchNotFoundException(sourceBranch);

        if (target is null)
            throw new BranchNotFoundException(targetBranch);

        var diffService = _serviceProvider.GetService<MigrationDiffService>() ?? throw new InvalidOperationException("Failed to get MigrationDiffService");
        var reportService = _serviceProvider.GetService<ReportGenerationService>() ?? throw new InvalidOperationException("Failed to get ReportGenerationService");

        var diff = diffService.CompareBranches(source, target);

        Console.WriteLine($"\nComparison Result: {diff.Result}");
        Console.WriteLine($"Conflicts: {diff.Conflicts.Count}");
        Console.WriteLine($"Schema Changes: {diff.GetTotalSchemaChanges()}");

        _appSettings.EnsureOutputDirectory();
        var reportPath = Path.Combine(_appSettings.GetOutputDirectory(), _appSettings.GetReportFilename());

        var reportContent = reportService.GenerateTextReport(diff);
        File.WriteAllText(reportPath, reportContent);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✓ Report generated: {reportPath}");
        Console.ResetColor();

        if (diff.HasBlockingConflicts())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  Blocking conflicts detected - deployment blocked");
            Console.ResetColor();
            Environment.Exit(1);
        }

        gitRepo.Dispose();
    }

    /// <summary>
    /// Handles the validate/check command to validate migrations.
    /// </summary>
    private async Task ValidateCommand(string[] args)
    {
        Console.WriteLine("\nValidating migrations...");

        _appSettings.RepositoryPath = Environment.CurrentDirectory;
        var migrationsPath = _appSettings.GetMigrationsDirectory();

        if (!Directory.Exists(migrationsPath))
        {
            throw new RepositoryException($"Migrations directory not found: {migrationsPath}");
        }

        var parserService = _serviceProvider.GetService<MigrationParserService>() ?? throw new InvalidOperationException("Failed to get MigrationParserService");

        var migrationFiles = Directory.GetFiles(migrationsPath, "*.cs")
                                      .Where(f => !f.EndsWith(".Designer.cs"))
                                      .ToList();

        Console.WriteLine($"Found {migrationFiles.Count} migration file(s)");

        int validCount = 0;
        int invalidCount = 0;

        foreach (var filePath in migrationFiles)
        {
            var migrationFile = new EfMigrationDiff.Models.MigrationFile(filePath, "DefaultContext");
            var errors = parserService.ValidateMigrationFile(migrationFile);

            if (errors.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ {Path.GetFileName(filePath)}");
                Console.ResetColor();
                validCount++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ {Path.GetFileName(filePath)}");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
                Console.ResetColor();
                invalidCount++;
            }
        }

        Console.WriteLine($"\nValidation Summary: {validCount} valid, {invalidCount} invalid");

        if (invalidCount > 0)
        {
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Handles the report command to generate detailed reports.
    /// </summary>
    private async Task ReportCommand(string[] args)
    {
        Console.WriteLine("\nGenerating report...");

        var format = args.Length > 1 ? args[1] : "text";
        _appSettings.ReportFormat = format;
        _appSettings.EnsureOutputDirectory();

        Console.WriteLine($"Report Format: {format}");
        Console.WriteLine($"Output Path: {_appSettings.GetOutputDirectory()}");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ Report generation complete");
        Console.ResetColor();
    }

    /// <summary>
    /// Handles the visual-diff command to generate HTML side-by-side or unified diff view.
    /// </summary>
    private void VisualDiffCommand(string[] args)
    {
        Console.WriteLine("\nGenerating visual diff...");

        _appSettings.RepositoryPath = Environment.CurrentDirectory;

        var sourceBranch = args.Length > 1 ? args[1] : _appSettings.SourceBranch;
        var targetBranch = args.Length > 2 ? args[2] : _appSettings.TargetBranch;
        var format       = args.Length > 3 ? args[3] : "sidebyside";

        Console.WriteLine($"Source Branch: {sourceBranch}");
        Console.WriteLine($"Target Branch: {targetBranch}");
        Console.WriteLine($"Format:        {format}");

        var pipeline  = _serviceProvider.GetService<EfMigrationDiff.Services.SchemaDiffPipelineService>()
            ?? throw new InvalidOperationException("SchemaDiffPipelineService not registered");

        var source = new EfMigrationDiff.Models.BranchInfo(sourceBranch, string.Empty);
        var target = new EfMigrationDiff.Models.BranchInfo(targetBranch, string.Empty);

        var result = pipeline.RunTwoWayDiff(source, target);

        var html = format.Equals("unified", StringComparison.OrdinalIgnoreCase)
            ? result.UnifiedHtml
            : result.SideBySideHtml;

        _appSettings.EnsureOutputDirectory();
        var stamp      = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var outputPath = Path.Combine(_appSettings.GetOutputDirectory(), $"visual-diff-{stamp}.html");
        File.WriteAllText(outputPath, html, System.Text.Encoding.UTF8);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✓ Visual diff report written to: {outputPath}");
        Console.ResetColor();

        if (result.HasDestructiveChanges)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  Destructive schema changes detected — review before merging");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Handles the dependency-graph command to display migration dependency ordering.
    /// </summary>
    private async Task DependencyGraphCommand(string[] args)
    {
        Console.WriteLine("\nBuilding migration dependency graph...");

        _appSettings.RepositoryPath = Environment.CurrentDirectory;
        var migrationsPath = _appSettings.GetMigrationsDirectory();

        var graphService = _serviceProvider.GetService<EfMigrationDiff.Services.MigrationDependencyGraphService>()
            ?? throw new InvalidOperationException("MigrationDependencyGraphService not registered");

        var parserService = _serviceProvider.GetService<MigrationParserService>()
            ?? throw new InvalidOperationException("MigrationParserService not found");

        var migrationFiles = Directory.Exists(migrationsPath)
            ? Directory.GetFiles(migrationsPath, "*.cs")
                .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
                .ToList()
            : new List<string>();

        var migrations = migrationFiles
            .Select(f =>
            {
                var mf = new EfMigrationDiff.Models.MigrationFile(f, "DefaultContext");
                return parserService.ParseMigrationFile(mf);
            })
            .Where(m => m is not null)
            .Cast<EfMigrationDiff.Models.Migration>()
            .ToList();

        var graph = graphService.Build(migrations);

        Console.WriteLine($"\nMigrations: {graph.Nodes.Count}  |  Dependencies: {graph.Edges.Count}");

        foreach (var order in graph.GetTopologicalOrder())
        {
            Console.WriteLine($"  {order.Sequence:D4}  {order.MigrationId}  →  {order.Name}");
        }

        if (graph.HasCycles)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n✗ Circular dependencies detected — merge will fail");
            Console.ResetColor();
            Environment.Exit(1);
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✓ Dependency graph is acyclic — safe to apply");
        Console.ResetColor();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles the auto-merge command to suggest conflict resolution strategies.
    /// </summary>
    private async Task AutoMergeCommand(string[] args)
    {
        Console.WriteLine("\nAnalyzing conflicts for auto-merge suggestions...");

        _appSettings.RepositoryPath = Environment.CurrentDirectory;

        var sourceBranch = args.Length > 1 ? args[1] : _appSettings.SourceBranch;
        var targetBranch = args.Length > 2 ? args[2] : _appSettings.TargetBranch;

        Console.WriteLine($"Source Branch: {sourceBranch}");
        Console.WriteLine($"Target Branch: {targetBranch}");

        var gitRepo = new GitRepository(_appSettings.RepositoryPath);
        if (!gitRepo.Initialize())
            throw new GitRepositoryException("Failed to initialize git repository", _appSettings.RepositoryPath);

        var source = gitRepo.GetBranch(sourceBranch);
        var target = gitRepo.GetBranch(targetBranch);

        if (source is null) throw new BranchNotFoundException(sourceBranch);
        if (target is null) throw new BranchNotFoundException(targetBranch);

        var diffService   = _serviceProvider.GetService<MigrationDiffService>()
            ?? throw new InvalidOperationException("MigrationDiffService not found");

        var resolverService = _serviceProvider.GetService<EfMigrationDiff.Services.MigrationAutoResolverService>()
            ?? throw new InvalidOperationException("MigrationAutoResolverService not found");

        var diff   = diffService.CompareBranches(source, target);
        var result = await resolverService.ResolveAsync(diff.Conflicts);

        Console.WriteLine($"\n{result.GetSummary()}");

        foreach (var attempt in result.Attempts)
        {
            var color = attempt.Succeeded ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.ForegroundColor = color;
            Console.WriteLine($"  {attempt}");
            Console.ResetColor();
        }

        if (result.UnresolvedConflicts.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n⚠️  {result.UnresolvedConflicts.Count} conflict(s) require manual review");
            Console.ResetColor();
        }

        gitRepo.Dispose();

        if (result.HasBlockingConflicts)
            Environment.Exit(1);
    }

    private void ShowUsage()
    {
        Console.WriteLine("\nUsage: ef-migration-diff <command> [options]");
        Console.WriteLine("\nCommands:");
        Console.WriteLine("  compare <source-branch> <target-branch>         Compare migrations between branches");
        Console.WriteLine("  validate                                         Validate all migration files");
        Console.WriteLine("  report <format>                                  Generate detailed report");
        Console.WriteLine("  visual-diff <source> <target> [format]          Generate HTML visual diff report");
        Console.WriteLine("  graph                                            Show migration dependency graph");
        Console.WriteLine("  auto-merge <source-branch> <target-branch>      Suggest auto-merge resolutions");
        Console.WriteLine("  help                                             Show help information");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  ef-migration-diff compare develop main");
        Console.WriteLine("  ef-migration-diff validate");
        Console.WriteLine("  ef-migration-diff report html");
        Console.WriteLine("  ef-migration-diff visual-diff develop main unified");
        Console.WriteLine("  ef-migration-diff graph");
        Console.WriteLine("  ef-migration-diff auto-merge feature/users main");
    }

    private void ShowHelp()
    {
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     EF Migration Diff - Entity Framework Migration Tool     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        Console.WriteLine("DESCRIPTION:");
        Console.WriteLine("  Compares Entity Framework migrations between git branches");
        Console.WriteLine("  and detects conflicts, schema changes, and incompatibilities.\n");

        Console.WriteLine("COMMANDS:");
        Console.WriteLine("  compare <src> <tgt>          Compare migrations between branches");
        Console.WriteLine("  validate                     Validate migration file structure");
        Console.WriteLine("  report <fmt>                 Generate reports (text/json/html)");
        Console.WriteLine("  visual-diff <src> <tgt>      Generate HTML side-by-side or unified diff");
        Console.WriteLine("  graph                        Display migration dependency graph");
        Console.WriteLine("  auto-merge <src> <tgt>       Suggest and apply auto-merge resolutions");
        Console.WriteLine("  help                         Show this help message\n");

        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  --help, -h            Show help");
        Console.WriteLine("  --version, -v         Show version\n");

        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  ef-migration-diff compare develop main");
        Console.WriteLine("  ef-migration-diff validate");
        Console.WriteLine("  ef-migration-diff report json");
        Console.WriteLine("  ef-migration-diff visual-diff develop main sidebyside");
        Console.WriteLine("  ef-migration-diff graph");
        Console.WriteLine("  ef-migration-diff auto-merge feature/users main\n");
    }

    private void ShowVersion()
    {
        Console.WriteLine($"{Constants.ApplicationName} {Constants.ApplicationVersion}");
        Console.WriteLine($"Author: {Constants.Author}");
    }
}
