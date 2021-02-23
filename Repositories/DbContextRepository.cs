#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Models;

namespace EfMigrationDiff.Repositories;

/// <summary>
/// Repository for managing DbContext metadata and configuration.
/// </summary>
public class DbContextRepository
{
    private readonly List<DbContextMetadata> _contexts = [];
    private readonly object _syncLock = new object();

    /// <summary>
    /// Adds a new DbContext metadata to the repository.
    /// </summary>
    public void Add(DbContextMetadata context)
    {
        if (!context.IsValid())
            throw new ArgumentException("DbContext metadata must be valid before adding to repository");

        lock (_syncLock)
        {
            if (_contexts.Any(c => c.Id == context.Id))
                throw new InvalidOperationException($"DbContext with ID {context.Id} already exists");

            _contexts.Add(context);
        }
    }

    /// <summary>
    /// Retrieves a DbContext by its ID.
    /// </summary>
    public DbContextMetadata? GetById(string id)
    {
        lock (_syncLock)
        {
            return _contexts.FirstOrDefault(c => c.Id == id);
        }
    }

    /// <summary>
    /// Retrieves a DbContext by its name.
    /// </summary>
    public DbContextMetadata? GetByName(string contextName)
    {
        lock (_syncLock)
        {
            return _contexts.FirstOrDefault(c => c.ContextName == contextName);
        }
    }

    /// <summary>
    /// Retrieves all DbContexts for a specific assembly.
    /// </summary>
    public List<DbContextMetadata> GetByAssembly(string assemblyName)
    {
        lock (_syncLock)
        {
            return _contexts.Where(c => c.AssemblyName == assemblyName).ToList();
        }
    }

    /// <summary>
    /// Retrieves all DbContexts with a specific database provider.
    /// </summary>
    public List<DbContextMetadata> GetByProvider(string provider)
    {
        lock (_syncLock)
        {
            return _contexts.Where(c => c.DatabaseProvider == provider).ToList();
        }
    }

    /// <summary>
    /// Updates an existing DbContext metadata.
    /// </summary>
    public void Update(DbContextMetadata context)
    {
        lock (_syncLock)
        {
            var existing = _contexts.FirstOrDefault(c => c.Id == context.Id);
            if (existing is null)
                throw new KeyNotFoundException($"DbContext with ID {context.Id} not found");

            _contexts.Remove(existing);
            _contexts.Add(context);
        }
    }

    /// <summary>
    /// Deletes a DbContext by its ID.
    /// </summary>
    public bool Delete(string id)
    {
        lock (_syncLock)
        {
            var context = _contexts.FirstOrDefault(c => c.Id == id);
            if (context is null)
                return false;

            return _contexts.Remove(context);
        }
    }

    /// <summary>
    /// Retrieves all DbContexts.
    /// </summary>
    public List<DbContextMetadata> GetAll()
    {
        lock (_syncLock)
        {
            return new List<DbContextMetadata>(_contexts);
        }
    }

    /// <summary>
    /// Gets the total count of DbContexts.
    /// </summary>
    public int Count()
    {
        lock (_syncLock)
        {
            return _contexts.Count;
        }
    }

    /// <summary>
    /// Searches DbContexts by name.
    /// </summary>
    public List<DbContextMetadata> SearchByName(string searchTerm)
    {
        lock (_syncLock)
        {
            return _contexts.Where(c => c.ContextName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    /// <summary>
    /// Gets DbContexts that have been recently scanned.
    /// </summary>
    public List<DbContextMetadata> GetRecentlyScanned(TimeSpan withinTimeSpan)
    {
        lock (_syncLock)
        {
            var threshold = DateTime.UtcNow - withinTimeSpan;
            return _contexts.Where(c => c.LastScannedAt > threshold).ToList();
        }
    }

    /// <summary>
    /// Gets DbContexts with migration history.
    /// </summary>
    public List<DbContextMetadata> GetWithMigrations()
    {
        lock (_syncLock)
        {
            return _contexts.Where(c => c.MigrationIds.Count > 0).ToList();
        }
    }

    /// <summary>
    /// Gets DbContexts that manage specific entity type.
    /// </summary>
    public List<DbContextMetadata> GetByEntityType(string entityTypeName)
    {
        lock (_syncLock)
        {
            return _contexts.Where(c => c.EntityTypes.Contains(entityTypeName)).ToList();
        }
    }

    /// <summary>
    /// Clears all DbContext metadata from the repository.
    /// </summary>
    public void Clear()
    {
        lock (_syncLock)
        {
            _contexts.Clear();
        }
    }

    /// <summary>
    /// Checks if a DbContext exists by ID.
    /// </summary>
    public bool Exists(string id)
    {
        lock (_syncLock)
        {
            return _contexts.Any(c => c.Id == id);
        }
    }

    /// <summary>
    /// Gets DbContexts by database provider and assembly.
    /// </summary>
    public List<DbContextMetadata> GetByProviderAndAssembly(string provider, string assemblyName)
    {
        lock (_syncLock)
        {
            return _contexts.Where(c => c.DatabaseProvider == provider && c.AssemblyName == assemblyName).ToList();
        }
    }
}
