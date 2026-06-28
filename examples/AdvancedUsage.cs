#nullable enable
using System;
using System.Threading.Tasks;
using EfMigrationDiff.Services;
using EfMigrationDiff.Repositories;
using EfMigrationDiff.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Advanced Usage Example:
/// Demonstrates configuring the service, handling errors, and using specific options.
/// </summary>
class AdvancedUsage
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        
        // Register services (including configuration)
        services.AddScoped<GitRepository>();
        services.AddScoped<MigrationRepository>();
        services.AddScoped<MigrationDiffService>();
        services.AddSingleton(new SchemaDiffOptions { 
            EnableDetailedLogging = true,
            TimeoutSeconds = 30 
        });
        
        var provider = services.BuildServiceProvider();
        var diffService = provider.GetRequiredService<MigrationDiffService>();

        try
        {
            Console.WriteLine("Running advanced migration analysis...");

            // Performing comparison with custom options
            var result = await diffService.CompareBranchesAsync(
                "main", 
                "feature/complex-db-changes"
            );

            // Handle results based on content
            if (result.HasConflicts)
            {
                Console.WriteLine("Conflicts detected! Analyzing...");
                // Add custom logic to handle conflicts
            }
        }
        catch (TimeoutException)
        {
            Console.WriteLine("The migration analysis timed out.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }
}
