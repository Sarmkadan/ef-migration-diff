#nullable enable
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
    private List<string>? _cyclePath;

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
    public bool HasCycles => _cyclePath != null;

    /// <summary>
    /// Gets the cycle path if a cycle exists, or null if the graph is acyclic.
    /// The cycle path is a list of migration IDs in the order they form the cycle.
    /// </summary>
    public IReadOnlyList<string>? CyclePath => _cyclePath;

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

        DetectCycles();
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

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<MigrationGraphNode>();
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
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var forward = BuildForwardAdjacency();
        CollectDescendants(migrationId, forward, result);
        return result;
    }

    /// <summary>
    /// Returns all migration IDs that are orphan migrations.
    /// An orphan migration is one that has no dependents (no other migration depends on it).
    /// This typically indicates a migration that was created but never merged or is orphaned from a bad merge.
    /// </summary>
    /// <returns>A set of migration IDs that are orphans.</returns>
    public IReadOnlySet<string> GetOrphanMigrations()
    {
        if (IsEmpty)
            return Array.Empty<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);

        var forward = BuildForwardAdjacency();
        var orphans = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var nodeId in _nodes.Keys)
        {
            if (!forward.TryGetValue(nodeId, out var dependents) || dependents.Count == 0)
            {
                orphans.Add(nodeId);
            }
        }

        return orphans;
    }

    /// <summary>
    /// Returns all migration IDs that are unreachable from any head migration.
    /// A migration is unreachable if it has no path from any head node (a node with no outgoing edges).
    /// This typically indicates migrations that were created but never applied or are orphaned.
    /// </summary>
    /// <returns>A set of migration IDs that are unreachable.</returns>
    public IReadOnlySet<string> GetUnreachableMigrations()
    {
        if (IsEmpty)
            return Array.Empty<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);

        var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var forward = BuildForwardAdjacency();

        // Find all head nodes (nodes with no dependents)
        var headNodes = _nodes.Keys
            .Where(nodeId => !forward.ContainsKey(nodeId) || forward[nodeId].Count == 0)
            .ToList();

        // If there are no head nodes, everything is reachable (cycle or single node)
        if (headNodes.Count == 0)
            return Array.Empty<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Perform BFS from each head node to find all reachable migrations
        var queue = new Queue<string>(headNodes);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
                continue;

            // Add to reachable set
            reachable.Add(current);

            // Find all migrations that this migration depends on (reverse dependencies)
            var prerequisites = _edges
                .Where(e => e.ToId.Equals(current, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.FromId)
                .ToList();

            foreach (var prerequisite in prerequisites)
            {
                if (!visited.Contains(prerequisite))
                {
                    queue.Enqueue(prerequisite);
                }
            }
        }

        // Unreachable migrations are those not in the reachable set
        var unreachable = new HashSet<string>(_nodes.Keys, StringComparer.OrdinalIgnoreCase);
        unreachable.ExceptWith(reachable);

        return unreachable;
    }

    /// <summary>
    /// Returns the head migration(s) - migrations that have no dependents.
    /// In a linear migration chain, there should be exactly one head.
    /// In a branched scenario, there may be multiple heads.
    /// </summary>
    /// <returns>A list of head migration IDs.</returns>
    public IReadOnlyList<string> GetHeadMigrations()
    {
        if (IsEmpty)
            return Array.Empty<string>();

        var forward = BuildForwardAdjacency();
        return forward
            .Where(kvp => kvp.Value.Count == 0)
            .Select(kvp => kvp.Key)
            .ToList();
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
        var colors = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var parent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var adjacency = BuildForwardAdjacency();
        _cyclePath = null;

        // Iterative DFS to avoid stack overflow
        var stack = new Stack<string>();

        foreach (var startId in _nodes.Keys)
        {
            if (colors.ContainsKey(startId))
                continue;

            stack.Push(startId);
            parent[startId] = null;

            while (stack.Count > 0)
            {
                var id = stack.Pop();

                if (colors.TryGetValue(id, out var currentColor))
                {
                    // Already processed this node
                    continue;
                }

                colors[id] = 1; // Mark as grey (in progress)
                stack.Push(id); // Push back to continue processing children

                if (adjacency.TryGetValue(id, out var children))
                {
                    foreach (var child in children)
                    {
                        if (!colors.ContainsKey(child))
                        {
                            parent[child] = id;
                            stack.Push(child);
                        }
                        else if (colors[child] == 1)
                        {
                            // Found a back edge - cycle detected
                            var cycle = new List<string>();
                            var temp = id;
                            do
                            {
                                cycle.Add(temp);
                                temp = parent[temp];
                            } while (temp != null && !string.Equals(temp, child, StringComparison.OrdinalIgnoreCase));

                            if (temp != null) // Valid cycle found
                            {
                                cycle.Add(child); // Complete the cycle
                                cycle.Reverse();
                                _cyclePath = cycle;
                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }
}