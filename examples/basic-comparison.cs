// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;
using EfMigrationDiff.Services;
using EfMigrationDiff.Repositories;
using EfMigrationDiff.Models;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Basic example: Compare migrations between two Git branches
/// Usage: dotnet run --project examples/basic-comparison.cs
/// </summary>
class BasicComparisonExample
{
    static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddScoped<GitRepository>();
        services.AddScoped<MigrationRepository>();
        services.AddScoped<MigrationDiffService>();
        services.AddScoped<ConflictDetectionService>();
        services.AddScoped<SchemaChangeDetectorService>();

        var provider = services.BuildServiceProvider();
        var diffService = provider.GetRequiredService<MigrationDiffService>();

        try
        {
            Console.WriteLine("🚀 Starting migration comparison...\n");

            // Compare migrations between main and feature branch
            var result = await diffService.CompareBranchesAsync(
                branch1: "main",
                branch2: "feature/users",
                options: new ComparisonOptions
                {
                    IncludeSchemaPreview = true,
                    DetectBreakingChanges = false
                });

            // Display results
            Console.WriteLine($"📊 Comparison Results:");
            Console.WriteLine($"   Has Differences: {result.HasDifferences}");
            Console.WriteLine($"   Conflicts Found: {result.Conflicts.Count}");
            Console.WriteLine($"   Schema Changes: {result.SchemaChanges.Count}\n");

            // Display conflicts
            if (result.Conflicts.Count > 0)
            {
                Console.WriteLine("⚠️  Conflicts Detected:");
                foreach (var conflict in result.Conflicts)
                {
                    Console.WriteLine($"   - {conflict.MigrationName} ({conflict.ConflictType})");
                }
            }

            // Display schema changes
            if (result.SchemaChanges.Count > 0)
            {
                Console.WriteLine("\n📝 Schema Changes:");
                foreach (var change in result.SchemaChanges)
                {
                    Console.WriteLine($"   - {change.TableName}: {change.OperationType}");
                }
            }

            Console.WriteLine("\n✅ Comparison completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}

/// <summary>Comparison options for branch analysis</summary>
public class ComparisonOptions
{
    public bool IncludeSchemaPreview { get; set; }
    public bool DetectBreakingChanges { get; set; }
}
