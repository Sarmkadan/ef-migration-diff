# MigrationDependencyGraphService

`MigrationDependencyGraphService` (namespace `EfMigrationDiff.Services`) builds a `MigrationDependencyGraph` from a set of EF Core `Migration` objects and renders or analyzes that graph. It infers two kinds of ordering edges:

- **Sequential** edges, which link each migration to its immediate predecessor in ordering (`Sequence`, then `Id`).
- **Table-shared** edges, which link migrations that touch the same table, as found by a regex scan of the migration source text.

The service is stateless and `sealed`. It is registered as a singleton in `Configuration/DependencyInjection.cs` and resolved by `CLI/Commands/CompareCommand.cs` and `Program.cs`.

The graph model types (`MigrationDependencyGraph`, `MigrationGraphNode`, `MigrationGraphEdge`, `DependencyKind`) live in `Models/MigrationDependencyGraph.cs`.

## API

### `public MigrationDependencyGraph Build(IEnumerable<Migration> migrations)`

Builds the graph in three steps:

1. Sorts the input by `Sequence`, then by `Id`, and adds one node per migration. A node's `Sequence` is the migration's `Sequence` when it is greater than `0`; otherwise it is the 1-based position in the sorted list.
2. Adds a `DependencyKind.Sequential` edge from each migration to the next one in the sorted list.
3. Adds `DependencyKind.TableShared` edges. For each table, consecutive migrations that touch it are linked, unless an edge between that pair already exists.

- **Parameters**: `migrations` - the migrations to include. Must not be `null`.
- **Returns**: a populated `MigrationDependencyGraph`. An empty input gives an empty graph.
- **Throws**: `ArgumentNullException` if `migrations` is `null`.

### `public string RenderDot(MigrationDependencyGraph graph)`

Produces a Graphviz `digraph MigrationGraph { ... }` document. Nodes are ordered by `Sequence` and labeled `[NNNN] Name`. Edges are labeled with their `DependencyKind`.

- **Parameters**: `graph` - the graph to render. Must not be `null`.
- **Returns**: the DOT source as a string.
- **Throws**: `ArgumentNullException` if `graph` is `null`.

Node IDs and names are written inside double quotes without escaping, so names containing `"` produce invalid DOT.

### `public string RenderText(MigrationDependencyGraph graph)`

Produces a console-friendly summary:

- A header with node and edge counts.
- A warning line if `graph.HasCycles` is `true`.
- One line per node in topological order, followed by `← depends on: ...` listing its direct predecessors. An empty graph lists nodes by `Sequence`. A graph with cycles prints no node lines, because `GetTopologicalOrder()` returns an empty list in that case.
- A final line `Topological order verified: <true|false>`, which is `true` when the graph has no cycles.

- **Parameters**: `graph` - the graph to render. Must not be `null`.
- **Returns**: the text as a multi-line string.
- **Throws**: `ArgumentNullException` if `graph` is `null`.

### `public IReadOnlyList<string> GetRollbackImpact(MigrationDependencyGraph graph, string migrationId)`

Returns the migration itself plus all of its transitive descendants (migrations that depend on it, directly or indirectly). The result is de-duplicated and sorted by node `Sequence`. IDs that are not in the graph sort last.

- **Parameters**:
  - `graph` - the graph to query. Must not be `null`.
  - `migrationId` - the migration to analyze. Must not be `null` or empty.
- **Returns**: the impacted migration IDs. The list always contains `migrationId`, even if it is not a node in the graph.
- **Throws**:
  - `ArgumentNullException` if `graph` is `null`.
  - `ArgumentException` if `migrationId` is `null` or empty.

## Behavior notes

- **Table detection is heuristic.** `ExtractAffectedTables` matches any `name:` or `table:` argument followed by a quoted string, case-insensitively, including `@"..."` verbatim strings. It does not parse C#, so it can also pick up names that are not tables.
- **Only consecutive pairs get shared-table edges.** If three migrations touch `Blogs`, the service adds edges `A→B` and `B→C`, not `A→C`. The transitive ordering still holds.
- **`DependencyKind.Explicit` is never produced** by this service. It exists in the model for the `[MigrationId]` attribute case, which this service does not detect.
- **Cycles are reported, not rejected.** Cycles can only come from edges added through `MigrationDependencyGraph.AddEdge`, since `Build` only adds forward edges in sorted order. `HasCycles` and `CyclePath` on the graph expose the result.
- **Duplicate IDs.** `AddNode` overwrites an existing node with the same ID, but the sequential edges are still created for each list entry. Callers should pass unique `Id` values.

## Usage

```csharp
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;

var service = new MigrationDependencyGraphService();

var migrations = new List<Migration>
{
    new Migration("20240101090000", "CreateBlogs", "BloggingContext")
    {
        Sequence = 1,
        Status   = MigrationStatus.Applied,
        Content  = "migrationBuilder.CreateTable(name: \"Blogs\", columns: table => new { })"
    },
    new Migration("20240115093045", "AddBlogCreatedDate", "BloggingContext")
    {
        Sequence = 2,
        Status   = MigrationStatus.Pending,
        Content  = "migrationBuilder.AddColumn<DateTime>(name: \"CreatedAt\", table: \"Blogs\")"
    },
    new Migration("20240201120000", "CreatePosts", "BloggingContext")
    {
        Sequence = 3,
        Status   = MigrationStatus.Pending,
        Content  = "migrationBuilder.CreateTable(name: \"Posts\", columns: table => new { })"
    }
};

MigrationDependencyGraph graph = service.Build(migrations);

// Sequential edges: 1→2, 2→3. Table-shared edge: 1→2 is already linked, so none is added.
Console.WriteLine(service.RenderText(graph));

// Migrations that would be affected by rolling back the first one.
IReadOnlyList<string> impact = service.GetRollbackImpact(graph, "20240101090000");
foreach (var id in impact)
    Console.WriteLine(id);

// Graphviz output, for example: dot -Tpng graph.dot -o graph.png
File.WriteAllText("graph.dot", service.RenderDot(graph));
```

Expected `RenderText` output (abridged):

```
Migration Dependency Graph  (3 nodes, 2 edges)
────────────────────────────────────────────────────────────

  [0001] 20240101090000 — CreateBlogs
  [0002] 20240115093045 — AddBlogCreatedDate  ← depends on: 20240101090000
  [0003] 20240201120000 — CreatePosts  ← depends on: 20240115093045

Topological order verified: True
```

`GetRollbackImpact` on `20240101090000` returns all three IDs, in sequence order.
