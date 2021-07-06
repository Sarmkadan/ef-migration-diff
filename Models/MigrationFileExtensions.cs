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
    /// <exception cref="ArgumentNullException"><paramref name="migrationFile"/> is <see langword="null"/>.</exception>
    /// <returns>True if this is a migration class file; otherwise false.</returns>
    public static bool IsMigrationClass(this MigrationFile migrationFile)
    {
        ArgumentNullException.ThrowIfNull(migrationFile);

        return !migrationFile.IsDesigner && migrationFile.FileName.EndsWith(".cs");
    }

    /// <summary>
    /// Gets the migration name from the filename (e.g., "20240101000000_AddUserTable").
    /// </summary>
    /// <param name="migrationFile">The migration file.</param>
    /// <exception cref="ArgumentNullException"><paramref name="migrationFile"/> is <see langword="null"/>.</exception>
    /// <returns>The migration name without the timestamp prefix.</returns>
    public static string GetMigrationName(this MigrationFile migrationFile)
    {
        ArgumentNullException.ThrowIfNull(migrationFile);

        var migrationId = migrationFile.ExtractMigrationId();
        return string.IsNullOrEmpty(migrationId)
            ? string.Empty
            : migrationId.Length > 14
                ? migrationId[14..]
                : migrationId;
    }

    /// <summary>
    /// Gets the timestamp portion of the migration ID (e.g., "20240101000000").
    /// </summary>
    /// <param name="migrationFile">The migration file.</param>
    /// <exception cref="ArgumentNullException"><paramref name="migrationFile"/> is <see langword="null"/>.</exception>
    /// <returns>The timestamp string or empty if not available.</returns>
    public static string GetMigrationTimestamp(this MigrationFile migrationFile)
    {
        ArgumentNullException.ThrowIfNull(migrationFile);

        var migrationId = migrationFile.ExtractMigrationId();
        return string.IsNullOrEmpty(migrationId) || migrationId.Length < 14
            ? string.Empty
            : migrationId[..14];
    }

    /// <summary>
    /// Gets a formatted display string for the migration file including context.
    /// </summary>
    /// <param name="migrationFile">The migration file.</param>
    /// <param name="includeContext">Whether to include DbContext name in the output.</param>
    /// <exception cref="ArgumentNullException"><paramref name="migrationFile"/> is <see langword="null"/>.</exception>
    /// <returns>Formatted display string.</returns>
    public static string GetFormattedDisplay(this MigrationFile migrationFile, bool includeContext = true)
    {
        ArgumentNullException.ThrowIfNull(migrationFile);

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