#nullable enable

using System.Text.RegularExpressions;
using EfMigrationDiff.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EfMigrationDiff.Configuration;

namespace EfMigrationDiff.Services;

/// <summary>
/// Reads and parses EF Core model snapshot files to extract the schema state
/// at a given point in time.
/// </summary>
public partial class SnapshotReader : IDisposable
{
    private readonly ILogger<SnapshotReader> _logger;
    private readonly EfMigrationDiffOptions _options;
    private bool _disposed;

    public SnapshotReader(
        ILogger<SnapshotReader> logger,
        IOptions<EfMigrationDiffOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Reads a model snapshot file and extracts the DbContext metadata it contains.
    /// </summary>
    /// <param name="snapshotPath">Absolute or relative path to the snapshot .cs file.</param>
    /// <returns>Parsed DbContext metadata from the snapshot.</returns>
    public async Task<DbContextMetadata> ReadSnapshotAsync(string snapshotPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(snapshotPath);

        var fullPath = Path.IsPathRooted(snapshotPath)
            ? snapshotPath
            : Path.Combine(_options.RepositoryPath, _options.MigrationsPath, snapshotPath);

        _logger.LogInformation("Reading snapshot from {Path}", fullPath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Snapshot file not found: {fullPath}", fullPath);

        var content = await File.ReadAllTextAsync(fullPath);
        return ParseSnapshot(content);
    }

    /// <summary>
    /// Parses snapshot file content and extracts entity/table definitions.
    /// </summary>
    /// <param name="content">The raw C# source content of the snapshot file.</param>
    /// <returns>Parsed DbContext metadata.</returns>
    public DbContextMetadata ParseSnapshot(string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);

        var metadata = new DbContextMetadata();

        var contextMatch = DbContextNameRegex().Match(content);
        if (contextMatch.Success)
            metadata.ContextName = contextMatch.Groups[1].Value;

        var entityMatches = EntityDefinitionRegex().Matches(content);
        foreach (Match match in entityMatches)
        {
            metadata.EntityTypes.Add(match.Groups[1].Value);
        }

        var tableNames = new List<string>();
        var tableMatches = TableNameRegex().Matches(content);
        foreach (Match match in tableMatches)
        {
            tableNames.Add(match.Groups[1].Value);
        }

        // Store table names in Properties for downstream consumers
        for (var i = 0; i < tableNames.Count; i++)
            metadata.Properties[$"Table:{i}"] = tableNames[i];

        _logger.LogInformation("Parsed snapshot: context={Context}, {EntityCount} entities, {TableCount} tables",
            metadata.ContextName, metadata.EntityTypes.Count, tableNames.Count);

        return metadata;
    }

    /// <summary>
    /// Finds all snapshot files within the configured migrations directory.
    /// </summary>
    /// <returns>A list of absolute paths to snapshot files.</returns>
    public IReadOnlyList<string> DiscoverSnapshots()
    {
        var searchPath = Path.Combine(_options.RepositoryPath, _options.MigrationsPath);

        if (!Directory.Exists(searchPath))
        {
            _logger.LogWarning("Migrations directory not found: {Path}", searchPath);
            return [];
        }

        var files = Directory.GetFiles(searchPath, "*ModelSnapshot.cs", SearchOption.AllDirectories);
        _logger.LogInformation("Discovered {Count} snapshot files in {Path}", files.Length, searchPath);
        return files;
    }

    /// <summary>
    /// Compares two snapshots and returns the set of entity types that differ.
    /// </summary>
    /// <param name="older">The baseline snapshot metadata.</param>
    /// <param name="newer">The newer snapshot metadata.</param>
    /// <returns>Entity type names that were added or removed.</returns>
    public IReadOnlyList<string> CompareSnapshots(DbContextMetadata older, DbContextMetadata newer)
    {
        ArgumentNullException.ThrowIfNull(older);
        ArgumentNullException.ThrowIfNull(newer);

        var added = newer.EntityTypes.Except(older.EntityTypes);
        var removed = older.EntityTypes.Except(newer.EntityTypes);

        var changed = added.Concat(removed).Distinct().ToList();
        _logger.LogInformation("Snapshot comparison: {Count} entity types differ", changed.Count);
        return changed;
    }

    [GeneratedRegex(@"class\s+(\w+)ModelSnapshot\s*:", RegexOptions.Compiled)]
    private static partial Regex DbContextNameRegex();

    [GeneratedRegex(@"modelBuilder\.Entity<(\w+)>", RegexOptions.Compiled)]
    private static partial Regex EntityDefinitionRegex();

    [GeneratedRegex(@"\.ToTable\(""(\w+)""\)", RegexOptions.Compiled)]
    private static partial Regex TableNameRegex();

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
