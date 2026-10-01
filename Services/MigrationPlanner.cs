#nullable enable

using EfMigrationDiff.Models;
using Microsoft.Extensions.Logging;

namespace EfMigrationDiff.Services;

/// <summary>
/// Plans the execution order and strategy for applying a set of migrations,
/// resolving dependencies and detecting circular references.
/// </summary>
public class MigrationPlanner
{
    private readonly ILogger<MigrationPlanner> _logger;

    public MigrationPlanner(ILogger<MigrationPlanner> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Builds a topologically sorted execution plan from the provided migrations,
    /// respecting their dependency graph.
    /// </summary>
    /// <param name="migrations">All migrations to include in the plan.</param>
    /// <param name="graph">The dependency graph describing ordering constraints.</param>
    /// <returns>An ordered list of migration IDs representing the execution sequence.</returns>
    public IReadOnlyList<string> BuildExecutionPlan(
        IReadOnlyList<Migration> migrations,
        MigrationDependencyGraph graph)
    {
        ArgumentNullException.ThrowIfNull(migrations);
        ArgumentNullException.ThrowIfNull(graph);

        _logger.LogInformation("Building execution plan for {Count} migrations", migrations.Count);

        var adjacency = new Dictionary<string, List<string>>();
        var inDegree = new Dictionary<string, int>();

        foreach (var m in migrations)
        {
            adjacency.TryAdd(m.Id, []);
            inDegree.TryAdd(m.Id, 0);
        }

        foreach (var edge in graph.Edges)
        {
            if (adjacency.ContainsKey(edge.FromId) && adjacency.ContainsKey(edge.ToId))
            {
                adjacency[edge.FromId].Add(edge.ToId);
                inDegree[edge.ToId] = inDegree.GetValueOrDefault(edge.ToId) + 1;
            }
        }

        var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var sorted = new List<string>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(current);

            foreach (var neighbor in adjacency.GetValueOrDefault(current, []))
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }

        if (sorted.Count < migrations.Count)
        {
            _logger.LogWarning("Circular dependency detected; {Unresolved} migrations could not be ordered",
                migrations.Count - sorted.Count);
        }

        return sorted;
    }

    /// <summary>
    /// Determines which migrations from a candidate set can be safely applied
    /// given the currently applied migrations.
    /// </summary>
    /// <param name="candidates">Migrations to evaluate.</param>
    /// <param name="applied">Already-applied migration IDs.</param>
    /// <returns>The subset of candidates whose prerequisites are satisfied.</returns>
    public IReadOnlyList<Migration> GetApplicable(
        IReadOnlyList<Migration> candidates,
        IReadOnlySet<string> applied)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(applied);

        return candidates
            .Where(m => m.Status == MigrationStatus.Pending && !applied.Contains(m.Id))
            .OrderBy(m => m.Sequence)
            .ToList();
    }

    /// <summary>
    /// Detects circular dependencies within the migration dependency graph.
    /// </summary>
    /// <param name="graph">The dependency graph to analyze.</param>
    /// <returns>A list of migration ID cycles, each represented as a list of IDs forming the cycle.</returns>
    public IReadOnlyList<IReadOnlyList<string>> DetectCycles(MigrationDependencyGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();
        var cycles = new List<IReadOnlyList<string>>();
        var path = new List<string>();

        var adj = new Dictionary<string, List<string>>();
        foreach (var nodeId in graph.Nodes.Keys)
            adj[nodeId] = [];
        foreach (var edge in graph.Edges)
        {
            if (adj.ContainsKey(edge.FromId))
                adj[edge.FromId].Add(edge.ToId);
        }

        foreach (var nodeId in graph.Nodes.Keys)
        {
            if (!visited.Contains(nodeId))
                DfsCycle(nodeId, adj, visited, inStack, path, cycles);
        }

        _logger.LogInformation("Cycle detection complete: {CycleCount} cycles found", cycles.Count);
        return cycles;
    }

    /// <summary>
    /// Estimates the risk level of applying migrations based on breaking change count
    /// and conflict severity.
    /// </summary>
    /// <param name="migrations">The migrations to assess.</param>
    /// <returns>A risk assessment string: Low, Medium, High, or Critical.</returns>
    public string AssessRisk(IReadOnlyList<Migration> migrations)
    {
        ArgumentNullException.ThrowIfNull(migrations);

        var breakingCount = migrations.Sum(m =>
            m.SchemaChanges.Count(c => c.ChangeType is SqlChangeType.DropTable or SqlChangeType.DropColumn));

        var criticalConflicts = migrations.Sum(m =>
            m.DetectedConflicts.Count(c => c.Severity == ConflictSeverity.Critical));

        return (breakingCount, criticalConflicts) switch
        {
            ( > 3, _) or (_, > 0) => "Critical",
            ( > 1, _) => "High",
            ( > 0, _) => "Medium",
            _ => "Low"
        };
    }

    private static void DfsCycle(
        string node,
        Dictionary<string, List<string>> adj,
        HashSet<string> visited,
        HashSet<string> inStack,
        List<string> path,
        List<IReadOnlyList<string>> cycles)
    {
        visited.Add(node);
        inStack.Add(node);
        path.Add(node);

        if (adj.TryGetValue(node, out var deps))
        {
            foreach (var dep in deps)
            {
                if (inStack.Contains(dep))
                {
                    var cycleStart = path.IndexOf(dep);
                    if (cycleStart >= 0)
                        cycles.Add(path.Skip(cycleStart).ToList());
                }
                else if (!visited.Contains(dep))
                {
                    DfsCycle(dep, adj, visited, inStack, path, cycles);
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        inStack.Remove(node);
    }
}
