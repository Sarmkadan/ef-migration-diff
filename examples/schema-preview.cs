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
/// Schema preview example: Visualize schema changes between branches
/// Shows before/after state of database schema
/// </summary>
class SchemaPreviewExample
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddScoped<SchemaChangeDetectorService>();
        services.AddScoped<MigrationRepository>();
        services.AddScoped<MigrationParserService>();

        var provider = services.BuildServiceProvider();
        var schemaDetector = provider.GetRequiredService<SchemaChangeDetectorService>();
        var migrationRepo = provider.GetRequiredService<MigrationRepository>();

        try
        {
            Console.WriteLine("📊 Schema Change Preview\n");

            // Load migrations
            var newMigrations = await migrationRepo.GetMigrationsAsync("feature/schema-update").ConfigureAwait(false);
            var baseMigrations = await migrationRepo.GetMigrationsAsync("main").ConfigureAwait(false);

            // Detect schema changes
            var changes = await schemaDetector.DetectChangesAsync(baseMigrations, newMigrations).ConfigureAwait(false);

            DisplaySchemaChanges(changes);
            DisplayChangesSummary(changes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void DisplaySchemaChanges(IEnumerable<SchemaChange> changes)
    {
        var changesList = changes.ToList();

        if (!changesList.Any())
        {
            Console.WriteLine("✅ No schema changes detected.\n");
            return;
        }

        // Group by table
        var groupedByTable = changesList.GroupBy(c => c.TableName);

        foreach (var tableGroup in groupedByTable.OrderBy(g => g.Key))
        {
            Console.WriteLine($"📋 Table: {tableGroup.Key}");
            Console.WriteLine($"   {'Operation',-15} {'Column',-30} {'Type',-20}");
            Console.WriteLine("   " + new string('-', 65));

            foreach (var change in tableGroup)
            {
                var operation = GetOperationIcon(change.OperationType);
                var columnName = change.ColumnName ?? "(table)";
                var columnType = change.ColumnType ?? "";

                Console.WriteLine($"   {operation,-15} {columnName,-30} {columnType,-20}");
            }
            Console.WriteLine();
        }
    }

    static void DisplayChangesSummary(IEnumerable<SchemaChange> changes)
    {
        var changesList = changes.ToList();

        Console.WriteLine("📈 Summary");
        Console.WriteLine($"   Total Changes: {changesList.Count}");
        Console.WriteLine($"   Tables Affected: {changesList.Select(c => c.TableName).Distinct().Count()}");
        Console.WriteLine($"   Columns Added: {changesList.Count(c => c.OperationType == "AddColumn")}");
        Console.WriteLine($"   Columns Dropped: {changesList.Count(c => c.OperationType == "DropColumn")}");
        Console.WriteLine($"   Tables Created: {changesList.Count(c => c.OperationType == "CreateTable")}");
        Console.WriteLine($"   Tables Dropped: {changesList.Count(c => c.OperationType == "DropTable")}");
        Console.WriteLine();

        // Check for potential data loss
        var dataLossOps = changesList.Where(c =>
            c.OperationType == "DropColumn" ||
            c.OperationType == "DropTable" ||
            (c.OperationType == "AlterColumn" && c.IsNullable == false)).ToList();

        if (dataLossOps.Any())
        {
            Console.WriteLine("⚠️  Potential Data Loss Operations:");
            foreach (var op in dataLossOps)
            {
                Console.WriteLine($"   • {op.OperationType} on {op.TableName}.{op.ColumnName}");
            }
        }
    }

    static string GetOperationIcon(string operation) => operation switch
    {
        "CreateTable" => "✨ CREATE",
        "DropTable" => "🗑️  DROP",
        "AddColumn" => "➕ ADD",
        "DropColumn" => "➖ REMOVE",
        "AlterColumn" => "🔧 ALTER",
        "AddIndex" => "📑 INDEX",
        "DropIndex" => "❌ IDX-DEL",
        "AddConstraint" => "🔒 CONSTRAINT",
        "DropConstraint" => "🔓 CONSTR-DEL",
        _ => "❓ UNKNOWN"
    };
}
