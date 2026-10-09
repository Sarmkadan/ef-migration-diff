#nullable enable
using EfMigrationDiff.Configuration;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EfMigrationDiff.CLI.Commands;

/// <summary>
/// Implements the validate command to check migration files for structural correctness.
/// Validates syntax, naming conventions, and compatibility with EF migration standards.
/// </summary>
public class ValidateCommand : ICommand
{
    private const string Description = "Validate migration files for structural correctness";
    private const string DefaultContextName = "DefaultContext";
    private const string MigrationFilePattern = "*.cs";
    private const string DesignerFileSuffix = ".Designer.cs";
    private const string AppSettingsNotFoundMessage = "AppSettings not found in service provider";
    private const string MigrationParserNotFoundMessage = "MigrationParserService not found";
    private const string NoMigrationFilesMessage = "No migration files found.";
    private const string NoMigrationsToValidateMessage = "No migrations to validate";
    private const string AllValidatedMessageFormat = "All {0} migration(s) validated successfully";

    public string GetDescription() => Description;

    /// <summary>
    /// Executes validation of migration files in the configured migrations directory.
    /// Reports errors and warnings for each file and provides a summary count.
    /// </summary>
    public async Task<CommandResult> ExecuteAsync(CommandContext context)
    {
        var appSettings = context.ServiceProvider.GetService<AppSettings>()
            ?? throw new InvalidOperationException(AppSettingsNotFoundMessage);

        var parserService = context.ServiceProvider.GetService<MigrationParserService>()
            ?? throw new InvalidOperationException(MigrationParserNotFoundMessage);

        appSettings.RepositoryPath = Environment.CurrentDirectory;
        var migrationsPath = appSettings.GetMigrationsDirectory();

        if (!Directory.Exists(migrationsPath))
        {
            return CommandResult.Error($"Migrations directory not found: {migrationsPath}");
        }

        context.WriteColoredOutput($"Validating migrations in: {migrationsPath}", ConsoleColor.Cyan);

        // Gather migration files
        var migrationFiles = Directory.GetFiles(migrationsPath, MigrationFilePattern)
            .Where(f => !f.EndsWith(DesignerFileSuffix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (migrationFiles.Count == 0)
        {
            context.WriteOutput(NoMigrationFilesMessage);
            return CommandResult.Ok(NoMigrationsToValidateMessage);
        }

        context.WriteOutput($"Found {migrationFiles.Count} migration file(s)\n");

        int validCount = 0;
        int invalidCount = 0;
        var allErrors = new List<string>();

        // Validate each migration file
        foreach (var filePath in migrationFiles)
        {
            var migrationFile = new MigrationFile(filePath, DefaultContextName);
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

        return CommandResult.Ok(string.Format(AllValidatedMessageFormat, validCount));
    }
}
