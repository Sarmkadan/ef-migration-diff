# MigrationExtensions

Provides static extension methods for analyzing and transforming EF Core migrations within the `ef-migration-diff` tool. These methods enable detection of destructive schema changes, generation of human-readable change summaries, identification of conflicts between migrations, and modification of migration content.

## API

### `HasDestructiveChanges`

```csharp
public static bool HasDestructiveChanges(this Migration migration)
```

**Purpose**  
Determines whether the specified migration contains any destructive schema changes (e.g., dropping tables, columns, or indexes) that could result in data loss.

**Parameters**  
- `migration` – The `Migration` instance to inspect.

**Returns**  
`true` if the migration includes at least one destructive operation; otherwise `false`.

**Throws**  
- `ArgumentNullException` – if `migration` is `null`.

---

### `GetSchemaChangesSummary`

```csharp
public static string GetSchemaChangesSummary(this Migration migration)
```

**Purpose**  
Produces a concise, human-readable summary of all schema changes present in the given migration.

**Parameters**  
- `migration` – The `Migration` instance to summarize.

**Returns**  
A `string` containing the summary. Returns an empty string if the migration has no changes.

**Throws**  
- `ArgumentNullException` – if `migration` is `null`.

---

### `FindConflictsWith`

```csharp
public static List<ConflictInfo> FindConflictsWith(this Migration migration, Migration otherMigration)
```

**Purpose**  
Compares two migrations and returns a list of conflicts that would arise if both were applied. Conflicts typically involve overlapping schema modifications (e.g., both migrations alter the same column).

**Parameters**  
- `migration` – The first migration to compare.  
- `otherMigration` – The second migration to compare against.

**Returns**  
A `List<ConflictInfo>` containing details of each detected conflict. Returns an empty list if no conflicts exist.

**Throws**  
- `ArgumentNullException` – if either `migration` or `otherMigration` is `null`.

---

### `TransformContent`

```csharp
public static Migration TransformContent(this Migration migration, Func<Migration, Migration> transform)
```

**Purpose**  
Applies a user-defined transformation function to the migration’s content and returns a new `Migration` instance with the modified operations.

**Parameters**  
- `migration` – The original `Migration` to transform.  
- `transform` – A delegate that receives the original migration and returns a new `Migration` with the desired changes.

**Returns**  
A new `Migration` instance produced by the `transform` function.

**Throws**  
- `ArgumentNullException` – if `migration` or `transform` is `null`.

---

## Usage

### Example 1: Detecting destructive changes and generating a summary

```csharp
using EfMigrationDiff;
using Microsoft.EntityFrameworkCore.Migrations;

Migration pendingMigration = /* obtain from DbContext or migration loader */;

if (MigrationExtensions.HasDestructiveChanges(pendingMigration))
{
    string summary = MigrationExtensions.GetSchemaChangesSummary(pendingMigration);
    Console.WriteLine("Destructive changes detected:");
    Console.WriteLine(summary);
}
else
{
    Console.WriteLine("Migration is safe to apply.");
}
```

### Example 2: Finding conflicts between two migrations and transforming content

```csharp
using EfMigrationDiff;
using Microsoft.EntityFrameworkCore.Migrations;

Migration migrationA = /* ... */;
Migration migrationB = /* ... */;

List<ConflictInfo> conflicts = MigrationExtensions.FindConflictsWith(migrationA, migrationB);
if (conflicts.Any())
{
    Console.WriteLine($"Found {conflicts.Count} conflict(s). Resolving by removing conflicting operations...");

    Migration resolved = MigrationExtensions.TransformContent(migrationA, original =>
    {
        // Remove all operations that conflict with migrationB
        var filteredOps = original.Operations
            .Where(op => !conflicts.Any(c => c.Operation == op))
            .ToList();
        return new Migration(filteredOps, original.TargetModel);
    });

    // Use resolved migration for further processing
}
```

## Notes

- All methods are static and operate on the provided `Migration` instances without modifying them. The `TransformContent` method returns a new `Migration` object; the original instance remains unchanged.
- `FindConflictsWith` performs a deep comparison of operations. Two migrations that modify the same table in non-overlapping columns are not considered conflicting.
- `HasDestructiveChanges` considers operations such as `DropTableOperation`, `DropColumnOperation`, and `DropIndexOperation` as destructive. Renames and additions are not flagged.
- `GetSchemaChangesSummary` may return a multi-line string. Its exact format is implementation-defined and may vary between versions.
- **Thread safety**: These extension methods are safe for concurrent use on distinct `Migration` instances. If the same `Migration` object is accessed or modified by multiple threads simultaneously, external synchronization is required.
- Passing `null` for any required parameter will result in an `ArgumentNullException`.
