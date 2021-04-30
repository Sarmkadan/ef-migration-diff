#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Models;

/// <summary>
/// A node in the migration dependency graph, representing a single migration.
/// </summary>
public sealed class MigrationGraphNode
{
    /// <summary>The EF-format timestamp identifier of the migration (e.g. "20240115093045").</summary>
    public string MigrationId { get; init; } = string.Empty;

    /// <summary>Human-readable name of the migration.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>DbContext this migration belongs to.</summary>
    public string DbContextName { get; init; } = string.Empty;

    /// <summary>Application order within the migration chain (1-based).</summary>
    public int Sequence { get; init; }

    /// <summary>Status of the migration.</summary>
    public MigrationStatus Status { get; init; }

    /// <summary>
    /// Returns a concise display string combining sequence, id, and name.
    /// </summary>
    public override string ToString() => $"[{Sequence:D4}] {MigrationId} — {Name}";
}

/// <summary>
/// A directed edge between two nodes in the migration dependency graph.
/// The edge points from a prerequisite migration to the one that depends on it.
/// </summary>
/// <param name="FromId">Migration that must be applied first (the prerequisite).</param>
/// <param name="ToId">Migration that depends on <paramref name="FromId"/>.</param>
/// <param name="Kind">Describes why the dependency exists.</param>
public sealed record MigrationGraphEdge(string FromId, string ToId, DependencyKind Kind = DependencyKind.Sequential);

/// <summary>
/// Classifies the reason a dependency edge exists between two migrations.
/// </summary>
public enum DependencyKind
{
    /// <summary>The dependency is implied by execution order (timestamp sequence).</summary>
    Sequential = 0,

    /// <summary>The downstream migration explicitly references the upstream one via an <c>[MigrationId]</c> attribute.</summary>
    Explicit = 1,

    /// <summary>Both migrations target the same table, so ordering is critical.</summary>
    TableShared = 2
}

/// <summary>
/// Directed acyclic graph (DAG) that captures the ordering relationships between migrations.
/// Supports topological sort, cycle detection, and impact analysis.
/// </summary>
public sealed class MigrationDependencyGraph
{
    private readonly Dictionary<string, MigrationGraphNode> _nodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<MigrationGraphEdge> _edges = new();

    /// <summary>All nodes in the graph (keyed by migration ID).</summary>
    public IReadOnlyDictionary<string, MigrationGraphNode> Nodes => _nodes;

    /// <summary>All directed edges in the graph.</summary>
    public IReadOnlyList<MigrationGraphEdge> Edges => _edges;

    /// <summary>Returns <c>true</c> when no nodes have been added.</summary>
    public bool IsEmpty => _nodes.Count == 0;

    /// <summary>
    /// Returns <c>true</c> when the graph contains at least one cycle,
    /// which would prevent a clean sequential migration run.
    /// </summary>
    public bool HasCycles { get; private set; }

    // =========================================================================
    // Mutation
    // =========================================================================

    /// <summary>Adds a migration node; silently replaces any existing node with the same id.</summary>
    /// <param name="node">The node to add.</param>
    public void AddNode(MigrationGraphNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _nodes[node.MigrationId] = node;
    }

    /// <summary>
    /// Adds a directed dependency edge.
    /// Both endpoint IDs must already exist as nodes; duplicate edges are ignored.
    /// </summary>
    /// <param name="edge">The edge to add.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when either endpoint is not present in <see cref="Nodes"/>.
    /// </exception>
    public void AddEdge(MigrationGraphEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);

        if (!_nodes.ContainsKey(edge.FromId))
            throw new ArgumentException($"Node '{edge.FromId}' not found in graph.", nameof(edge));

        if (!_nodes.ContainsKey(edge.ToId))
            throw new ArgumentException($"Node '{edge.ToId}' not found in graph.", nameof(edge));

        // Avoid duplicate edges
        if (!_edges.Any(e => e.FromId == edge.FromId && e.ToId == edge.ToId))
            _edges.Add(edge);

        HasCycles = DetectCycles();
    }

    // =========================================================================
    // Analysis
    // =========================================================================

    /// <summary>
    /// Returns migration nodes in topological order (ancestors first).
    /// Nodes at the same depth are ordered by their <see cref="MigrationGraphNode.Sequence"/>.
    /// </summary>
    /// <returns>
    /// An ordered list of <see cref="MigrationGraphNode"/> values; empty if the graph has cycles
    /// or no nodes.
    /// </returns>
    public IReadOnlyList<MigrationGraphNode> GetTopologicalOrder()
    {
        if (IsEmpty || HasCycles)
            return Array.Empty<MigrationGraphNode>();

        var visited   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result    = new List<MigrationGraphNode>();
        var adjacency = BuildAdjacency();

        void Visit(string id)
        {
            if (!visited.Add(id)) return;

            // Visit all prerequisites first
            if (adjacency.TryGetValue(id, out var prereqs))
                foreach (var prereq in prereqs.OrderBy(p => _nodes[p].Sequence))
                    Visit(prereq);

            result.Add(_nodes[id]);
        }

        foreach (var id in _nodes.Keys.OrderBy(k => _nodes[k].Sequence))
            Visit(id);

        return result;
    }

    /// <summary>
    /// Returns all migration IDs that must be applied before <paramref name="migrationId"/>
    /// (direct and transitive predecessors).
    /// </summary>
    /// <param name="migrationId">The migration whose ancestors are needed.</param>
    /// <returns>A set of migration IDs; empty when the migration has no prerequisites.</returns>
    public IReadOnlySet<string> GetAncestors(string migrationId)
    {
        var result    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var adjacency = BuildAdjacency();
        CollectAncestors(migrationId, adjacency, result);
        return result;
    }

    /// <summary>
    /// Returns all migration IDs that directly or transitively depend on
    /// <paramref name="migrationId"/> (successors).
    /// </summary>
    /// <param name="migrationId">The migration whose descendants are needed.</param>
    /// <returns>A set of migration IDs; empty when nothing depends on the given migration.</returns>
    public IReadOnlySet<string> GetDescendants(string migrationId)
    {
        var result  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var forward = BuildForwardAdjacency();
        CollectDescendants(migrationId, forward, result);
        return result;
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    /// <summary>Builds a map from each node to the nodes it directly depends on (predecessors).</summary>
    private Dictionary<string, List<string>> BuildAdjacency()
    {
        var adj = _nodes.Keys.ToDictionary(k => k, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var edge in _edges)
            adj[edge.ToId].Add(edge.FromId);

        return adj;
    }

    /// <summary>Builds a map from each node to the nodes that directly depend on it (successors).</summary>
    private Dictionary<string, List<string>> BuildForwardAdjacency()
    {
        var fwd = _nodes.Keys.ToDictionary(k => k, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var edge in _edges)
            fwd[edge.FromId].Add(edge.ToId);

        return fwd;
    }

    private static void CollectAncestors(
        string id,
        Dictionary<string, List<string>> adj,
        HashSet<string> visited)
    {
        if (!adj.TryGetValue(id, out var parents)) return;

        foreach (var parent in parents)
        {
            if (visited.Add(parent))
                CollectAncestors(parent, adj, visited);
        }
    }

    private static void CollectDescendants(
        string id,
        Dictionary<string, List<string>> fwd,
        HashSet<string> visited)
    {
        if (!fwd.TryGetValue(id, out var children)) return;

        foreach (var child in children)
        {
            if (visited.Add(child))
                CollectDescendants(child, fwd, visited);
        }
    }

    /// <summary>
    /// Detects cycles using depth-first search with three-color marking
    /// (white = unvisited, grey = in-progress, black = done).
    /// </summary>
    private bool DetectCycles()
    {
        var colors    = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var adjacency = BuildForwardAdjacency();

        bool Dfs(string id)
        {
            colors[id] = 1; // grey — in progress

            if (adjacency.TryGetValue(id, out var children))
            {
                foreach (var child in children)
                {
                    colors.TryGetValue(child, out var color);
                    if (color == 1) return true;  // back edge → cycle
                    if (color == 0 && Dfs(child)) return true;
                }
            }

            colors[id] = 2; // black — done
            return false;
        }

        foreach (var id in _nodes.Keys)
        {
            colors.TryGetValue(id, out var c);
            if (c == 0 && Dfs(id)) return true;
        }

        return false;
    }
}
