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
using EfMigrationDiff.Analysis;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Custom analysis example: Extend ef-migration-diff with custom logic
/// Shows how to use the analysis engine for custom business rules
/// </summary>
class CustomAnalysisExample
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddScoped<MigrationParserService>();
        services.AddScoped<MigrationRepository>();
        services.AddScoped<SchemaChangeDetectorService>();

        var provider = services.BuildServiceProvider();
        var parserService = provider.GetRequiredService<MigrationParserService>();
        var schemaDetector = provider.GetRequiredService<SchemaChangeDetectorService>();

        try
        {
            Console.WriteLine("🔬 Custom Migration Analysis\n");

            // Example 1: Analyze performance impact
            await AnalyzePerformanceImpact();

            // Example 2: Check compliance rules
            await CheckComplianceRules();

            // Example 3: Estimate migration complexity
            await EstimateMigrationComplexity();

            Console.WriteLine("\n✅ Custom analysis completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static async Task AnalyzePerformanceImpact()
    {
        Console.WriteLine("⚡ Performance Impact Analysis\n");

        var concerns = new List<string>();

        // Check for full table scans
        concerns.Add("Full table scan on Users table (new query without index)");

        // Check for large data migrations
        concerns.Add("Copying 5M+ records in Products table");

        // Check for lock scenarios
        concerns.Add("Adding NOT NULL column without DEFAULT (may lock table)");

        if (concerns.Any())
        {
            Console.WriteLine("⚠️  Potential Performance Issues:");
            foreach (var concern in concerns)
            {
                Console.WriteLine($"   • {concern}");
            }
        }

        Console.WriteLine();
    }

    static async Task CheckComplianceRules()
    {
        Console.WriteLine("🔒 Compliance Rules Check\n");

        var rules = new Dictionary<string, (bool passed, string message)>
        {
            {
                "GDPR: No PII in logs",
                (true, "✓ No personal data in migration scripts")
            },
            {
                "Data Retention: 30-day backup",
                (false, "❌ Migration doesn't ensure backup exists")
            },
            {
                "Encryption: Sensitive columns encrypted",
                (true, "✓ SSN field uses encryption")
            },
            {
                "Audit Trail: Changes logged",
                (true, "✓ Audit tables created")
            }
        };

        foreach (var (rule, result) in rules)
        {
            var status = result.passed ? "✅" : "❌";
            Console.WriteLine($"{status} {rule}");
            Console.WriteLine($"   {result.message}\n");
        }
    }

    static async Task EstimateMigrationComplexity()
    {
        Console.WriteLine("📊 Migration Complexity Estimation\n");

        var migrations = new List<MigrationComplexity>
        {
            new MigrationComplexity
            {
                Name = "AddUsersTable",
                Lines = 45,
                Changes = 1,
                ComplexityScore = 2,
                EstimatedTimeMinutes = 5,
                RiskLevel = "Low"
            },
            new MigrationComplexity
            {
                Name = "RefactorOrdersSchema",
                Lines = 250,
                Changes = 12,
                ComplexityScore = 8,
                EstimatedTimeMinutes = 30,
                RiskLevel = "Medium"
            },
            new MigrationComplexity
            {
                Name = "MigrateHistoricalData",
                Lines = 800,
                Changes = 1,
                ComplexityScore = 9,
                EstimatedTimeMinutes = 120,
                RiskLevel = "High"
            }
        };

        var totalComplexity = migrations.Sum(m => m.ComplexityScore);
        var totalTime = migrations.Sum(m => m.EstimatedTimeMinutes);

        Console.WriteLine($"{'Migration',-35} {'Score',-8} {'Time',-10} {'Risk',-10}");
        Console.WriteLine(new string('-', 63));

        foreach (var m in migrations.OrderByDescending(m => m.ComplexityScore))
        {
            Console.WriteLine($"{m.Name,-35} {m.ComplexityScore,-8} {m.EstimatedTimeMinutes}min{'',-4} {m.RiskLevel,-10}");
        }

        Console.WriteLine(new string('-', 63));
        Console.WriteLine($"{'Total',-35} {totalComplexity,-8} {totalTime}min");
        Console.WriteLine();

        Console.WriteLine("📈 Recommendations:");
        if (totalComplexity > 20)
        {
            Console.WriteLine("   • Consider breaking into smaller migrations");
        }
        if (totalTime > 60)
        {
            Console.WriteLine("   • Plan maintenance window of at least {totalTime + 15} minutes");
        }
        if (migrations.Any(m => m.RiskLevel == "High"))
        {
            Console.WriteLine("   • Perform dry run in staging environment");
            Console.WriteLine("   • Have rollback plan ready");
        }

        Console.WriteLine();
    }
}

class MigrationComplexity
{
    public string Name { get; set; } = "";
    public int Lines { get; set; }
    public int Changes { get; set; }
    public int ComplexityScore { get; set; }
    public int EstimatedTimeMinutes { get; set; }
    public string RiskLevel { get; set; } = "";
}
