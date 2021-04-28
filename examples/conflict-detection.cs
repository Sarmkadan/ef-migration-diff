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
/// Conflict detection example: Identify migration conflicts and overlaps
/// Useful for teams working on parallel features
/// </summary>
class ConflictDetectionExample
{
    static async Task Main(string[] args)
    {
        var services = ConfigureServices();
        var provider = services.BuildServiceProvider();
        var conflictService = provider.GetRequiredService<ConflictDetectionService>();
        var migrationRepo = provider.GetRequiredService<MigrationRepository>();

        try
        {
            Console.WriteLine("🔍 Starting conflict detection...\n");

            // Load migrations from two branches
            var branch1Migrations = await migrationRepo.GetMigrationsAsync("main");
            var branch2Migrations = await migrationRepo.GetMigrationsAsync("feature/new-tables");

            // Detect conflicts
            var conflicts = await conflictService.DetectConflictsAsync(
                branch1Migrations,
                branch2Migrations);

            DisplayConflictReport(conflicts);

            // Provide recommendations
            var recommendations = GenerateRecommendations(conflicts);
            DisplayRecommendations(recommendations);

            Environment.Exit(conflicts.Any(c => c.Severity == ConflictSeverity.Critical) ? 1 : 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void DisplayConflictReport(IEnumerable<ConflictInfo> conflicts)
    {
        var conflictList = conflicts.ToList();

        Console.WriteLine($"📋 Conflict Report:");
        Console.WriteLine($"   Total Conflicts: {conflictList.Count}\n");

        var critical = conflictList.Count(c => c.Severity == ConflictSeverity.Critical);
        var warning = conflictList.Count(c => c.Severity == ConflictSeverity.Warning);
        var info = conflictList.Count(c => c.Severity == ConflictSeverity.Info);

        Console.WriteLine($"   🔴 Critical: {critical}");
        Console.WriteLine($"   🟠 Warning:  {warning}");
        Console.WriteLine($"   🔵 Info:     {info}\n");

        if (conflictList.Count == 0)
        {
            Console.WriteLine("   ✅ No conflicts detected!\n");
            return;
        }

        foreach (var conflict in conflictList.OrderByDescending(c => c.Severity))
        {
            var icon = GetIconForSeverity(conflict.Severity);
            Console.WriteLine($"   {icon} {conflict.MigrationName}");
            Console.WriteLine($"      Type: {conflict.ConflictType}");
            Console.WriteLine($"      Details: {conflict.Description}\n");
        }
    }

    static void DisplayRecommendations(IEnumerable<string> recommendations)
    {
        Console.WriteLine("💡 Recommendations:");
        foreach (var rec in recommendations)
        {
            Console.WriteLine($"   • {rec}");
        }
    }

    static IEnumerable<string> GenerateRecommendations(IEnumerable<ConflictInfo> conflicts)
    {
        var conflictList = conflicts.ToList();

        if (!conflictList.Any())
        {
            yield return "No conflicts detected. Ready to merge!";
            yield break;
        }

        if (conflictList.Any(c => c.Severity == ConflictSeverity.Critical))
        {
            yield return "Resolve critical conflicts before merging";
            yield return "Consider renaming migrations to avoid naming conflicts";
        }

        if (conflictList.Any(c => c.ConflictType == "DuplicateNames"))
        {
            yield return "Renaming migrations is recommended to maintain chronological order";
        }

        if (conflictList.Any(c => c.ConflictType == "DependencyIssues"))
        {
            yield return "Review migration dependencies to ensure proper ordering";
        }

        yield return "Run 'ef database update' in test environment before production deployment";
    }

    static string GetIconForSeverity(ConflictSeverity severity) => severity switch
    {
        ConflictSeverity.Critical => "🔴",
        ConflictSeverity.Warning => "🟠",
        ConflictSeverity.Info => "🔵",
        _ => "⚪"
    };

    static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddScoped<GitRepository>();
        services.AddScoped<MigrationRepository>();
        services.AddScoped<ConflictDetectionService>();
        return services;
    }
}

public enum ConflictSeverity
{
    Info,
    Warning,
    Critical
}
