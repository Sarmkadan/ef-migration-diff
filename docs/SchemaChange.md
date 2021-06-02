# SchemaChange

Represents a single structural difference detected between two database migration states. Each `SchemaChange` captures the exact nature of a schema modification—whether a table or column was added, altered, or removed—along with the raw SQL that would apply the change, contextual metadata, and utilities for comparing and validating changes across migrations.

## API

### Properties

**`public string Id`**  
A unique identifier for this schema change instance. Typically a GUID or hash derived from the change’s content, used to correlate changes across different migration comparisons.

**`public string MigrationId`**  
The identifier of the migration in which this change was detected. Corresponds to the migration’s name or timestamp-based ID from the EF Core migrations history.

**`public SqlChangeType ChangeType`**  
An enumeration value indicating the category of schema modification. Expected values include `AddTable`, `DropTable`, `AddColumn`, `AlterColumn`, `DropColumn`, and similar structural operations.

**`public string TableName`**  
The name of the database table affected by this change. May be `null` or empty for changes that do not target a specific table (e.g., index-only operations, though typical usage always involves a table).

**`public string ColumnName`**  
The name of the column affected by this change. Set to `null` when the change operates at the table level (e.g., `AddTable`, `DropTable`) rather than on a specific column.

**`public string Sql`**  
The raw SQL statement that would apply this schema change to the database. This is the exact DDL extracted or generated from the migration difference, suitable for review or direct execution.

**`public Dictionary<string, object?> Metadata`**  
A mutable dictionary of arbitrary key-value pairs attached to the change. Used to store additional context such as original CLR type, annotations, provider-specific hints, or custom tags. Values are nullable objects.

**`public int LineNumber`**  
The line number within the migration file where this change’s SQL or declaration appears. Useful for pointing users directly to the relevant location in source control or migration scripts.

**`public string? OldValue`**  
For alter operations, the previous value of a property that changed (e.g., old column type, old nullable flag, old default). `null` when the change is an addition or removal, or when no prior value is applicable.

**`public string? NewValue`**  
For alter operations, the new value of a property that changed. `null` when the change is an addition or removal, or when no new value is applicable.

**`public string? DefaultValue`**  
The default value assigned to a column, if one is specified in the migration. `null` when no default is defined or when the change does not involve column defaults.

**`public bool IsValid`**  
Returns `true` if the change object is in a consistent, usable state. Validation criteria are internal to the type and typically verify that required fields (`Id`, `MigrationId`, `ChangeType`, `Sql`) are populated and logically coherent. Invalid changes should be discarded or regenerated.

### Constructors

**`public SchemaChange()`**  
Parameterless constructor. Creates an empty `SchemaChange` instance with default property values. The `Metadata` dictionary is initialized as empty. The instance will report `IsValid` as `false` until all required fields are set.

**`public SchemaChange(/* parameters inferred from typical usage */)`**  
Parameterized constructor. Accepts values for the core properties (`Id`, `MigrationId`, `ChangeType`, `TableName`, `ColumnName`, `Sql`, `LineNumber`, and optionally `OldValue`, `NewValue`, `DefaultValue`) and initializes the `Metadata` dictionary. The resulting instance is expected to be valid immediately upon construction when all required arguments are supplied.

### Methods

**`public string GetDescription()`**  
Returns a human-readable, single-line summary of the change. The description is generated from `ChangeType`, `TableName`, and `ColumnName` (e.g., *“AddColumn ‘Price’ to table ‘Products’”*). Takes no parameters. Never throws; returns a fallback string even if some properties are null.

**`public bool AffectsSameTable(SchemaChange other)`**  
Determines whether this change and the `other` change target the same database table. Returns `true` if both `TableName` values are non-null and equal using ordinal case-insensitive comparison. Returns `false` if either `TableName` is null or the names differ. Throws `ArgumentNullException` if `other` is null.

**`public bool ConflictsWith(SchemaChange other)`**  
Checks whether this change logically conflicts with the `other` change. A conflict exists when both changes affect the same table and column and their operations cannot coexist (e.g., one drops a column the other alters, or both add the same column with differing definitions). Returns `true` if a conflict is detected; `false` otherwise. Throws `ArgumentNullException` if `other` is null.

**`public void AddMetadata(string key, object? value)`**  
Adds or overwrites an entry in the `Metadata` dictionary. If the key already exists, its value is replaced. Throws `ArgumentNullException` if `key` is null. The `value` parameter is nullable and stored as-is.

**`public object? GetMetadata(string key)`**  
Retrieves the value associated with the specified key from the `Metadata` dictionary. Returns the stored object (which may be null) if the key exists; returns `null` if the key is not present. Throws `ArgumentNullException` if `key` is null.

**`public bool IsDestructive()`**  
Returns `true` if the change represents a destructive operation—specifically `DropTable`, `DropColumn`, or any alteration that removes or restricts existing schema elements. Non-destructive changes (additions, expansions) return `false`. Takes no parameters and never throws.

## Usage

### Example 1: Detecting Destructive Changes in a Diff

```csharp
using var diffRunner = new MigrationDiffRunner(sourceMigration, targetMigration);
IReadOnlyList<SchemaChange> changes = diffRunner.GenerateDiff();

var destructiveChanges = new List<SchemaChange>();

foreach (var change in changes)
{
    if (!change.IsValid)
    {
        Console.WriteLine($"Skipping invalid change at line {change.LineNumber}");
        continue;
    }

    if (change.IsDestructive())
    {
        destructiveChanges.Add(change);
        Console.WriteLine($"DESTRUCTIVE: {change.GetDescription()}");
        Console.WriteLine($"  SQL: {change.Sql}");
    }
}

if (destructiveChanges.Any())
{
    Console.WriteLine($"Warning: {destructiveChanges.Count} destructive changes detected.");
}
```

### Example 2: Comparing Changes Across Two Migration Pairs

```csharp
var changesV1toV2 = diffRunner.GenerateDiff(migrationV1, migrationV2);
var changesV2toV3 = diffRunner.GenerateDiff(migrationV2, migrationV3);

foreach (var newChange in changesV2toV3)
{
    newChange.AddMetadata("reviewed", false);
    newChange.AddMetadata("reviewer", null);

    foreach (var priorChange in changesV1toV2)
    {
        if (newChange.ConflictsWith(priorChange))
        {
            Console.WriteLine($"Conflict: '{newChange.GetDescription()}' conflicts with prior '{priorChange.GetDescription()}'");
            newChange.AddMetadata("hasConflict", true);
            newChange.AddMetadata("conflictingChangeId", priorChange.Id);
            break;
        }

        if (newChange.AffectsSameTable(priorChange))
        {
            Console.WriteLine($"Info: Both changes affect table '{newChange.TableName}'");
        }
    }
}

var reviewedCount = changesV2toV3.Count(c => c.GetMetadata("reviewed") is bool b && b);
Console.WriteLine($"Reviewed changes: {reviewedCount}/{changesV2toV3.Count}");
```

## Notes

- **Validity**: A `SchemaChange` constructed with the parameterless constructor is not valid until all required properties are set. Always check `IsValid` before relying on `GetDescription()`, `ConflictsWith()`, or `IsDestructive()`. The parameterized constructor produces a valid instance immediately.
- **Metadata thread safety**: The `Metadata` dictionary is a standard `Dictionary<string, object?>` and is not thread-safe. Concurrent calls to `AddMetadata` or `GetMetadata` from multiple threads will cause corruption or exceptions. Synchronize access externally if sharing a `SchemaChange` across threads.
- **Null handling in `AffectsSameTable`**: If either change has a null `TableName`, the method returns `false`. This is by design—two changes without table context are not considered to affect the same table, even if both are null.
- **Conflict detection scope**: `ConflictsWith` performs a logical comparison based on `ChangeType`, `TableName`, and `ColumnName`. It does not parse or compare the raw `Sql` strings. Two changes with identical SQL but different metadata may not be flagged as conflicting if their declared operations are compatible.
- **`OldValue`/`NewValue` semantics**: These properties are populated only for alter-type changes (`AlterColumn`, `AlterTable`). For additions, both are null. For removals, `OldValue` may contain the previous definition while `NewValue` is null. Do not assume symmetry.
- **`IsDestructive` granularity**: The method classifies `DropTable` and `DropColumn` as destructive. Column type changes that narrow the domain (e.g., `varchar(100)` to `varchar(50)`) may or may not be considered destructive depending on the implementation’s policy. Check the specific version’s behavior if relying on this for safety gates.
