#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EfMigrationDiff.Services;
using EfMigrationDiff.Repositories;
using EfMigrationDiff.Models;
using EfMigrationDiff.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Library usage example: Use ef-migration-diff as a NuGet package in your own projects
/// Shows how to integrate the tool into web apps, services, and tools
/// </summary>
class LibraryUsageExample
{
    static async Task Main(string[] args)
    {
        // Setup DI and configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();

        // Register ef-migration-diff services
        services.AddScoped<MigrationDiffService>();
        services.AddScoped<ConflictDetectionService>();
        services.AddScoped<SchemaChangeDetectorService>();
        services.AddScoped<MigrationParserService>();
        services.AddScoped<MigrationRepository>();
        services.AddScoped<GitRepository>();
        services.AddScoped<ReportGenerationService>();

        services.AddSingleton(configuration);

        var provider = services.BuildServiceProvider();

        try
        {
            Console.WriteLine("📚 ef-migration-diff as Library\n");

            // Example 1: Simple integration in a web service
            await SimpleWebServiceIntegration(provider);

            // Example 2: Scheduled background job
            await ScheduledBackgroundJob(provider);

            // Example 3: REST API wrapper
            await RestApiWrapperExample(provider);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static async Task SimpleWebServiceIntegration(IServiceProvider provider)
    {
        Console.WriteLine("🌐 Scenario 1: Web Service Integration");
        Console.WriteLine("-----------------------------------------\n");

        var diffService = provider.GetRequiredService<MigrationDiffService>();

        // Simulating a web endpoint that compares migrations
        var result = await ComparisonEndpoint(diffService, "main", "feature/users");

        Console.WriteLine($"✓ Comparison result: {(result.HasDifferences ? "Differences found" : "No differences")}");
        Console.WriteLine($"  Conflicts: {result.Conflicts.Count}");
        Console.WriteLine($"  Schema changes: {result.SchemaChanges.Count}\n");
    }

    static async Task ScheduledBackgroundJob(IServiceProvider provider)
    {
        Console.WriteLine("⏰ Scenario 2: Scheduled Background Job");
        Console.WriteLine("-----------------------------------------\n");

        var diffService = provider.GetRequiredService<MigrationDiffService>();

        // Simulating a daily job that checks for migration conflicts
        Console.WriteLine("Running daily migration health check...");

        var branches = new[] { "develop", "staging", "release-candidate" };

        foreach (var branch in branches)
        {
            try
            {
                var result = await diffService.CompareBranchesAsync("main", branch);
                var status = result.Conflicts.Any() ? "⚠️  Issues" : "✓ Healthy";
                Console.WriteLine($"  {branch,-25} {status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {branch,-25} ❌ Error: {ex.Message}");
            }
        }

        Console.WriteLine();
    }

    static async Task RestApiWrapperExample(IServiceProvider provider)
    {
        Console.WriteLine("🔌 Scenario 3: REST API Wrapper");
        Console.WriteLine("---------------------------------\n");

        Console.WriteLine("Example endpoints you could expose:\n");

        var endpoints = new[]
        {
            "POST /api/migrations/compare",
            "  Request: { branch1: 'main', branch2: 'feature/users' }",
            "  Response: { hasConflicts: false, conflicts: [], schemaChanges: [] }",
            "",
            "GET /api/migrations/{branch}/validate",
            "  Response: { isValid: true, warnings: [] }",
            "",
            "POST /api/migrations/batch-analyze",
            "  Request: { branches: ['feature/a', 'feature/b', 'feature/c'] }",
            "  Response: { results: [...] }",
            ""
        };

        foreach (var endpoint in endpoints)
        {
            Console.WriteLine($"  {endpoint}");
        }
    }

    static async Task<ComparisonResult> ComparisonEndpoint(
        MigrationDiffService service,
        string branch1,
        string branch2)
    {
        return await service.CompareBranchesAsync(branch1, branch2);
    }
}

public class ComparisonResult
{
    public bool HasDifferences { get; set; }
    public List<ConflictInfo> Conflicts { get; set; } = new();
    public List<SchemaChange> SchemaChanges { get; set; } = new();
    public List<Migration> AddedMigrations { get; set; } = new();
    public List<Migration> RemovedMigrations { get; set; } = new();
}

public class MigrationDiff
{
    public List<ConflictInfo> Conflicts { get; set; } = new();
    public List<SchemaChange> SchemaChanges { get; set; } = new();
}

/// <summary>Example of a complete ASP.NET Core service integration</summary>
public class MigrationAnalysisService
{
    private readonly MigrationDiffService _diffService;

    public MigrationAnalysisService(MigrationDiffService diffService)
    {
        _diffService = diffService;
    }

    /// <summary>Check if a pull request has safe migrations</summary>
    public async Task<bool> IsPullRequestSafeAsync(string baseBranch, string featureBranch)
    {
        var comparison = await _diffService.CompareBranchesAsync(baseBranch, featureBranch);

        // Business logic: determine if safe to merge
        var hasCriticalConflicts = comparison.Conflicts.Any(c => c.Severity == "Critical");
        var hasDataLoss = comparison.SchemaChanges.Any(c =>
            c.OperationType == "DropTable" || c.OperationType == "DropColumn");

        return !hasCriticalConflicts && !hasDataLoss;
    }

    /// <summary>Generate a health report for database migrations</summary>
    public async Task<HealthReport> GetMigrationHealthAsync(string branch)
    {
        return new HealthReport
        {
            Branch = branch,
            IsHealthy = true,
            Timestamp = DateTime.UtcNow,
            Checks = new[]
            {
                "Migrations are properly named",
                "No circular dependencies",
                "All migrations can be applied"
            }
        };
    }
}

public class HealthReport
{
    public string Branch { get; set; } = "";
    public bool IsHealthy { get; set; }
    public DateTime Timestamp { get; set; }
    public IEnumerable<string> Checks { get; set; } = new List<string>();
}
