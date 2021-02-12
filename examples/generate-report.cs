// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.IO;
using System.Threading.Tasks;
using EfMigrationDiff.Reports;
using EfMigrationDiff.Services;
using EfMigrationDiff.Repositories;
using EfMigrationDiff.Models;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Report generation example: Create HTML, JSON, and CSV reports
/// Useful for sharing analysis results with stakeholders
/// </summary>
class GenerateReportExample
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddScoped<MigrationDiffService>();
        services.AddScoped<ReportEngine>();
        services.AddScoped<GitRepository>();
        services.AddScoped<MigrationRepository>();

        var provider = services.BuildServiceProvider();
        var diffService = provider.GetRequiredService<MigrationDiffService>();
        var reportEngine = provider.GetRequiredService<ReportEngine>();

        try
        {
            Console.WriteLine("📄 Generating migration comparison reports...\n");

            // Get comparison results
            var result = await diffService.CompareBranchesAsync(
                "main",
                "feature/database-refactor",
                new ComparisonOptions { IncludeSchemaPreview = true });

            // Ensure output directory exists
            Directory.CreateDirectory("./reports");

            // Generate HTML report
            await GenerateHtmlReport(reportEngine, result);

            // Generate JSON report
            await GenerateJsonReport(reportEngine, result);

            // Generate CSV report
            await GenerateCsvReport(reportEngine, result);

            Console.WriteLine("✅ All reports generated successfully!");
            Console.WriteLine("\n📁 Reports created:");
            Console.WriteLine("   • ./reports/migration-diff.html");
            Console.WriteLine("   • ./reports/migration-diff.json");
            Console.WriteLine("   • ./reports/migration-diff.csv");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static async Task GenerateHtmlReport(ReportEngine engine, MigrationDiff result)
    {
        Console.WriteLine("📊 Generating HTML report...");

        var options = new ReportOptions
        {
            IncludeTimestamp = true,
            IncludeStatistics = true,
            IncludeRecommendations = true,
            Theme = "light"
        };

        var htmlContent = await engine.GenerateHtmlReportAsync(result, options);
        await File.WriteAllTextAsync("./reports/migration-diff.html", htmlContent);

        Console.WriteLine("   ✓ HTML report saved to ./reports/migration-diff.html");
    }

    static async Task GenerateJsonReport(ReportEngine engine, MigrationDiff result)
    {
        Console.WriteLine("🔹 Generating JSON report...");

        var jsonContent = await engine.GenerateJsonReportAsync(result);
        await File.WriteAllTextAsync("./reports/migration-diff.json", jsonContent);

        Console.WriteLine("   ✓ JSON report saved to ./reports/migration-diff.json");
    }

    static async Task GenerateCsvReport(ReportEngine engine, MigrationDiff result)
    {
        Console.WriteLine("📋 Generating CSV report...");

        var csvContent = await engine.GenerateCsvReportAsync(result);
        await File.WriteAllTextAsync("./reports/migration-diff.csv", csvContent);

        Console.WriteLine("   ✓ CSV report saved to ./reports/migration-diff.csv");
    }
}

public class ReportOptions
{
    public bool IncludeTimestamp { get; set; }
    public bool IncludeStatistics { get; set; }
    public bool IncludeRecommendations { get; set; }
    public string? Theme { get; set; } = "light";
}

public class ComparisonOptions
{
    public bool IncludeSchemaPreview { get; set; }
    public bool DetectBreakingChanges { get; set; }
}
