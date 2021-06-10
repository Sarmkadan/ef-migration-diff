# ConflictDetectionService

A utility service that analyzes database schema differences to detect potential migration conflicts between the current model and an incoming migration. It validates object names against database rules and identifies structural conflicts such as duplicate tables, conflicting column definitions, or incompatible schema changes.

## API

### `public ConflictDetectionService()`

Initializes a new instance of the `ConflictDetectionService` with default validation rules.

### `public List<ConflictInfo> DetectConflicts()`

Scans the current database schema and compares it against the target migration model to identify structural conflicts. Each conflict represents a potential issue that could cause a migration to fail or produce unexpected behavior.

- **Parameters**: None
- **Return value**: A `List<ConflictInfo>` containing zero or more `ConflictInfo` objects describing detected conflicts. Each `ConflictInfo` includes a severity level, message, and details about the conflicting elements.
- **Exceptions**: Throws `InvalidOperationException` if the service is not properly initialized or if schema metadata cannot be loaded.

### `public bool IsValidTableName(string name)`

Determines whether the specified table name is valid according to database naming conventions.

- **Parameters**:
  - `name` (string): The table name to validate.
- **Return value**: `true` if the name is valid; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `name` is `null`.

### `public bool IsValidColumnName(string name)`

Determines whether the specified column name is valid according to database naming conventions.

- **Parameters**:
  - `name` (string): The column name to validate.
- **Return value**: `true` if the name is valid; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `name` is `null`.

## Usage
