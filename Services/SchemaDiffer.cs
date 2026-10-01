#nullable enable

using EfMigrationDiff.Models;
using Microsoft.Extensions.Logging;

namespace EfMigrationDiff.Services;

/// <summary>
/// Compares two sets of schema changes and produces a structured diff describing
/// additions, removals, and modifications between them.
/// </summary>
public class SchemaDiffer
{
    private readonly ILogger<SchemaDiffer> _logger;

    public SchemaDiffer(ILogger<SchemaDiffer> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Computes the difference between source and target schema changes,
    /// grouping results by table name.
    /// </summary>
    /// <param name="source">Schema changes from the source branch.</param>
    /// <param name="target">Schema changes from the target branch.</param>
    /// <returns>A dictionary keyed by table name with categorized diff entries.</returns>
    public Dictionary<string, TableDiffSummary> DiffByTable(
        IReadOnlyList<SchemaChange> source,
        IReadOnlyList<SchemaChange> target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        _logger.LogInformation("Diffing {SourceCount} source vs {TargetCount} target changes",
            source.Count, target.Count);

        var sourceByTable = source.GroupBy(c => c.TableName).ToDictionary(g => g.Key, g => g.ToList());
        var targetByTable = target.GroupBy(c => c.TableName).ToDictionary(g => g.Key, g => g.ToList());
        var allTables = sourceByTable.Keys.Union(targetByTable.Keys).Distinct();

        var result = new Dictionary<string, TableDiffSummary>();

        foreach (var table in allTables)
        {
            sourceByTable.TryGetValue(table, out var srcChanges);
            targetByTable.TryGetValue(table, out var tgtChanges);
            result[table] = ComputeTableDiff(table, srcChanges, tgtChanges);
        }

        return result;
    }

    /// <summary>
    /// Identifies schema changes that exist in the target but not in the source.
    /// </summary>
    /// <param name="source">The baseline schema changes.</param>
    /// <param name="target">The compared schema changes.</param>
    /// <returns>Changes present only in the target.</returns>
    public IReadOnlyList<SchemaChange> FindAdded(
        IReadOnlyList<SchemaChange> source,
        IReadOnlyList<SchemaChange> target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        var sourceIds = new HashSet<string>(source.Select(c => c.Id));
        return target.Where(c => !sourceIds.Contains(c.Id)).ToList();
    }

    /// <summary>
    /// Identifies schema changes that exist in the source but not in the target.
    /// </summary>
    /// <param name="source">The baseline schema changes.</param>
    /// <param name="target">The compared schema changes.</param>
    /// <returns>Changes present only in the source.</returns>
    public IReadOnlyList<SchemaChange> FindRemoved(
        IReadOnlyList<SchemaChange> source,
        IReadOnlyList<SchemaChange> target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        var targetIds = new HashSet<string>(target.Select(c => c.Id));
        return source.Where(c => !targetIds.Contains(c.Id)).ToList();
    }

    /// <summary>
    /// Detects changes that target the same table and column but differ in operation or value.
    /// </summary>
    /// <param name="source">Schema changes from the source branch.</param>
    /// <param name="target">Schema changes from the target branch.</param>
    /// <returns>Pairs of conflicting source and target changes.</returns>
    public IReadOnlyList<(SchemaChange Source, SchemaChange Target)> FindConflicting(
        IReadOnlyList<SchemaChange> source,
        IReadOnlyList<SchemaChange> target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        var conflicts = new List<(SchemaChange, SchemaChange)>();

        foreach (var src in source)
        {
            foreach (var tgt in target)
            {
                if (src.TableName == tgt.TableName
                    && src.ColumnName == tgt.ColumnName
                    && !string.IsNullOrEmpty(src.ColumnName)
                    && src.ChangeType != tgt.ChangeType)
                {
                    conflicts.Add((src, tgt));
                }
            }
        }

        _logger.LogInformation("Found {ConflictCount} conflicting change pairs", conflicts.Count);
        return conflicts;
    }

    private TableDiffSummary ComputeTableDiff(
        string tableName,
        List<SchemaChange>? source,
        List<SchemaChange>? target)
    {
        return new TableDiffSummary
        {
            TableName = tableName,
            SourceOnly = source?.Where(s => target is null || !target.Any(t => t.Id == s.Id)).ToList() ?? [],
            TargetOnly = target?.Where(t => source is null || !source.Any(s => s.Id == t.Id)).ToList() ?? [],
            SharedCount = source is not null && target is not null
                ? source.Count(s => target.Any(t => t.Id == s.Id))
                : 0
        };
    }
}

/// <summary>
/// Summary of differences for a single table between source and target branches.
/// </summary>
public class TableDiffSummary
{
    /// <summary>Table name this summary applies to.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Changes present only in the source branch.</summary>
    public List<SchemaChange> SourceOnly { get; set; } = [];

    /// <summary>Changes present only in the target branch.</summary>
    public List<SchemaChange> TargetOnly { get; set; } = [];

    /// <summary>Number of changes shared between both branches.</summary>
    public int SharedCount { get; set; }
}
