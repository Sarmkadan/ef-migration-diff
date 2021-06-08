# MigrationGraphNode

Represents a node in a directed acyclic graph (DAG) of Entity Framework Core migrations, used to analyze and manipulate migration dependencies and topological ordering.

## API

### `public string MigrationId`
The unique identifier of the migration (e.g., `"202310011200_AddBlogCreatedDate"`).

### `public string Name`
The display name of the migration (e.g., `"AddBlogCreatedDate"`).

### `public string DbContextName`
The name of the `DbContext` associated with this migration.

### `public int Sequence`
The sequence number of the migration, used to determine relative ordering when no explicit dependencies exist.

### `public MigrationStatus Status`
The current status of the migration (e.g., `Pending`, `Applied`, `Removed`).

### `public override string ToString()`
Returns a string representation of the node, typically combining `MigrationId`, `Name`, and `DbContextName` for debugging or display purposes.

### `public sealed record MigrationGraphEdge`
A record representing a directed edge between two `MigrationGraphNode` instances, indicating a dependency from one migration to another.

### `public bool HasCycles`
Gets a value indicating whether the graph contains any cycles. Returns `true` if cycles are detected; otherwise, `false`.

### `public void AddNode(MigrationGraphNode node)`
Adds a new node to the graph.

- **Parameters**:
  - `node`: The `MigrationGraphNode` to add.
- **Throws**:
  - `ArgumentNullException`: If `node` is `null`.
  - `InvalidOperationException`: If a node with the same `MigrationId` already exists.

### `public void AddEdge(MigrationGraphEdge edge)`
Adds a directed edge between two nodes in the graph.

- **Parameters**:
  - `edge`: The `MigrationGraphEdge` to add.
- **Throws**:
  - `ArgumentNullException`: If `edge` is `null`.
  - `InvalidOperationException`: If either node referenced by the edge does not exist in the graph.

### `public IReadOnlyList<MigrationGraphNode> GetTopologicalOrder()`
Returns a topologically sorted list of nodes, such that every node appears before any node that depends on it.

- **Returns**:
  - A read-only list of `MigrationGraphNode` instances in topological order.
- **Throws**:
  - `InvalidOperationException`: If the graph contains cycles (checked via `HasCycles`).

### `public IReadOnlySet<string> GetAncestors(string migrationId)`
Returns the set of migration IDs that are ancestors of the specified migration (i.e., all migrations that must be applied before it).

- **Parameters**:
  - `migrationId`: The ID of the migration whose ancestors are to be retrieved.
- **Returns**:
  - A read-only set of ancestor migration IDs.
- **Throws**:
  - `ArgumentNullException`: If `migrationId` is `null`.
  - `KeyNotFoundException`: If no node with the specified `migrationId` exists.

### `public IReadOnlySet<string> GetDescendants(string migrationId)`
Returns the set of migration IDs that are descendants of the specified migration (i.e., all migrations that depend on it).

- **Parameters**:
  - `migrationId`: The ID of the migration whose descendants are to be retrieved.
- **Returns**:
  - A read-only set of descendant migration IDs.
- **Throws**:
  - `ArgumentNullException`: If `migrationId` is `null`.
  - `KeyNotFoundException`: If no node with the specified `migrationId` exists.

## Usage

### Example 1: Building a migration graph and detecting cycles
