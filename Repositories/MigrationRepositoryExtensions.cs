#nullable enable

using EfMigrationDiff.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EfMigrationDiff.Repositories;

/// <summary>
/// Extension methods for MigrationRepository providing additional convenience and batch operations.
/// </summary>
public static class MigrationRepositoryExtensions
{
    /// <summary>
    /// Adds multiple migrations to the repository in a single operation.
    /// </summary>
    /// <param name="repository">The repository instance</param>
    /// <param name="migrations">Collection of migrations to add</param>
    /// <returns>Count of successfully added migrations</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="migrations"/> is <see langword="null"/></exception>
    public static int AddRange(this MigrationRepository repository, IEnumerable<Migration> migrations)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(migrations);

        return migrations.Count(migration => { repository.Add(migration); return true; });
    }

    /// <summary>
    /// Retrieves all migrations filtered by multiple statuses.
    /// </summary>
    /// <param name="repository">The repository instance</param>
    /// <param name="statuses">Statuses to filter by</param>
    /// <returns>Filtered list of migrations</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/></exception>
    public static List<Migration> GetByStatuses(this MigrationRepository repository, params MigrationStatus[] statuses)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (statuses is null || statuses.Length == 0)
            return [];

        lock (repository.GetSyncLock())
        {
            return repository.GetAll()
                .Where(m => statuses.Contains(m.Status))
                .ToList();
        }
    }

    /// <summary>
    /// Retrieves migrations by name pattern using SQL-like pattern matching.
    /// Supports % for any sequence and _ for single character wildcards.
    /// </summary>
    /// <param name="repository">The repository instance</param>
    /// <param name="namePattern">Pattern to match (e.g., "%Init%", "Create_%")</param>
    /// <returns>Matching migrations</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="namePattern"/> is empty or whitespace</exception>
    public static List<Migration> SearchByNamePattern(this MigrationRepository repository, string namePattern)
    {
        ArgumentNullException.ThrowIfNull(repository);

        if (string.IsNullOrWhiteSpace(namePattern))
            return [];

        lock (repository.GetSyncLock())
        {
            var allMigrations = repository.GetAll();

            // Convert SQL-like pattern to regex
            string pattern = namePattern
                .Replace("%", ".*")
                .Replace("_", ".");

            var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return allMigrations
                .Where(m => regex.IsMatch(m.Name))
                .ToList();
        }
    }

    /// <summary>
    /// Gets the total count of migrations filtered by status.
    /// </summary>
    /// <param name="repository">The repository instance</param>
    /// <param name="status">Status to filter by</param>
    /// <returns>Count of migrations with the specified status</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/></exception>
    public static int CountByStatus(this MigrationRepository repository, MigrationStatus status)
    {
        ArgumentNullException.ThrowIfNull(repository);

        lock (repository.GetSyncLock())
        {
            return repository.GetByStatus(status).Count;
        }
    }

    /// <summary>
    /// Gets the sync lock object for thread-safe operations when needed outside the repository.
    /// </summary>
    /// <param name="repository">The repository instance</param>
    /// <returns>The synchronization lock object</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">Sync lock field not found in MigrationRepository</exception>
    private static object GetSyncLock(this MigrationRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        // Using reflection to access the private _syncLock field
        var field = typeof(MigrationRepository).GetField("_syncLock",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field is null)
            throw new InvalidOperationException("Sync lock field not found in MigrationRepository");

        return field.GetValue(repository)!;
    }
}