#nullable enable

namespace EfMigrationDiff.Models;

/// <summary>
/// Extension methods for <see cref="MigrationFile"/> that provide additional functionality
/// for working with Entity Framework migration files.
/// </summary>
public static class MigrationFileExtensions
{
    /// <summary>
    /// Determines if this migration file is a migration class (not a designer file).
    /// </summary>
    /// <param name="migrationFile">The migration file to check.</param>
    /// <returns>True if this is a migration class file; otherwise false.</returns>
    public static bool IsMigrationClass(this MigrationFile migrationFile)
    {
        if (migrationFile == null)
        {
            throw new ArgumentNullException(nameof(migrationFile));
        }

        return !migrationFile.IsDesigner && migrationFile.FileName.EndsWith(".cs");
    }

    /// <summary>
    /// Gets the migration name from the filename (e.g., "20240101000000_AddUserTable").
    /// </summary>
    /// <param name="migrationFile">The migration file.</param>
    /// <returns>The migration name without the timestamp prefix.</returns>
    public static string GetMigrationName(this MigrationFile migrationFile)
    {
        if (migrationFile == null)
        {
            throw new ArgumentNullException(nameof(migrationFile));
        }

        var migrationId = migrationFile.ExtractMigrationId();
        if (string.IsNullOrEmpty(migrationId))
        {
            return string.Empty;
        }

        // Migration ID format: "20240101000000_AddUserTable"
        // Extract everything after the timestamp (14 digits)
        if (migrationId.Length > 14)
        {
            return migrationId[14..];
        }

        return migrationId;
    }

    /// <summary>
    /// Gets the timestamp portion of the migration ID (e.g., "20240101000000").
    /// </summary>
    /// <param name="migrationFile">The migration file.</param>
    /// <returns>The timestamp string or empty if not available.</returns>
    public static string GetMigrationTimestamp(this MigrationFile migrationFile)
    {
        if (migrationFile == null)
        {
            throw new ArgumentNullException(nameof(migrationFile));
        }

        var migrationId = migrationFile.ExtractMigrationId();
        if (string.IsNullOrEmpty(migrationId) || migrationId.Length < 14)
        {
            return string.Empty;
        }

        return migrationId[..14];
    }

    /// <summary>
    /// Gets a formatted display string for the migration file including context.
    /// </summary>
    /// <param name="migrationFile">The migration file.</param>
    /// <param name="includeContext">Whether to include DbContext name in the output.</param>
    /// <returns>Formatted display string.</returns>
    public static string GetFormattedDisplay(this MigrationFile migrationFile, bool includeContext = true)
    {
        if (migrationFile == null)
        {
            throw new ArgumentNullException(nameof(migrationFile));
        }

        var migrationName = migrationFile.GetMigrationName();
        var timestamp = migrationFile.GetMigrationTimestamp();

        var displayParts = new List<string>();

        if (!string.IsNullOrEmpty(timestamp))
        {
            displayParts.Add($"[{timestamp}]");
        }

        displayParts.Add(migrationName);

        if (includeContext && !string.IsNullOrEmpty(migrationFile.DbContextName))
        {
            displayParts.Add($"({migrationFile.DbContextName})");
        }

        return string.Join(" ", displayParts);
    }
}