#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Configuration;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;

namespace EfMigrationDiff.CLI.Commands;

/// <summary>
/// Implements the validate command to check migration files for structural correctness.
/// Validates syntax, naming conventions, and compatibility with EF migration standards.
/// </summary>
public class ValidateCommand : ICommand
{
    public string GetDescription() => "Validate migration files for structural correctness";

    /// <summary>
    /// Executes validation of migration files in the configured migrations directory.
    /// Reports errors and warnings for each file and provides a summary count.
    /// </summary>
    public async Task<CommandResult> ExecuteAsync(CommandContext context)
    {
        var appSettings = context.ServiceProvider.GetService<AppSettings>()
            ?? throw new InvalidOperationException("AppSettings not found in service provider");

        var parserService = context.ServiceProvider.GetService<MigrationParserService>()
            ?? throw new InvalidOperationException("MigrationParserService not found");

        appSettings.RepositoryPath = Environment.CurrentDirectory;
        var migrationsPath = appSettings.GetMigrationsDirectory();

        if (!Directory.Exists(migrationsPath))
        {
            return CommandResult.Error($"Migrations directory not found: {migrationsPath}");
        }

        context.WriteColoredOutput($"Validating migrations in: {migrationsPath}", ConsoleColor.Cyan);

        // Gather migration files
        var migrationFiles = Directory.GetFiles(migrationsPath, "*.cs")
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (migrationFiles.Count == 0)
        {
            context.WriteOutput("No migration files found.");
            return CommandResult.Ok("No migrations to validate");
        }

        context.WriteOutput($"Found {migrationFiles.Count} migration file(s)\n");

        int validCount = 0;
        int invalidCount = 0;
        var allErrors = new List<string>();

        // Validate each migration file
        foreach (var filePath in migrationFiles)
        {
            var migrationFile = new MigrationFile(filePath, "DefaultContext");
            var errors = parserService.ValidateMigrationFile(migrationFile);

            if (errors.Count == 0)
            {
                context.WriteColoredOutput($"  ✓ {Path.GetFileName(filePath)}", ConsoleColor.Green);
                validCount++;
            }
            else
            {
                context.WriteColoredOutput($"  ✗ {Path.GetFileName(filePath)}", ConsoleColor.Red);
                foreach (var error in errors)
                {
                    context.WriteOutput($"    - {error}");
                    allErrors.Add($"{Path.GetFileName(filePath)}: {error}");
                }
                invalidCount++;
            }
        }

        // Display summary
        context.WriteOutput($"\n{'─',40}");
        context.WriteColoredOutput($"Valid: {validCount} | Invalid: {invalidCount}", validCount > 0 ? ConsoleColor.Green : ConsoleColor.Red);

        if (invalidCount > 0)
        {
            return CommandResult.Error(
                $"Validation failed: {invalidCount} invalid file(s)",
                1);
        }

        return CommandResult.Ok($"All {validCount} migration(s) validated successfully");
    }
}
