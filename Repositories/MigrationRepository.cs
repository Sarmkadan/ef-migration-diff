// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Models;

namespace EfMigrationDiff.Repositories;

/// <summary>
/// Repository for managing migration data access and CRUD operations.
/// </summary>
public class MigrationRepository
{
    private readonly List<Migration> _migrations = [];
    private readonly object _syncLock = new object();

    /// <summary>
    /// Adds a new migration to the repository.
    /// </summary>
    public void Add(Migration migration)
    {
        if (!migration.IsValid())
            throw new ArgumentException("Migration must be valid before adding to repository");

        lock (_syncLock)
        {
            if (_migrations.Any(m => m.Id == migration.Id))
                throw new InvalidOperationException($"Migration with ID {migration.Id} already exists");

            _migrations.Add(migration);
        }
    }

    /// <summary>
    /// Retrieves a migration by its ID.
    /// </summary>
    public Migration? GetById(string id)
    {
        lock (_syncLock)
        {
            return _migrations.FirstOrDefault(m => m.Id == id);
        }
    }

    /// <summary>
    /// Retrieves all migrations for a specific DbContext.
    /// </summary>
    public List<Migration> GetByDbContext(string dbContextName)
    {
        lock (_syncLock)
        {
            return _migrations.Where(m => m.DbContextName == dbContextName).ToList();
        }
    }

    /// <summary>
    /// Retrieves all migrations with a specific status.
    /// </summary>
    public List<Migration> GetByStatus(MigrationStatus status)
    {
        lock (_syncLock)
        {
            return _migrations.Where(m => m.Status == status).ToList();
        }
    }

    /// <summary>
    /// Updates an existing migration.
    /// </summary>
    public void Update(Migration migration)
    {
        lock (_syncLock)
        {
            var existing = _migrations.FirstOrDefault(m => m.Id == migration.Id);
            if (existing == null)
                throw new KeyNotFoundException($"Migration with ID {migration.Id} not found");

            _migrations.Remove(existing);
            _migrations.Add(migration);
        }
    }

    /// <summary>
    /// Deletes a migration by its ID.
    /// </summary>
    public bool Delete(string id)
    {
        lock (_syncLock)
        {
            var migration = _migrations.FirstOrDefault(m => m.Id == id);
            if (migration == null)
                return false;

            return _migrations.Remove(migration);
        }
    }

    /// <summary>
    /// Retrieves all migrations sorted by creation date.
    /// </summary>
    public List<Migration> GetAll()
    {
        lock (_syncLock)
        {
            return _migrations.OrderBy(m => m.CreatedAt).ToList();
        }
    }

    /// <summary>
    /// Retrieves paginated migrations.
    /// </summary>
    public List<Migration> GetPaginated(int skip, int take)
    {
        lock (_syncLock)
        {
            return _migrations.OrderBy(m => m.CreatedAt).Skip(skip).Take(take).ToList();
        }
    }

    /// <summary>
    /// Searches migrations by name.
    /// </summary>
    public List<Migration> SearchByName(string searchTerm)
    {
        lock (_syncLock)
        {
            return _migrations.Where(m => m.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    /// <summary>
    /// Gets the total count of migrations.
    /// </summary>
    public int Count()
    {
        lock (_syncLock)
        {
            return _migrations.Count;
        }
    }

    /// <summary>
    /// Gets migrations for multiple DbContexts.
    /// </summary>
    public List<Migration> GetByDbContexts(params string[] contextNames)
    {
        lock (_syncLock)
        {
            return _migrations.Where(m => contextNames.Contains(m.DbContextName)).ToList();
        }
    }

    /// <summary>
    /// Gets migrations within a date range.
    /// </summary>
    public List<Migration> GetByDateRange(DateTime startDate, DateTime endDate)
    {
        lock (_syncLock)
        {
            return _migrations.Where(m => m.CreatedAt >= startDate && m.CreatedAt <= endDate).ToList();
        }
    }

    /// <summary>
    /// Clears all migrations from the repository.
    /// </summary>
    public void Clear()
    {
        lock (_syncLock)
        {
            _migrations.Clear();
        }
    }

    /// <summary>
    /// Checks if a migration exists by ID.
    /// </summary>
    public bool Exists(string id)
    {
        lock (_syncLock)
        {
            return _migrations.Any(m => m.Id == id);
        }
    }

    /// <summary>
    /// Gets the latest migration for a DbContext.
    /// </summary>
    public Migration? GetLatestByDbContext(string dbContextName)
    {
        lock (_syncLock)
        {
            return _migrations.Where(m => m.DbContextName == dbContextName)
                              .OrderByDescending(m => m.CreatedAt)
                              .FirstOrDefault();
        }
    }
}
