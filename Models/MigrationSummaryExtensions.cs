#nullable enable

namespace EfMigrationDiff.Models;

/// <summary>
/// Provides one-line summary helpers for <see cref="Migration"/>.
/// </summary>
public static class MigrationSummaryExtensions
{
    /// <summary>
    /// Builds a single-line summary of a migration, e.g.
    /// <c>20260101120000 AddUsers [Pending] (3 schema change(s))</c>.
    /// </summary>
    /// <param name="migration">The migration to summarize.</param>
    /// <returns>A one-line, human-readable summary of the migration.</returns>
    public static string ToSummaryLine(this Migration migration)
    {
        ArgumentNullException.ThrowIfNull(migration);

        var changeCount = migration.SchemaChanges.Count;

        return $"{migration.Timestamp} {migration.Name} [{migration.Status}] ({changeCount} schema change(s))";
    }
}
