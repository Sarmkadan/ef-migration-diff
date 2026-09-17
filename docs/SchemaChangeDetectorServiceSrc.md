# SchemaChangeDetectorService (src/Services)

> **Work-in-progress stub.** This documents the in-progress class at
> `src/Services/SchemaChangeDetectorService.cs`. It is a distinct, unfinished
> scaffold and is **not** the production service at
> `Services/SchemaChangeDetectorService.cs` (see
> [SchemaChangeDetectorService.md](./SchemaChangeDetectorService.md) for that
> one). The two share a type name but live in different namespaces and have
> different APIs. Treat this document as a snapshot of the stub in its current
> state, not a contract.

## Overview

`src/Services/SchemaChangeDetectorService` is an in-progress class in the
`Services` namespace. It currently exposes a single async method,
`DetectChangesAsync`, whose body is a placeholder: it builds an in-memory
schema dictionary keyed by schema/table/column names and re-throws any
exception. The real detection logic has not been implemented yet.

## API

### `Task DetectChangesAsync(string schemaName, string tableName, string columnName, CancellationToken cancellationToken = default)`

Asynchronously detects schema changes for the given schema, table, and column.

- **Parameters**:
  - `schemaName` (`string`): The name of the schema to inspect.
  - `tableName` (`string`): The name of the table to inspect.
  - `columnName` (`string`): The name of the column to inspect.
  - `cancellationToken` (`CancellationToken`, optional): A token to observe
    while waiting for the task to complete. Defaults to `CancellationToken.None`.
- **Return value**: `Task` representing the asynchronous operation. The method
  does not currently return a result value.
- **Exceptions**: Re-throws any exception raised inside the method body. The
  current placeholder catches `Exception` and immediately re-throws it, so
  callers observe the original exception.

## Current behavior (placeholder)

The method body is a stub. It currently:

1. Creates a `StringComparer.Ordinal` comparer.
2. Builds a `Dictionary<string, Dictionary<string, string>>` schema map,
   registering `schemaName` → `tableName` → `columnName` (the inner dictionary
   uses `StringComparer.OrdinalIgnoreCase`).
3. Wraps the body in a `try/catch (Exception e)` that re-throws the exception.

No schema comparison, migration parsing, or change detection is performed yet.
The `cancellationToken` parameter is accepted but not currently observed.

## Usage

The method is not yet wired into any caller and has no meaningful observable
behavior beyond the placeholder above. A minimal invocation would look like:

```csharp
var detector = new SchemaChangeDetectorService();
await detector.DetectChangesAsync("dbo", "Users", "Email");
```

## Notes

- This class is a work-in-progress and its signature and behavior are expected
  to change as the implementation is completed.
- It is distinct from the production `SchemaChangeDetectorService` in the
  `EfMigrationDiff.Services` namespace, which exposes a synchronous
  `List<SchemaChange> DetectChanges(Migration)` API. Do not confuse the two.