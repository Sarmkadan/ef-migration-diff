#nullable enable

using EfMigrationDiff.Models;
using System.Globalization;

namespace EfMigrationDiff.Services;

/// <summary>
/// Provides validation helpers for <see cref="MigrationParserService"/> to ensure
/// migration files and their contents are properly structured and valid.
/// </summary>
public static class MigrationParserServiceValidation
{
    /// <summary>
    /// Validates the <see cref="MigrationParserService"/> instance and its configuration.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this MigrationParserService value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // MigrationParserService itself has no configurable state to validate
        // All validation is done on the parameters passed to its methods

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the <see cref="MigrationParserService"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to check.</param>
    /// <returns>True if the service is valid; otherwise false.</returns>
    public static bool IsValid(this MigrationParserService value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the <see cref="MigrationParserService"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the service is invalid, containing a list of problems.</exception>
    public static void EnsureValid(this MigrationParserService value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"MigrationParserService is invalid. Problems: {string.Join("; ", errors)}");
        }
    }

    /// <summary>
    /// Validates a migration file before parsing.
    /// </summary>
    /// <param name="value">The service instance.</param>
    /// <param name="migrationFile">The migration file to validate.</param>
    /// <returns>A list of validation errors; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value or migrationFile is null.</exception>
    public static IReadOnlyList<string> Validate(
        this MigrationParserService value,
        MigrationFile migrationFile)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(migrationFile);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(migrationFile.FilePath))
        {
            errors.Add("Migration file path cannot be null or whitespace");
        }
        else if (!File.Exists(migrationFile.FilePath))
        {
            errors.Add($"Migration file does not exist: {migrationFile.FilePath}");
        }

        if (string.IsNullOrWhiteSpace(migrationFile.FileName))
        {
            errors.Add("Migration file name cannot be null or whitespace");
        }
        else if (!migrationFile.FileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) &&
                 !migrationFile.FileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Migration file must have .cs or .Designer.cs extension");
        }

        if (string.IsNullOrWhiteSpace(migrationFile.DbContextName))
        {
            errors.Add("Database context name cannot be null or whitespace");
        }

        if (migrationFile.FileSize <= 0)
        {
            errors.Add("Migration file must have positive size");
        }

        if (migrationFile.LastModified == default)
        {
            errors.Add("Migration file last modified date must be set");
        }

        if (string.IsNullOrWhiteSpace(migrationFile.Content))
        {
            errors.Add("Migration file content cannot be null or whitespace");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates a migration file and returns any errors.
    /// </summary>
    /// <param name="value">The service instance.</param>
    /// <param name="migrationFile">The migration file to validate.</param>
    /// <returns>A list of validation errors; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value or migrationFile is null.</exception>
    public static IReadOnlyList<string> ValidateMigrationFile(
        this MigrationParserService value,
        MigrationFile migrationFile)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(migrationFile);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(migrationFile.Content))
        {
            errors.Add("Migration content is empty");
            return errors.AsReadOnly();
        }

        if (!migrationFile.Content.Contains("public partial class", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Missing 'public partial class' declaration");
        }

        if (!migrationFile.Content.Contains("Down(", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Missing Down method");
        }

        if (!migrationFile.Content.Contains("Up(", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Missing Up method");
        }

        var classMatches = System.Text.RegularExpressions.Regex.Matches(
            migrationFile.Content,
            @"public\s+partial\s+class\s+\w+\s*:",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (classMatches.Count != 1)
        {
            errors.Add("Should contain exactly one public partial class");
        }

        if (string.IsNullOrWhiteSpace(migrationFile.ExtractMigrationId()))
        {
            errors.Add("Migration ID cannot be extracted from filename");
        }
        else if (migrationFile.ExtractMigrationId()?.Length != 14 ||
                 !migrationFile.ExtractMigrationId()!.All(char.IsDigit))
        {
            errors.Add("Migration ID must be a 14-digit timestamp in YYYYMMDDHHmmss format");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates a migration object.
    /// </summary>
    /// <param name="value">The service instance.</param>
    /// <param name="migration">The migration to validate.</param>
    /// <returns>A list of validation errors; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value or migration is null.</exception>
    public static IReadOnlyList<string> Validate(
        this MigrationParserService value,
        Migration migration)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(migration);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(migration.Id))
        {
            errors.Add("Migration ID cannot be null or whitespace");
        }
        else if (migration.Id.Length != 14 || !migration.Id.All(char.IsDigit))
        {
            errors.Add("Migration ID must be a 14-digit timestamp in YYYYMMDDHHmmss format");
        }

        if (string.IsNullOrWhiteSpace(migration.Name))
        {
            errors.Add("Migration name cannot be null or whitespace");
        }

        if (string.IsNullOrWhiteSpace(migration.DbContextName))
        {
            errors.Add("Database context name cannot be null or whitespace");
        }

        if (migration.CreatedAt == default)
        {
            errors.Add("Migration created date must be set");
        }

        if (string.IsNullOrWhiteSpace(migration.Content))
        {
            errors.Add("Migration content cannot be null or whitespace");
        }

        if (migration.Sequence < 0)
        {
            errors.Add("Migration sequence number cannot be negative");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates migration dependencies.
    /// </summary>
    /// <param name="value">The service instance.</param>
    /// <param name="migration">The migration whose dependencies to validate.</param>
    /// <returns>A list of validation errors; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value or migration is null.</exception>
    public static IReadOnlyList<string> ValidateGetMigrationDependencies(
        this MigrationParserService value,
        Migration migration)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(migration);

        var errors = new List<string>();
        errors.AddRange(value.Validate(migration));

        if (string.IsNullOrWhiteSpace(migration.Content))
        {
            errors.Add("Cannot extract dependencies from empty migration content");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates migration comparison parameters.
    /// </summary>
    /// <param name="value">The service instance.</param>
    /// <param name="migration1">First migration to compare.</param>
    /// <param name="migration2">Second migration to compare.</param>
    /// <returns>A list of validation errors; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value, migration1, or migration2 is null.</exception>
    public static IReadOnlyList<string> ValidateCompareMigrations(
        this MigrationParserService value,
        Migration migration1,
        Migration migration2)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(migration1);
        ArgumentNullException.ThrowIfNull(migration2);

        var errors = new List<string>();
        errors.AddRange(value.Validate(migration1));
        errors.AddRange(value.Validate(migration2));

        if (migration1.GetContentSize() <= 0)
        {
            errors.Add("First migration content size must be positive");
        }

        if (migration2.GetContentSize() <= 0)
        {
            errors.Add("Second migration content size must be positive");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates SQL operation extraction parameters.
    /// </summary>
    /// <param name="value">The service instance.</param>
    /// <param name="migration">The migration to extract SQL from.</param>
    /// <returns>A list of validation errors; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value or migration is null.</exception>
    public static IReadOnlyList<string> ValidateExtractSqlOperations(
        this MigrationParserService value,
        Migration migration)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(migration);

        var errors = new List<string>();
        errors.AddRange(value.Validate(migration));

        if (string.IsNullOrWhiteSpace(migration.Content))
        {
            errors.Add("Cannot extract SQL operations from empty migration content");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates migration sequence ID.
    /// </summary>
    /// <param name="value">The service instance.</param>
    /// <param name="migrationId">The migration ID to validate.</param>
    /// <returns>A list of validation errors; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    /// <exception cref="ArgumentException">Thrown if migrationId is null or whitespace.</exception>
    public static IReadOnlyList<string> ValidateGetMigrationSequence(
        this MigrationParserService value,
        string migrationId)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrEmpty(migrationId);

        var errors = new List<string>();

        if (migrationId.Length < 14)
        {
            errors.Add("Migration ID must be at least 14 characters long");
        }
        else if (!migrationId[..14].All(char.IsDigit))
        {
            errors.Add("Migration ID timestamp portion must be numeric");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates directory path for loading migrations.
    /// </summary>
    /// <param name="value">The service instance.</param>
    /// <param name="directoryPath">The directory path to validate.</param>
    /// <param name="dbContextName">The database context name.</param>
    /// <returns>A list of validation errors; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value or dbContextName is null.</exception>
    /// <exception cref="ArgumentException">Thrown if directoryPath is null or whitespace.</exception>
    public static IReadOnlyList<string> ValidateLoadMigrationsFromDirectoryAsync(
        this MigrationParserService value,
        string directoryPath,
        string dbContextName)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        ArgumentException.ThrowIfNullOrEmpty(dbContextName);

        var errors = new List<string>();

        if (!Directory.Exists(directoryPath))
        {
            errors.Add($"Migration directory does not exist: {directoryPath}");
        }
        else
        {
            try
            {
                // Test if we can access the directory
                _ = Directory.GetFiles(directoryPath, "*.cs");
            }
            catch (UnauthorizedAccessException)
            {
                errors.Add($"Insufficient permissions to access directory: {directoryPath}");
            }
            catch (PathTooLongException)
            {
                errors.Add($"Directory path is too long: {directoryPath}");
            }
        }

        if (string.IsNullOrWhiteSpace(dbContextName))
        {
            errors.Add("Database context name cannot be null or whitespace");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates multiple migration files before parsing.
    /// </summary>
    /// <param name="value">The service instance.</param>
    /// <param name="migrationFiles">The migration files to validate.</param>
    /// <returns>A list of validation errors; empty if all files are valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value or migrationFiles is null.</exception>
    public static IReadOnlyList<string> ValidateParseMigrationFiles(
        this MigrationParserService value,
        List<MigrationFile> migrationFiles)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(migrationFiles);

        var errors = new List<string>();

        if (migrationFiles.Count == 0)
        {
            errors.Add("Migration files list cannot be empty");
        }

        for (int i = 0; i < migrationFiles.Count; i++)
        {
            var file = migrationFiles[i];
            var fileErrors = value.Validate(file);
            if (fileErrors.Count > 0)
            {
                errors.AddRange(fileErrors.Select(e => $"File[{i}]: {e}"));
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates a migration file for parsing.
    /// </summary>
    /// <param name="value">The service instance.</param>
    /// <param name="migrationFile">The migration file to validate.</param>
    /// <returns>A list of validation errors; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value or migrationFile is null.</exception>
    public static IReadOnlyList<string> ValidateParseMigrationFile(
        this MigrationParserService value,
        MigrationFile migrationFile)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(migrationFile);

        return value.Validate(migrationFile);
    }
}