#nullable enable
using System;
using System.Threading.Tasks;
using EfMigrationDiff.Services;
using EfMigrationDiff.Repositories;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Basic Usage Example:
/// Demonstrates the simplest way to initialize the services and perform a basic migration diff.
/// </summary>
class BasicUsage
{
    static async Task Main(string[] args)
    {
        // 1. Setup DI container
        var services = new ServiceCollection();
        
        // Register necessary services
        services.AddScoped<GitRepository>();
        services.AddScoped<MigrationRepository>();
        services.AddScoped<MigrationDiffService>();
        
        var provider = services.BuildServiceProvider();
        var diffService = provider.GetRequiredService<MigrationDiffService>();

        // 2. Perform the diff
        Console.WriteLine("Running basic migration diff...");
        
        // Compare current branch against main
        var diff = await diffService.CompareBranchesAsync("main", "feature/my-migration");

        // 3. Output basic results
        Console.WriteLine($"Found {diff.MigrationFiles.Count} differences.");
        
        foreach (var file in diff.MigrationFiles)
        {
            Console.WriteLine($"Migration: {file.Name} | Action: {file.Action}");
        }
    }
}
