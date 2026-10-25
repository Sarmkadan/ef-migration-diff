# MigrationDependencyGraph

The graph model in `Models/MigrationDependencyGraph.cs` (namespace `EfMigrationDiff.Models`) represents the ordering relationships between EF Core migrations as a directed graph. It holds the nodes, the edges, and the cycle state, and answers topology questions: topological order, ancestors, descendants, heads, orphans, and unreachable nodes.

The model is a plain data structure with graph algorithms. It has no knowledge of files, git, or EF Core. Building the graph from `Migration` objects is the job of `MigrationDependencyGraphService` (see [MigrationDependencyGraphService.md](MigrationDependencyGraphService.md)).

## Types

The file defines four public types.

| Type | Kind | Purpose |
|---|---|---|
| `MigrationGraphNode` | `sealed class` | One migration in the graph. |
| `MigrationGraphEdge` | `sealed record` | One directed dependency between two migrations. |
| `DependencyKind` | `enum` | Why an edge exists. |
| `MigrationDependencyGraph` | `sealed class` | The graph: nodes, edges, cycle state, and analysis methods. |

### `MigrationGraphNode`

| Property | Type | Meaning |
|---|---|---|
| `MigrationId` | `string` | EF-format timestamp identifier, e.g. `20240115093045`. This is the key in the graph. |
| `Name` | `string` | Human-readable migration name. |
| `DbContextName` | `string` | The DbContext the migration belongs to. |
| `Sequence` | `int` | Application order within the chain, 1-based. |
| `Status` | `MigrationStatus` | `Pending`, `Applied`, `Reverted`, `Failed`, or `Superseded`. |

All properties use `init`, so a node is set at construction and is not changed afterward. `ToString()` returns `[SSSS] <MigrationId> — <Name>`, with `Sequence` padded to four digits.

### `MigrationGraphEdge`

```csharp
public sealed record MigrationGraphEdge(string FromId, string ToId, DependencyKind Kind = DependencyKind.Sequential);
```

- `FromId` is the prerequisite: the migration that must be applied first.
- `ToId` is the dependent: the migration that relies on `FromId`.
- `Kind` defaults to `DependencyKind.Sequential`.

Edges always point from prerequisite to dependent. Read `A → B` as "A must come before B".

### `DependencyKind`

| Value | Meaning | Produced by |
|---|---|---|
| `Sequential` (0) | Implied by timestamp order. | `MigrationDependencyGraphService.Build` |
| `Explicit` (1) | The downstream migration references the upstream one through an `[MigrationId]` attribute. | Not produced anywhere in the current code. |
| `TableShared` (2) | Both migrations change the same table, so their order matters. | `MigrationDependencyGraphService.Build` |

## `MigrationDependencyGraph`

### State

- `_nodes`: a `Dictionary<string, MigrationGraphNode>` with ordinal-ignore-case keys.
- `_edges`: a `List<MigrationGraphEdge>` in insertion order.
- `_cyclePath`: the last cycle found, or `null`.

Public read-only views:

| Member | Type | Notes |
|---|---|---|
| `Nodes` | `IReadOnlyDictionary<string, MigrationGraphNode>` | Keyed by `MigrationId`. |
| `Edges` | `IReadOnlyList<MigrationGraphEdge>` | In insertion order. |
| `IsEmpty` | `bool` | `true` when there are no nodes. |
| `HasCycles` | `bool` | `true` when `_cyclePath` is set. |
| `CyclePath` | `IReadOnlyList<string>?` | Migration IDs forming a cycle, or `null`. |

### Mutation

**`AddNode(MigrationGraphNode node)`** stores the node under `node.MigrationId`. An existing node with the same ID is replaced silently. Existing edges are kept.

**`AddEdge(MigrationGraphEdge edge)`** does the following, in order:

1. Throws `ArgumentNullException` if `edge` is `null`.
2. Throws `ArgumentException` if `FromId` or `ToId` is not a node in the graph.
3. Adds the edge unless an edge with the same `FromId` and `ToId` already exists. The check ignores `Kind`, so the first edge added between a pair wins.
4. Re-runs cycle detection over the whole graph.

Because cycle detection runs on every call, building a graph of *n* edges costs roughly *n* full traversals.

### Analysis

All analysis methods read the graph and do not change it. Two helpers build the adjacency maps they need: `BuildAdjacency()` maps each node to its prerequisites, and `BuildForwardAdjacency()` maps each node to its dependents.

| Method | Returns | Behavior |
|---|---|---|
| `GetTopologicalOrder()` | `IReadOnlyList<MigrationGraphNode>` | Depth-first, prerequisites first. Nodes are visited in ascending `Sequence`. Before each node is emitted, its prerequisites are emitted, also in ascending `Sequence`. Returns an empty list when the graph is empty or has a cycle. |
| `GetAncestors(id)` | `IReadOnlySet<string>` | All direct and transitive prerequisites of `id`. Does not include `id` itself. Empty if `id` is unknown. |
| `GetDescendants(id)` | `IReadOnlySet<string>` | All direct and transitive dependents of `id`. Does not include `id` itself. Empty if `id` is unknown. |
| `GetHeadMigrations()` | `IReadOnlyList<string>` | Nodes with no dependents. A linear chain has one head. Several heads mean the chain has branched. |
| `GetOrphanMigrations()` | `IReadOnlySet<string>` | Nodes with no dependents. This is the same condition as `GetHeadMigrations()`, returned as a set. |
| `GetUnreachableMigrations()` | `IReadOnlySet<string>` | Nodes that are neither a head nor an ancestor of a head. It walks backward from every head through prerequisites, then returns every node it did not visit. Returns empty when there are no heads, which only happens when every node has a dependent, that is, when the graph has a cycle. |
| `ToString()` | `string` | `MigrationDependencyGraph(Nodes: N, Edges: M, HasCycles: b)`. |

`GetOrphanMigrations()` and `GetHeadMigrations()` return the same IDs. They differ only in return type.

When the graph has a cycle, `GetTopologicalOrder()` returns an empty list. `GetHeadMigrations()` and `GetOrphanMigrations()` still answer from the edges.

## Cycle detection

`DetectCycles()` runs after every successful `AddEdge`. It is an iterative depth-first search over an explicit stack, so deep chains do not overflow the call stack. Nodes are marked as in progress when first expanded. The first back edge it finds sets `_cyclePath` to the cycle and stops the search. If it finds no back edge, `_cyclePath` is set to `null`.

A cycle can only come from edges added through `AddEdge`. `MigrationDependencyGraphService.Build` adds edges from earlier to later entries in sorted order, so a graph it builds is acyclic, with one exception: duplicate migration IDs in the input can produce a self-edge.

## How the graph is built and consumed

### Build

```
IEnumerable<Migration>
        │
        ▼
MigrationDependencyGraphService.Build()
   1. AddNode for each migration
   2. AddEdge(Sequential) between consecutive migrations
   3. AddEdge(TableShared) between consecutive migrations touching the same table
        │
        ▼
MigrationDependencyGraph
```

The sort key is `Sequence`, then `Id`. A node's `Sequence` is the migration's own value when it is positive, and its 1-based position otherwise.

### Consumers

| Consumer | Uses |
|---|---|
| `Program.cs` (`graph` and `dependency-graph` commands) | `Nodes`, `Edges`, `GetTopologicalOrder()`, `HasCycles`. Exits with code 1 when the graph has a cycle. |
| `MigrationDependencyGraphService.RenderDot` | `Nodes`, `Edges`. |
| `MigrationDependencyGraphService.RenderText` | `Nodes`, `Edges`, `GetTopologicalOrder()`, `HasCycles`, `IsEmpty`. |
| `MigrationDependencyGraphService.GetRollbackImpact` | `GetDescendants()`, `Nodes`. |
| `Services/MigrationPlanner.cs` | `Edges`. It runs its own Kahn's algorithm rather than calling `GetTopologicalOrder()`. |

## Example

```csharp
using EfMigrationDiff.Models;

var graph = new MigrationDependencyGraph();

graph.AddNode(new MigrationGraphNode
{
    MigrationId = "20240101090000",
    Name = "CreateBlogs",
    DbContextName = "BloggingContext",
    Sequence = 1,
    Status = MigrationStatus.Applied
});

graph.AddNode(new MigrationGraphNode
{
    MigrationId = "20240102100000",
    Name = "AddBlogUrl",
    DbContextName = "BloggingContext",
    Sequence = 2,
    Status = MigrationStatus.Pending
});

graph.AddEdge(new MigrationGraphEdge("20240101090000", "20240102100000", DependencyKind.Sequential));

graph.GetTopologicalOrder();               // CreateBlogs, AddBlogUrl
graph.GetDescendants("20240101090000");    // { "20240102100000" }
graph.GetHeadMigrations();                 // [ "20240102100000" ]
graph.HasCycles;                           // false
```

## Limitations

- **Edges are checked by endpoint and direction only.** A second edge between the same pair with a different `Kind` is ignored.
- **`Explicit` edges are not produced.** The model defines the kind, but no code detects `[MigrationId]` attributes yet.
- **Cycle state is recomputed on every edge.** Each `AddEdge` runs a full traversal, so building with *E* edges costs roughly O(E × (V + E)). This is fine for typical migration counts. There is no batch-add API, so bulk construction pays this cost on every call.
- **Lookups are linear.** The duplicate check in `AddEdge` and `GetUnreachableMigrations()` scan `_edges`, so each is O(E) per call.
- **Head and orphan results are not ordered.** They come from dictionary iteration order. Sort them if you need a stable order.
