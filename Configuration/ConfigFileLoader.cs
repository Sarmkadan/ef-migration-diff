#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using EfMigrationDiff.Exceptions;

namespace EfMigrationDiff.Configuration;

/// <summary>
/// Provides functionality to load configuration from JSON files.
/// Supports file discovery (walking up directory tree) and strict validation.
/// </summary>
public static class ConfigFileLoader
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Loads configuration from the nearest efmigrationdiff.json file found by walking up the directory tree.
    /// </summary>
    /// <param name="startingDirectory">The directory to start searching from.</param>
    /// <returns>The loaded configuration, or null if no config file was found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="startingDirectory"/> is null.</exception>
    public static EfMigrationDiffOptions? LoadFromNearestConfigFile(string startingDirectory)
    {
        ArgumentNullException.ThrowIfNull(startingDirectory);

        var configFilePath = FindNearestConfigFile(startingDirectory);
        if (configFilePath is null)
        {
            return null;
        }

        return LoadFromFile(configFilePath);
    }

    /// <summary>
    /// Finds the nearest efmigrationdiff.json file by walking up the directory tree.
    /// </summary>
    /// <param name="startingDirectory">The directory to start searching from.</param>
    /// <returns>The full path to the config file, or null if not found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="startingDirectory"/> is null.</exception>
    public static string? FindNearestConfigFile(string startingDirectory)
    {
        ArgumentNullException.ThrowIfNull(startingDirectory);

        var currentDir = Path.GetFullPath(startingDirectory);

        while (!string.IsNullOrEmpty(currentDir))
        {
            // Check for efmigrationdiff.json first
            var configPath = Path.Combine(currentDir, "efmigrationdiff.json");
            if (File.Exists(configPath))
            {
                return configPath;
            }

            // Also check for legacy ef-migration-diff.json for backward compatibility
            var legacyConfigPath = Path.Combine(currentDir, "ef-migration-diff.json");
            if (File.Exists(legacyConfigPath))
            {
                return legacyConfigPath;
            }

            // Move up to parent directory
            var parentDir = Path.GetDirectoryName(currentDir);
            if (parentDir == currentDir || parentDir is null)
            {
                break;
            }

            currentDir = parentDir;
        }

        return null;
    }

    /// <summary>
    /// Loads configuration from a specific JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON config file.</param>
    /// <returns>The loaded configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null.</exception>
    /// <exception cref="ConfigFileException">Thrown when the file cannot be read or contains invalid JSON.</exception>
    public static EfMigrationDiffOptions LoadFromFile(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!File.Exists(filePath))
        {
            throw new ConfigFileException($"Config file not found: {filePath}");
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var options = JsonSerializer.Deserialize<EfMigrationDiffOptions>(json, _jsonOptions);

            if (options is null)
            {
                throw new ConfigFileException($"Failed to deserialize config file: {filePath}");
            }

            // Validate that we loaded something
            if (string.IsNullOrEmpty(options.RepositoryPath))
            {
                throw new ConfigFileException($"Config file {filePath} must specify RepositoryPath");
            }

            return options;
        }
        catch (JsonException ex)
        {
            throw new ConfigFileException($"Invalid JSON in config file {filePath}: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not ConfigFileException)
        {
            throw new ConfigFileException($"Failed to load config file {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validates that a configuration file contains only known properties.
    /// </summary>
    /// <param name="filePath">The path to the JSON config file.</param>
    /// <exception cref="ConfigFileException">Thrown when unknown properties are found.</exception>
    public static void ValidateNoUnknownProperties(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!File.Exists(filePath))
        {
            return; // Skip if file doesn't exist
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Check for unknown properties at the root level
            if (root.TryGetProperty("EfMigrationDiff", out var efMigrationDiff))
            {
                CheckForUnknownProperties(efMigrationDiff, typeof(EfMigrationDiffOptions), filePath);
            }
            else
            {
                // Direct root properties (legacy format)
                CheckForUnknownProperties(root, typeof(EfMigrationDiffOptions), filePath);
            }
        }
        catch (JsonException ex)
        {
            throw new ConfigFileException($"Failed to parse config file for validation {filePath}: {ex.Message}", ex);
        }
    }

    private static void CheckForUnknownProperties(JsonElement element, Type expectedType, string filePath)
    {
        var properties = expectedType.GetProperties();
        var expectedPropertyNames = properties
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var property in element.EnumerateObject())
        {
            if (!expectedPropertyNames.Contains(property.Name))
            {
                throw new ConfigFileException(
                    $"Unknown property '{property.Name}' in config file {filePath}. " +
                    $"Valid properties are: {string.Join(", ", expectedPropertyNames)}");
            }
        }
    }

    /// <summary>
    /// Merges file-based configuration with CLI overrides using proper precedence.
    /// CLI options take precedence over file options, which take precedence over defaults.
    /// </summary>
    /// <param name="baseOptions">The base options (from file or defaults).</param>
    /// <param name="overrideOptions">The override options (from CLI).</param>
    /// <returns>A new options instance with file values overridden by CLI values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either parameter is null.</exception>
    public static EfMigrationDiffOptions MergeWithPrecedence(
        EfMigrationDiffOptions baseOptions,
        EfMigrationDiffOptions overrideOptions)
    {
        ArgumentNullException.ThrowIfNull(baseOptions);
        ArgumentNullException.ThrowIfNull(overrideOptions);

        // Create a new instance with base values (file or defaults)
        var result = new EfMigrationDiffOptions
        {
            // RepositoryPath should generally not be overridden via CLI, use file value
            RepositoryPath = baseOptions.RepositoryPath,

            MigrationsPath = !string.IsNullOrEmpty(overrideOptions.MigrationsPath)
                ? overrideOptions.MigrationsPath
                : baseOptions.MigrationsPath,

            OutputPath = !string.IsNullOrEmpty(overrideOptions.OutputPath)
                ? overrideOptions.OutputPath
                : baseOptions.OutputPath,

            ReportFormat = !string.IsNullOrEmpty(overrideOptions.ReportFormat)
                ? overrideOptions.ReportFormat
                : baseOptions.ReportFormat,

            EnableDetailedLogging = overrideOptions.EnableDetailedLogging,
            MaxConcurrentAnalysis = overrideOptions.MaxConcurrentAnalysis != 0
                ? overrideOptions.MaxConcurrentAnalysis
                : baseOptions.MaxConcurrentAnalysis,

            GenerateHtmlReport = overrideOptions.GenerateHtmlReport,
            GenerateJsonReport = overrideOptions.GenerateJsonReport,

            DbContextNames = overrideOptions.DbContextNames?.Length > 0
                ? overrideOptions.DbContextNames
                : baseOptions.DbContextNames,

            SourceBranch = !string.IsNullOrEmpty(overrideOptions.SourceBranch)
                ? overrideOptions.SourceBranch
                : baseOptions.SourceBranch,

            TargetBranch = !string.IsNullOrEmpty(overrideOptions.TargetBranch)
                ? overrideOptions.TargetBranch
                : baseOptions.TargetBranch,

            SchemaDiff = overrideOptions.SchemaDiff ?? baseOptions.SchemaDiff,

            IgnoredMigrations = overrideOptions.IgnoredMigrations?.Length > 0
                ? overrideOptions.IgnoredMigrations
                : baseOptions.IgnoredMigrations
        };

        return result;
    }

    /// <summary>
        // Loads configuration with proper precedence: config file (if exists) < CLI flags./
    /// </summary>
    /// <param name="cliOptions">The CLI options that should override file-based configuration.</param>
    /// <param name="repositoryPath">The repository path to search for config files from.</param>
    /// <returns>A merged options instance with file values overridden by CLI values.</returns>
    public static EfMigrationDiffOptions LoadWithPrecedence(
        EfMigrationDiffOptions cliOptions,
        string repositoryPath)
    {
        ArgumentNullException.ThrowIfNull(cliOptions);
        ArgumentNullException.ThrowIfNull(repositoryPath);

        // Try to load config file from repository path or parent directories
        var fileOptions = LoadFromNearestConfigFile(repositoryPath);

        // If config file exists, merge it with CLI options (CLI takes precedence)
        if (fileOptions != null)
        {
            return MergeWithPrecedence(fileOptions, cliOptions);
        }

        // No config file found, return CLI options as-is
        return cliOptions;
    }
}