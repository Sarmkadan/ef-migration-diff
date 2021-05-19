# MigrationDependencyGraphTests

The `MigrationDependencyGraphTests` class contains unit tests for the `MigrationDependencyGraph` type, which models dependencies between database migrations in the `ef-migration-diff` project. Each test method validates a specific behavior of the graph, such as construction, topological ordering, cycle detection, ancestor/descendant queries, rollback impact analysis, and text rendering. The tests are designed to be run with a standard test framework (e.g., xUnit, NUnit) and cover both expected success paths and error conditions.

## API

All methods are public, return `void`, and accept no parameters. They are intended to be invoked by a test runner.

- **`Build_WithEmptyList_ReturnsEmptyGraph`**  
  Verifies that constructing a `MigrationDependencyGraph` from an empty list of migrations produces a graph with no nodes and no edges.

- **`Build_WithSingleMigration_ProducesSingleNode`**  
  Confirms that a graph built from a single migration contains exactly one node and no edges.

- **`Build_WithTwoMigrations_AddsSequentialEdge`**  
  Ensures that when two migrations are provided in order, a directed edge is added from the first to the second, representing a sequential dependency.

- **`Build_WithMigrationsTouchingSameTable_AddsSharedTableEdge`**  
  Validates that migrations that modify the same database table are connected by an edge, even if they are not directly sequential.

- **`GetTopologicalOrder_WithLinearChain_ReturnsMigrationsInOrder`**  
  Tests that a linear chain of dependencies yields a topological order matching the original sequence.

- **`HasCycles_WithAcyclicGraph_ReturnsFalse`**  
  Asserts that an acyclic graph correctly reports no cycles.

- **`GetAncestors_ReturnsAllPredecessors`**  
  Checks that the set of ancestors for a given node includes all nodes that can reach it via directed paths.

- **`GetDescendants_ReturnsAllSuccessors`**  
  Verifies that the set of descendants for a given node includes all nodes reachable from it.

- **`GetRollbackImpact_IncludesTargetAndAllDescendants`**  
  Ensures that the rollback impact set contains the target migration itself and every migration that depends on it (directly or transitively).

- **`RenderText_ProducesMeaningfulOutput`**  
  Confirms that the text representation of the graph is non‑empty and contains recognizable information about nodes and edges.

- **`AddEdge_WithUnknownNode_ThrowsArgumentException`**  
  Tests that attempting to add an edge referencing a node not present in the graph throws an `ArgumentException`.

- **`GetTopologicalOrder_WithCyclicGraph_ReturnsEmpty`**  
  Validates that when the graph contains a cycle, `GetTopologicalOrder` returns an empty list (or equivalent indicator) rather than an invalid ordering.

## Usage

The following examples demonstrate how the `MigrationDependencyGraph` class is used in practice. The tests in `MigrationDependencyGraphTests` verify that these operations behave correctly.

**Example 1: Building a graph and checking for cycles**

```csharp
var migrations = new List<Migration>
{
    new Migration("M1", new[] { "TableA" }),
    new Migration("M2", new[] { "TableA" }),
    new Migration("M3", new[] { "TableB" })
};

var graph = new MigrationDependencyGraph(migrations);
bool hasCycles = graph.HasCycles(); // false
var order = graph.GetTopologicalOrder(); // ["M1", "M2", "M3"] or similar
```

**Example 2: Determining rollback impact**

```csharp
var migrations = new List<Migration>
{
    new Migration("Init", new[] { "Users" }),
    new Migration("AddEmail", new[] { "Users" }),
    new Migration("AddIndex", new[] { "Users" })
};

var graph = new MigrationDependencyGraph(migrations);
var impact = graph.GetRollbackImpact("AddEmail");
// impact contains "AddEmail" and "AddIndex" (descendants)
```

## Notes

- **Edge cases**  
  The tests cover empty input, single‑node graphs, linear chains, shared‑table dependencies, and cyclic graphs. The `AddEdge_WithUnknownNode_ThrowsArgumentException` test ensures that invalid node references are rejected early. When a cycle is present, `GetTopologicalOrder` returns an empty list rather than throwing an exception.

- **Thread safety**  
  `MigrationDependencyGraph` is not designed for concurrent use. The test class itself is single‑threaded and should be executed in a test runner that isolates test instances. No thread‑safety guarantees are provided for the underlying graph operations.

- **Test framework**  
  The tests are written in a style compatible with xUnit or NUnit. They assume a standard `[Fact]` or `[Test]` attribute and are intended to be run with `dotnet test` or an equivalent command.
