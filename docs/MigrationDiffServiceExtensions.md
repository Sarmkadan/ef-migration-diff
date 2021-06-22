# MigrationDiffServiceExtensions

`MigrationDiffServiceExtensions` provides static convenience methods for comparing Entity Framework Core migration histories and generating human-readable reports. It is designed to help developers quickly assess whether two migration sequences diverge, contain destructive operations, or can be merged without manual intervention.

## API

### GenerateQuickReport

```csharp
public static string GenerateQuickReport(
    this IMigrationDiffService service,
    string sourceMigration,
    string targetMigration)
```

Produces a concise textual summary of the differences between two migration points. The report includes the number of pending migrations, any destructive change warnings, and a merge-safety verdict.

**Parameters**
- `service` *(this IMigrationDiffService)* — the diff service instance being extended.
- `sourceMigration` *(string)* — the name or identifier of the baseline migration.
- `targetMigration` *(string)* — the name or identifier of the migration to compare against.

**Return value**
A `string` containing the formatted quick report.

**Exceptions**
- `ArgumentNullException` — if `sourceMigration` or `targetMigration` is null.
- `ArgumentException` — if either migration name is empty or whitespace.
- `InvalidOperationException` — if the underlying service cannot resolve one or both migration names in the configured history.

---

### HasDestructiveChanges

```csharp
public static bool HasDestructiveChanges(
    this IMigrationDiffService service,
    string sourceMigration,
    string targetMigration)
```

Determines whether any migration between `sourceMigration` and `targetMigration` contains destructive operations (e.g. column drops, table drops, data-loss altering).

**Parameters**
- `service` *(this IMigrationDiffService)* — the diff service instance.
- `sourceMigration` *(string)* — the starting migration name.
- `targetMigration` *(string)* — the ending migration name.

**Return value**
`true` if at least one destructive change is detected; otherwise `false`.

**Exceptions**
- `ArgumentNullException` — if either parameter is null.
- `ArgumentException` — if either parameter is empty or whitespace.
- `InvalidOperationException` — if migration resolution fails.

---

### GetCommonMigrationNames

```csharp
public static List<string> GetCommonMigrationNames(
    this IMigrationDiffService service,
    IEnumerable<string> firstBranch,
    IEnumerable<string> secondBranch)
```

Returns the set of migration names that appear in both branches, preserving the order from the first branch. Useful for identifying a shared ancestor when comparing divergent migration histories.

**Parameters**
- `service` *(this IMigrationDiffService)* — the diff service instance.
- `firstBranch` *(IEnumerable<string>)* — the migration name sequence from one branch.
- `secondBranch` *(IEnumerable<string>)* — the migration name sequence from another branch.

**Return value**
A `List<string>` of migration names common to both sequences. Returns an empty list if there is no overlap.

**Exceptions**
- `ArgumentNullException` — if either branch collection is null.
- No exception is thrown for empty collections; an empty list is returned.

---

### GenerateConflictReport

```csharp
public static string GenerateConflictReport(
    this IMigrationDiffService service,
    IEnumerable<string> localMigrations,
    IEnumerable<string> remoteMigrations)
```

Generates a detailed conflict report when two migration branches have diverged. The report lists conflicting migration names, identifies the last common migration, and describes the nature of each conflict (e.g. same name but different content, or ordering discrepancies).

**Parameters**
- `service` *(this IMigrationDiffService)* — the diff service instance.
- `localMigrations` *(IEnumerable<string>)* — the local branch migration names.
- `remoteMigrations` *(IEnumerable<string>)* — the remote branch migration names.

**Return value**
A `string` containing the formatted conflict report. If no conflicts are found, the report indicates a clean merge state.

**Exceptions**
- `ArgumentNullException` — if either collection is null.
- `InvalidOperationException` — if the service cannot load migration metadata for one or more names.

---

### CanMergeSafely

```csharp
public static bool CanMergeSafely(
    this IMigrationDiffService service,
    IEnumerable<string> localMigrations,
    IEnumerable<string> remoteMigrations)
```

Evaluates whether two migration branches can be merged without manual resolution. A safe merge requires a common ancestor, no conflicting migration names with different bodies, and no destructive changes in either branch relative to the ancestor.

**Parameters**
- `service` *(this IMigrationDiffService)* — the diff service instance.
- `localMigrations` *(IEnumerable<string>)* — the local branch migration names.
- `remoteMigrations` *(IEnumerable<string>)* — the remote branch migration names.

**Return value**
`true` if the branches can be merged automatically; `false` if manual intervention is required.

**Exceptions**
- `ArgumentNullException` — if either collection is null.
- `InvalidOperationException` — if migration metadata cannot be resolved.

## Usage

### Example 1: Checking for destructive changes before deployment

```csharp
var diffService = serviceProvider.GetRequiredService<IMigrationDiffService>();

string deployedMigration = "20240115_AddCustomerTable";
string pendingMigration = "20240201_AlterCustomerEmailColumn";

if (diffService.HasDestructiveChanges(deployedMigration, pendingMigration))
{
    Console.WriteLine("WARNING: Destructive changes detected. Review before deploying.");
    string report = diffService.GenerateQuickReport(deployedMigration, pendingMigration);
    Console.WriteLine(report);
}
else
{
    Console.WriteLine("No destructive changes. Safe to proceed.");
}
```

### Example 2: Validating merge safety between feature branches

```csharp
var diffService = serviceProvider.GetRequiredService<IMigrationDiffService>();

var mainBranchMigrations = new List<string>
{
    "20240101_InitialCreate",
    "20240115_AddCustomerTable",
    "20240201_AddOrderTable"
};

var featureBranchMigrations = new List<string>
{
    "20240101_InitialCreate",
    "20240115_AddCustomerTable",
    "20240210_AddProductCatalog"
};

if (diffService.CanMergeSafely(mainBranchMigrations, featureBranchMigrations))
{
    Console.WriteLine("Branches can be merged safely.");
}
else
{
    string conflictReport = diffService.GenerateConflictReport(
        mainBranchMigrations, featureBranchMigrations);
    Console.WriteLine("Merge conflicts detected:");
    Console.WriteLine(conflictReport);

    var common = diffService.GetCommonMigrationNames(
        mainBranchMigrations, featureBranchMigrations);
    Console.WriteLine($"Last common migration: {common.LastOrDefault()}");
}
```

## Notes

- All methods are extension methods on `IMigrationDiffService` and require a properly configured service instance. The underlying service is assumed to have access to the full migration metadata (model snapshots, up/down operations) for accurate comparison.
- `GetCommonMigrationNames` performs a simple name-based intersection. It does not verify that the migration bodies are identical; two migrations with the same name but different content will still appear in the result. Use `GenerateConflictReport` to detect such semantic conflicts.
- `CanMergeSafely` and `GenerateConflictReport` both require ordered sequences that reflect the actual migration application order. Passing unordered or incomplete sequences may produce misleading results.
- These methods are **not** guaranteed to be thread-safe. The underlying `IMigrationDiffService` implementation may hold mutable state (e.g. cached migration metadata). Callers should synchronize access if the service instance is shared across threads, or ensure each thread uses its own scoped instance.
- `HasDestructiveChanges` inspects the operations between two specific migration points. It does not consider changes that are purely additive (new tables, new columns without data loss). The definition of "destructive" depends on the service implementation and typically includes `DropTable`, `DropColumn`, and `AlterColumn` operations that narrow data types.
- When `sourceMigration` and `targetMigration` are the same, `HasDestructiveChanges` returns `false` and `GenerateQuickReport` produces a report indicating zero pending changes.
