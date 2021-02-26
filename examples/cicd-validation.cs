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
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// CI/CD Validation example: Automated checks for pull requests
/// Prevents merging code with migration issues
/// Suitable for GitHub Actions, Azure Pipelines, GitLab CI
/// </summary>
class CicdValidationExample
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddScoped<MigrationDiffService>();
        services.AddScoped<ConflictDetectionService>();
        services.AddScoped<SchemaChangeDetectorService>();
        services.AddScoped<MigrationRepository>();
        services.AddScoped<GitRepository>();

        var provider = services.BuildServiceProvider();
        var diffService = provider.GetRequiredService<MigrationDiffService>();

        try
        {
            Console.WriteLine("🔐 Running CI/CD Migration Validation\n");

            var baseBranch = Environment.GetEnvironmentVariable("CI_BASE_BRANCH") ?? "origin/main";
            var headBranch = Environment.GetEnvironmentVariable("CI_HEAD_BRANCH") ?? "HEAD";

            Console.WriteLine($"📌 Configuration:");
            Console.WriteLine($"   Base Branch: {baseBranch}");
            Console.WriteLine($"   Head Branch: {headBranch}\n");

            // Run validation checks
            var checks = await RunValidationChecks(diffService, baseBranch, headBranch).ConfigureAwait(false);

            // Display results
            DisplayValidationResults(checks);

            // Determine exit code
            var hasFailures = checks.Any(c => !c.Passed && c.IsCritical);
            var exitCode = hasFailures ? 1 : 0;

            Console.WriteLine($"\n{'='*60}");
            Console.WriteLine($"Final Status: {(exitCode == 0 ? "✅ PASSED" : "❌ FAILED")}");
            Console.WriteLine($"{'='*60}");

            Environment.Exit(exitCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Validation Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static async Task<List<ValidationCheck>> RunValidationChecks(
        MigrationDiffService diffService,
        string baseBranch,
        string headBranch)
    {
        var checks = new List<ValidationCheck>();

        try
        {
            var comparison = await diffService.CompareBranchesAsync(
                baseBranch,
                headBranch,
                new ComparisonOptions
                {
                    IncludeSchemaPreview = true,
                    DetectBreakingChanges = true
                });

            // Check 1: No duplicate migrations
            checks.Add(new ValidationCheck
            {
                Name = "Duplicate Migration Names",
                Description = "Ensures no two migrations have the same name",
                Passed = !comparison.Conflicts.Any(c => c.ConflictType == "DuplicateNames"),
                IsCritical = true,
                Details = comparison.Conflicts
                    .Where(c => c.ConflictType == "DuplicateNames")
                    .Select(c => c.MigrationName)
                    .ToList()
            });

            // Check 2: No orphaned migrations
            checks.Add(new ValidationCheck
            {
                Name = "Orphaned Migrations",
                Description = "Ensures all migrations have proper dependency chains",
                Passed = !comparison.Conflicts.Any(c => c.ConflictType == "OrphanedMigration"),
                IsCritical = true,
                Details = comparison.Conflicts
                    .Where(c => c.ConflictType == "OrphanedMigration")
                    .Select(c => c.MigrationName)
                    .ToList()
            });

            // Check 3: No breaking changes (warning level)
            var hasBreakingChanges = comparison.SchemaChanges.Any(c =>
                c.OperationType == "DropTable" ||
                c.OperationType == "DropColumn");

            checks.Add(new ValidationCheck
            {
                Name = "Breaking Schema Changes",
                Description = "Warns about operations that could cause data loss",
                Passed = !hasBreakingChanges,
                IsCritical = false,
                Details = comparison.SchemaChanges
                    .Where(c => c.OperationType == "DropTable" || c.OperationType == "DropColumn")
                    .Select(c => $"{c.OperationType} on {c.TableName}")
                    .ToList()
            });

            // Check 4: Reasonable number of migrations
            checks.Add(new ValidationCheck
            {
                Name = "Migration File Count",
                Description = "Ensures reasonable number of new migrations (< 50)",
                Passed = comparison.AddedMigrations.Count() < 50,
                IsCritical = false,
                Details = new List<string> { $"New migrations: {comparison.AddedMigrations.Count()}" }
            });
        }
        catch (Exception ex)
        {
            checks.Add(new ValidationCheck
            {
                Name = "Comparison Error",
                Description = ex.Message,
                Passed = false,
                IsCritical = true
            });
        }

        return checks;
    }

    static void DisplayValidationResults(List<ValidationCheck> checks)
    {
        Console.WriteLine($"📋 Validation Checks Results\n");
        Console.WriteLine($"{'Check',-40} {'Status',-15} {'Level',-10}");
        Console.WriteLine(new string('-', 65));

        foreach (var check in checks)
        {
            var status = check.Passed ? "✅ PASS" : "❌ FAIL";
            var level = check.IsCritical ? "CRITICAL" : "WARNING";

            Console.WriteLine($"{check.Name,-40} {status,-15} {level,-10}");

            if (!check.Passed && check.Details.Any())
            {
                foreach (var detail in check.Details)
                {
                    Console.WriteLine($"   • {detail}");
                }
            }
        }

        Console.WriteLine();

        var passed = checks.Count(c => c.Passed);
        var failed = checks.Count(c => !c.Passed);

        Console.WriteLine($"Summary: {passed} passed, {failed} failed");
    }
}

class ValidationCheck
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Passed { get; set; }
    public bool IsCritical { get; set; }
    public List<string> Details { get; set; } = new();
}

public class ComparisonOptions
{
    public bool IncludeSchemaPreview { get; set; }
    public bool DetectBreakingChanges { get; set; }
}
