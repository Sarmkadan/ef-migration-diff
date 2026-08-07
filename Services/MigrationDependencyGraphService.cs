#nullable enable
using EfMigrationDiff.Models;

namespace EfMigrationDiff.Services;

/// <summary>
/// Builds and analyzes a <see cref="MigrationDependencyGraph"/> from a collection of
/// <see cref="Migration"/> objects, inferring ordering edges from timestamps and
/// detecting shared-table dependencies.
/// </summary>
public sealed class MigrationDependencyGraphService
{
    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>
    /// Constructs a dependency graph from the supplied migrations.
    /// Sequential edges are inferred from the numeric timestamp in each
    /// <see cref="Migration.Id"/> and <see cref="Migration.Sequence"/>.
    /// Shared-table edges are added when multiple migrations touch the same table.
    /// </summary>
    /// <param name="migrations">The migrations to include in the graph.</param>
    /// <returns>
    /// A fully populated <see cref="MigrationDependencyGraph"/> with all inferred edges.
    /// </returns>
    public MigrationDependencyGraph Build(IEnumerable<Migration> migrations)
    {
        ArgumentNullException.ThrowIfNull(migrations);
        var list  = migrations.OrderBy(m => m.Sequence).ThenBy(m => m.Id).ToList();
        var graph = new MigrationDependencyGraph();

        // 1. Add all nodes first
        for (int i = 0; i < list.Count; i++)
        {
            var m = list[i];
            graph.AddNode(new MigrationGraphNode
            {
                MigrationId   = m.Id,
                Name          = m.Name,
                DbContextName = m.DbContextName,
                Sequence      = m.Sequence > 0 ? m.Sequence : i + 1,
                Status        = m.Status
            });
        }

        // 2. Sequential edges: each migration depends on its immediate predecessor
        for (int i = 1; i < list.Count; i++)
        {
            graph.AddEdge(new MigrationGraphEdge(
                list[i - 1].Id,
                list[i].Id,
                DependencyKind.Sequential));
        }

        // 3. Shared-table edges: if a later migration touches a table that an earlier
        //    migration created or altered, add an explicit edge to capture the dependency.
        AddSharedTableEdges(list, graph);

        return graph;
    }

    /// <summary>
    /// Generates a DOT (Graphviz) representation of the graph.
    /// </summary>
    /// <param name="graph">The graph to render.</param>
    /// <returns>A string containing the DOT representation.</returns>
    public string RenderDot(MigrationDependencyGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("digraph MigrationGraph {");
        sb.AppendLine("    node [shape=box];");
        foreach (var node in graph.Nodes.Values.OrderBy(n => n.Sequence))
        {
            sb.AppendLine($"    \"{node.MigrationId}\" [label=\"[{node.Sequence:D4}] {node.Name}\"];");
        }
        foreach (var edge in graph.Edges)
        {
            sb.AppendLine($"    \"{edge.FromId}\" -> \"{edge.ToId}\" [label=\"{edge.Kind}\"];");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Generates a plain-text representation of the graph suitable for console output.
    /// Each line shows one node and its direct dependencies.
    /// </summary>
    /// <param name="graph">The graph to render.</param>
    /// <returns>A multi-line string with the graph topology.</returns>
    public string RenderText(MigrationDependencyGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Migration Dependency Graph  ({graph.Nodes.Count} nodes, {graph.Edges.Count} edges)");
        sb.AppendLine(new string('─', 60));

        if (graph.HasCycles)
            sb.AppendLine("⚠  CYCLE DETECTED — the graph is not a DAG");

        sb.AppendLine();

        var order = graph.IsEmpty
            ? graph.Nodes.Values.OrderBy(n => n.Sequence).ToList()
            : graph.GetTopologicalOrder().ToList();

        foreach (var node in order)
        {
            var prereqs = graph.Edges
                .Where(e => e.ToId.Equals(node.MigrationId, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.FromId)
                .ToList();

            var prereqStr = prereqs.Count > 0
                ? $"  ← depends on: {string.Join(", ", prereqs)}"
                : string.Empty;

            sb.AppendLine($"  {node}{prereqStr}");
        }

        sb.AppendLine();
        sb.AppendLine($"Topological order verified: {!graph.HasCycles}");
        return sb.ToString();
    }

    /// <summary>
    /// Returns the set of migrations that would be affected if
    /// <paramref name="migrationId"/> were rolled back.
    /// Includes the migration itself and all its descendants.
    /// </summary>
    /// <param name="graph">The graph to query.</param>
    /// <param name="migrationId">The migration to analyze rollback impact for.</param>
    /// <returns>A sorted list of impacted migration IDs.</returns>
    public IReadOnlyList<string> GetRollbackImpact(MigrationDependencyGraph graph, string migrationId)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentException.ThrowIfNullOrEmpty(migrationId);
        var descendants = graph.GetDescendants(migrationId);
        var result      = new List<string> { migrationId };
        result.AddRange(descendants);
        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => graph.Nodes.TryGetValue(id, out var n) ? n.Sequence : int.MaxValue)
            .ToList();
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    private static void AddSharedTableEdges(List<Migration> list, MigrationDependencyGraph graph)
    {
        // Collect tables affected per migration
        var tableMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var migration in list)
        {
            var tables = ExtractAffectedTables(migration);
            foreach (var table in tables)
            {
                if (!tableMap.TryGetValue(table, out var ids))
                    tableMap[table] = ids = new List<string>();

                ids.Add(migration.Id);
            }
        }

        // For each table, add edges between consecutive migrations that touch it
        foreach (var (_, ids) in tableMap)
        {
            for (int i = 1; i < ids.Count; i++)
            {
                // Avoid adding a duplicate of the sequential edge
                var from = ids[i - 1];
                var to   = ids[i];

                bool alreadyLinked = graph.Edges.Any(e =>
                    e.FromId.Equals(from, StringComparison.OrdinalIgnoreCase) &&
                    e.ToId.Equals(to,   StringComparison.OrdinalIgnoreCase));

                if (!alreadyLinked)
                    graph.AddEdge(new MigrationGraphEdge(from, to, DependencyKind.TableShared));
            }
        }
    }

    /// <summary>
    /// Extracts table names referenced in a migration's content using simple pattern matching.
    /// Supports the common EF Core migration builder method signatures.
    /// </summary>
    private static IEnumerable<string> ExtractAffectedTables(Migration migration)
    {
        if (string.IsNullOrWhiteSpace(migration.Content))
            yield break;

        // Matches patterns like: .CreateTable(name: "TableName" or name: @"TableName"
        var namePattern = new System.Text.RegularExpressions.Regex(
            @"""(\w+)""\s*[,\)]",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        // Look for table: "Name" or name: "Name" patterns
        var tablePattern = new System.Text.RegularExpressions.Regex(
            @"(?:name|table)\s*:\s*@?""([^""]+)""",
            System.Text.RegularExpressions.RegexOptions.Compiled |
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match m in tablePattern.Matches(migration.Content))
        {
            if (m.Groups[1].Value is { Length: > 0 } tableName)
                yield return tableName;
        }
    }
}
