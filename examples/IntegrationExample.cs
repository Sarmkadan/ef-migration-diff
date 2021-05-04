using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EfMigrationDiff.Extensions;
using EfMigrationDiff.Services;

/// <summary>
/// Integration Example:
/// Demonstrates how to wire the EfMigrationDiff services into an ASP.NET Core DI container.
/// </summary>
class IntegrationExample
{
    static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Register EfMigrationDiff services
        builder.Services.AddSchemaDiffServices(() => new EfMigrationDiff.Configuration.SchemaDiffOptions {
            TimeoutSeconds = 60
        });
        builder.Services.AddSchemaDiffPipeline();

        using IHost host = builder.Build();

        // Resolve and use the pipeline
        var pipeline = host.Services.GetRequiredService<SchemaDiffPipelineService>();
        
        Console.WriteLine("Services integrated successfully.");
    }
}
