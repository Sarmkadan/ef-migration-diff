// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Models;

/// <summary>
/// Represents metadata about a DbContext and its configuration.
/// </summary>
public class DbContextMetadata
{
    public string Id { get; set; } = string.Empty;
    public string ContextName { get; set; } = string.Empty;
    public string AssemblyName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string DatabaseProvider { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public List<string> MigrationIds { get; set; } = [];
    public List<string> EntityTypes { get; set; } = [];
    public Dictionary<string, string> Properties { get; set; } = [];
    public DateTime LastScannedAt { get; set; }

    public DbContextMetadata()
    {
    }

    public DbContextMetadata(string contextName, string assemblyName)
    {
        Id = Guid.NewGuid().ToString();
        ContextName = contextName;
        AssemblyName = assemblyName;
        DatabaseProvider = "SqlServer";
    }

    /// <summary>
    /// Validates the context metadata has required properties.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ContextName) &&
               !string.IsNullOrWhiteSpace(AssemblyName) &&
               !string.IsNullOrWhiteSpace(DatabaseProvider);
    }

    /// <summary>
    /// Adds a migration ID to this context's migration history.
    /// </summary>
    public void AddMigration(string migrationId)
    {
        if (!string.IsNullOrWhiteSpace(migrationId) && !MigrationIds.Contains(migrationId))
        {
            MigrationIds.Add(migrationId);
        }
    }

    /// <summary>
    /// Adds an entity type that is managed by this context.
    /// </summary>
    public void AddEntityType(string entityTypeName)
    {
        if (!string.IsNullOrWhiteSpace(entityTypeName) && !EntityTypes.Contains(entityTypeName))
        {
            EntityTypes.Add(entityTypeName);
        }
    }

    /// <summary>
    /// Adds a property that describes this context.
    /// </summary>
    public void AddProperty(string key, string value)
    {
        Properties[key] = value;
    }

    /// <summary>
    /// Gets a property value by key, returns empty string if not found.
    /// </summary>
    public string GetProperty(string key)
    {
        return Properties.TryGetValue(key, out var value) ? value : string.Empty;
    }

    /// <summary>
    /// Gets the number of migrations associated with this context.
    /// </summary>
    public int GetMigrationCount()
    {
        return MigrationIds.Count;
    }

    /// <summary>
    /// Gets the number of entity types managed by this context.
    /// </summary>
    public int GetEntityTypeCount()
    {
        return EntityTypes.Count;
    }

    /// <summary>
    /// Checks if this context has a migration with the given ID.
    /// </summary>
    public bool HasMigration(string migrationId)
    {
        return MigrationIds.Contains(migrationId);
    }

    /// <summary>
    /// Gets the last applied migration ID, or null if none exist.
    /// </summary>
    public string? GetLastMigration()
    {
        return MigrationIds.Count > 0 ? MigrationIds[^1] : null;
    }

    /// <summary>
    /// Marks this context as recently scanned.
    /// </summary>
    public void MarkAsScanned()
    {
        LastScannedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a display-friendly string of the database provider.
    /// </summary>
    public string GetProviderDisplayName()
    {
        return DatabaseProvider.ToLowerInvariant() switch
        {
            "sqlserver" => "SQL Server",
            "postgresql" => "PostgreSQL",
            "mysql" => "MySQL",
            "sqlite" => "SQLite",
            "cosmosdb" => "Azure Cosmos DB",
            "inmemory" => "In-Memory",
            _ => DatabaseProvider
        };
    }

    public override string ToString()
    {
        return $"{ContextName} ({AssemblyName}) - {GetProviderDisplayName()}";
    }
}
