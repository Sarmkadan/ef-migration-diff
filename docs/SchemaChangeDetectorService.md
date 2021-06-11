# SchemaChangeDetectorService

A service that analyzes and compares database schemas to detect structural changes between an existing database and a target schema definition. It is designed to support Entity Framework migrations by identifying potential breaking changes, destructive operations, and affected database objects before migration execution.

## API

### `List<SchemaChange> DetectChanges()`

Compares the current database schema against the target schema and returns a list of detected schema changes.

- **Parameters**: None
- **Return value**: A `List<SchemaChange>` containing all detected schema changes, including additions, modifications, and removals.
- **Exceptions**: May throw if schema comparison cannot be performed due to connectivity, permissions, or schema parsing errors.

---

### `List<SchemaChange> GetChangesByType(SchemaChangeType changeType)`

Filters the detected schema changes by the specified change type.

- **Parameters**:
  - `changeType` (`SchemaChangeType`): The type of change to filter by (e.g., `Add`, `Modify`, `Remove`).
- **Return value**: A `List<SchemaChange>` containing only changes of the specified type.
- **Exceptions**: Throws `ArgumentOutOfRangeException` if an invalid `SchemaChangeType` is provided.

---

### `List<string> GetAffectedTables()`

Retrieves the names of all database tables affected by detected schema changes.

- **Parameters**: None
- **Return value**: A `List<string>` of table names that are involved in any detected change.
- **Exceptions**: None.

---

### `int CountDestructiveChanges()`

Counts the number of destructive schema changes detected, such as column or table removals.

- **Parameters**: None
- **Return value**: The count of destructive changes (e.g., `DROP COLUMN`, `DROP TABLE`).
- **Exceptions**: None.

---
### `bool IsMigrationSafe()`

Determines whether the detected schema changes can be applied safely without data loss or breaking application functionality.

- **Parameters**: None
- **Return value**: `true` if the migration is deemed safe; otherwise, `false`.
- **Exceptions**: None.

---
### `Dictionary<string, object> GetMigrationMetadata()`

Collects metadata relevant to generating a migration script, such as change summaries and risk indicators.

- **Parameters**: None
- **Return value**: A `Dictionary<string, object>` where keys describe metadata (e.g., `"IsSafe"`, `"AffectedTables"`) and values provide corresponding data.
- **Exceptions**: None.

## Usage

### Example 1: Basic Schema Comparison
