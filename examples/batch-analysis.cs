#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EfMigrationDiff.Services;
using EfMigrationDiff.Repositories;
using EfMigrationDiff.Models;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Batch analysis example: Compare multiple feature branches against main in one run
/// Useful for CI/CD pipelines analyzing multiple pull requests
/// </summary>
class BatchAnalysisExample
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddScoped<MigrationDiffService>();
        services.AddScoped<GitRepository>();
        services.AddScoped<MigrationRepository>();
        services.AddScoped<ConflictDetectionService>();

        var provider = services.BuildServiceProvider();
        var diffService = provider.GetRequiredService<MigrationDiffService>();
        var gitRepo = provider.GetRequiredService<GitRepository>();

        try
        {
            Console.WriteLine("🔄 Starting batch migration analysis...\n");

            // Get all feature branches
            var branches = await GetFeatureBranches(gitRepo);

            if (!branches.Any())
            {
                Console.WriteLine("ℹ️  No feature branches found to analyze.");
                return;
            }

            Console.WriteLine($"📊 Analyzing {branches.Count} branches against main...\n");

            Directory.CreateDirectory("./batch-reports");
            var results = new List<BatchAnalysisResult>();

            // Analyze each branch
            foreach (var branch in branches)
            {
                Console.WriteLine($"🔍 Analyzing {branch}...");

                try
                {
                    var comparison = await diffService.CompareBranchesAsync(
                        "main",
                        branch,
                        new ComparisonOptions { IncludeSchemaPreview = true });

                    var result = new BatchAnalysisResult
                    {
                        Branch = branch,
                        HasConflicts = comparison.Conflicts.Any(),
                        ConflictCount = comparison.Conflicts.Count,
                        SchemaChangeCount = comparison.SchemaChanges.Count,
                        Status = "Success"
                    };

                    results.Add(result);
                    Console.WriteLine($"   ✓ Complete\n");
                }
                catch (Exception ex)
                {
                    results.Add(new BatchAnalysisResult
                    {
                        Branch = branch,
                        Status = "Failed",
                        ErrorMessage = ex.Message
                    });
                    Console.WriteLine($"   ❌ Error: {ex.Message}\n");
                }
            }

            // Generate summary report
            DisplaySummaryReport(results);
            await SaveBatchResults(results);

            Environment.Exit(results.Any(r => r.HasConflicts) ? 1 : 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static async Task<List<string>> GetFeatureBranches(GitRepository gitRepo)
    {
        var allBranches = await gitRepo.GetAllBranchesAsync();
        return allBranches
            .Where(b => b.StartsWith("feature/") || b.StartsWith("bugfix/"))
            .ToList();
    }

    static void DisplaySummaryReport(List<BatchAnalysisResult> results)
    {
        Console.WriteLine("=" * 60);
        Console.WriteLine("📋 Batch Analysis Summary");
        Console.WriteLine("=" * 60);

        var successful = results.Count(r => r.Status == "Success");
        var failed = results.Count(r => r.Status == "Failed");
        var withConflicts = results.Count(r => r.HasConflicts);

        Console.WriteLine($"\n✓ Successful: {successful}/{results.Count}");
        Console.WriteLine($"✗ Failed: {failed}");
        Console.WriteLine($"⚠️  With Conflicts: {withConflicts}\n");

        Console.WriteLine("Detailed Results:\n");
        Console.WriteLine($"{'Branch',-35} {'Conflicts',-12} {'Changes',-10} {'Status',-10}");
        Console.WriteLine(new string('-', 67));

        foreach (var result in results.OrderByDescending(r => r.HasConflicts))
        {
            var conflictStr = result.HasConflicts ? $"⚠️  {result.ConflictCount}" : "✓ None";
            Console.WriteLine($"{result.Branch,-35} {conflictStr,-12} {result.SchemaChangeCount,-10} {result.Status,-10}");
        }

        Console.WriteLine("\n" + new string('=', 60));
    }

    static async Task SaveBatchResults(List<BatchAnalysisResult> results)
    {
        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync("./batch-reports/summary.json", json);
        Console.WriteLine("\n✓ Batch results saved to ./batch-reports/summary.json");
    }
}

class BatchAnalysisResult
{
    public string Branch { get; set; } = "";
    public bool HasConflicts { get; set; }
    public int ConflictCount { get; set; }
    public int SchemaChangeCount { get; set; }
    public string Status { get; set; } = "";
    public string? ErrorMessage { get; set; }
}

public class ComparisonOptions
{
    public bool IncludeSchemaPreview { get; set; }
}
