# MigrationDiffExtensions

The `MigrationDiffExtensions` static class provides a set of extension methods for analyzing a `MigrationDiff` object produced by the ef-migration-diff library. These methods offer quick insights into the number of migrations, schema changes, destructive operations, and overall migration health without requiring manual traversal of the diff structure.

## API

All methods are static and extend the `MigrationDiff` type. They throw `ArgumentNullException` if the `diff` argument is `null`.

### `GetTotalMigrations`

```csharp
public static int GetTotalMigrations(this MigrationDiff diff)
```

Returns the total number of migrations present in the diff. This includes all migrations, regardless of their status.

- **Parameters**: `diff` – the `MigrationDiff` instance.
- **Returns**: The total count of migrations.
- **Throws**: `ArgumentNullException` if `diff` is `null`.

### `GetMigrationsNeedingAttention`

```csharp
public static int GetMigrationsNeedingAttention(this MigrationDiff diff)
```

Returns the number of migrations that require manual review or intervention (e.g., pending, conflicting, or unapplied migrations).

- **Parameters**: `diff` – the `MigrationDiff` instance.
- **Returns**: The count of migrations needing attention.
- **Throws**: `ArgumentNullException` if `diff` is `null`.

### `HasMigrationsNeedingAttention`

```csharp
public static bool HasMigrationsNeedingAttention(this MigrationDiff diff)
```

Indicates whether any migration in the diff requires attention.

- **Parameters**: `diff` – the `MigrationDiff` instance.
- **Returns**: `true` if at least one migration needs attention; otherwise `false`.
- **Throws**: `ArgumentNullException` if `diff` is `null`.

### `GetCommonMigrationPercentage`

```csharp
public static double GetCommonMigrationPercentage(this MigrationDiff diff)
```

Calculates the percentage of migrations that are common (shared) between the two compared states. The value ranges from 0.0 to 100.0.

- **Parameters**: `diff` – the `MigrationDiff` instance.
- **Returns**: A double representing the percentage of common migrations.
- **Throws**: `ArgumentNullException` if `diff` is `null`. May throw `InvalidOperationException` if the diff does not contain sufficient data to compute the percentage (e.g., no migrations in either state).

### `GetAllSchemaChanges`

```csharp
public static List<SchemaChange> GetAllSchemaChanges(this MigrationDiff diff)
```

Retrieves a list of all schema changes recorded in the diff. Each `SchemaChange` describes an individual alteration (add, remove, modify) to database objects.

- **Parameters**: `diff` – the `MigrationDiff` instance.
- **Returns**: A `List<SchemaChange>` containing all schema changes. Returns an empty list if there are no changes.
- **Throws**: `ArgumentNullException` if `diff` is `null`.

### `GetMostRecentMigrationTimestamp`

```csharp
public static string? GetMostRecentMigrationTimestamp(this MigrationDiff diff)
```

Returns the timestamp of the most recent migration in the diff, formatted as a string. Returns `null` if no migration exists.

- **Parameters**: `diff` – the `MigrationDiff` instance.
- **Returns**: A string representing the timestamp, or `null`.
- **Throws**: `ArgumentNullException` if `diff` is `null`.

### `HasDestructiveChanges`

```csharp
public static bool HasDestructiveChanges(this MigrationDiff diff)
```

Determines whether the diff contains any destructive schema changes (e.g., dropping tables, columns, or indexes) that could cause data loss.

- **Parameters**: `diff` – the `MigrationDiff` instance.
- **Returns**: `true` if at least one destructive change is present; otherwise `false`.
- **Throws**: `ArgumentNullException` if `diff` is `null`.

### `GetFormattedSummary`

```csharp
public static string GetFormattedSummary(this MigrationDiff diff)
```

Generates a human-readable summary of the migration diff, including counts of migrations, schema changes, and any flags for attention or destructiveness.

- **Parameters**: `diff` – the `MigrationDiff` instance.
- **Returns**: A formatted string summarizing the diff.
- **Throws**: `ArgumentNullException` if `diff` is `null`.

## Usage

The following examples demonstrate typical usage of `MigrationDiffExtensions` methods.

### Example 1: Quick health check

```csharp
using EfMigrationDiff;

// Assume 'diff' is obtained from a comparison operation
MigrationDiff diff = GetMigrationDiff();

if (diff.HasMigrationsNeedingAttention())
{
    Console.WriteLine($"Migrations needing attention: {diff.GetMigrationsNeedingAttention()}");
}

if (diff.HasDestructiveChanges())
{
    Console.WriteLine("WARNING: Destructive changes detected!");
}

Console.WriteLine(diff.GetFormattedSummary());
```

### Example 2: Detailed analysis

```csharp
using EfMigrationDiff;

MigrationDiff diff = GetMigrationDiff();

int total = diff.GetTotalMigrations();
double commonPct = diff.GetCommonMigrationPercentage();
string? latestTimestamp = diff.GetMostRecentMigrationTimestamp();
List<SchemaChange> changes = diff.GetAllSchemaChanges();

Console.WriteLine($"Total migrations: {total}");
Console.WriteLine($"Common migration percentage: {commonPct:F1}%");
Console.WriteLine($"Latest migration: {latestTimestamp ?? "none"}");
Console.WriteLine($"Schema changes: {changes.Count}");

foreach (var change in changes)
{
    Console.WriteLine($"  - {change.Kind}: {change.ObjectName}");
}
```

## Notes

- All methods are static and operate on a `MigrationDiff` instance. They are thread-safe as long as the `MigrationDiff` object is not mutated concurrently. The methods themselves do not modify the diff.
- If the `diff` argument is `null`, every method throws `ArgumentNullException`.
- `GetCommonMigrationPercentage` may throw `InvalidOperationException` when the diff lacks data from both sides of the comparison (e.g., when one side has zero migrations). Always ensure the diff is fully populated before calling this method.
- `GetAllSchemaChanges` returns a new `List<SchemaChange>` each time it is called. The caller owns the list and may safely modify it.
- `GetMostRecentMigrationTimestamp` returns `null` only when the diff contains no migrations. An empty diff is valid; other methods like `GetTotalMigrations` will return 0 in that case.
- The `HasDestructiveChanges` flag is based on the diff’s internal classification of schema changes. It does not guarantee that the changes are actually applied; it only reflects the diff content.
