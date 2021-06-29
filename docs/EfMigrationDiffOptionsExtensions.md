# EfMigrationDiffOptionsExtensions

The `EfMigrationDiffOptionsExtensions` static class provides a set of fluent extension methods for the `EfMigrationDiffOptions` type. These methods allow callers to configure an `EfMigrationDiffOptions` instance in a declarative, chainable manner, enabling common setup tasks such as ensuring that referenced paths exist, specifying the output report format, selecting the DbContext types to compare, and defining the source and target branches for the migration diff operation.

## API

### `EnsurePathsExist`

```csharp
public static EfMigrationDiffOptions EnsurePathsExist(this EfMigrationDiffOptions options)
```

**Purpose:**  
Validates that all file or directory paths currently configured in the `options` instance exist on the file system. If any path does not exist, the method throws an appropriate exception.

**Parameters:**  
- `options` (`EfMigrationDiffOptions`): The options instance to validate.

**Returns:**  
The same `EfMigrationDiffOptions` instance, enabling method chaining.

**Throws:**  
- `ArgumentNullException` if `options` is `null`.  
- `DirectoryNotFoundException` or `FileNotFoundException` if a configured path does not exist.

---

### `WithReportFormat`

```csharp
public static EfMigrationDiffOptions WithReportFormat(this EfMigrationDiffOptions options, ReportFormat format)
```

**Purpose:**  
Sets the output format for the migration diff report. The `format` parameter determines whether the report is generated as plain text, JSON, HTML, or another supported format.

**Parameters:**  
- `options` (`EfMigrationDiffOptions`): The options instance to configure.  
- `format` (`ReportFormat`): The desired report format.

**Returns:**  
The same `EfMigrationDiffOptions` instance, enabling method chaining.

**Throws:**  
- `ArgumentNullException` if `options` is `null`.  
- `ArgumentException` if `format` is not a valid `ReportFormat` value.

---

### `WithDbContexts`

```csharp
public static EfMigrationDiffOptions WithDbContexts(this EfMigrationDiffOptions options, params Type[] dbContextTypes)
```

**Purpose:**  
Specifies the DbContext types to include in the migration diff analysis. Only the migrations associated with the provided DbContext types will be compared.

**Parameters:**  
- `options` (`EfMigrationDiffOptions`): The options instance to configure.  
- `dbContextTypes` (`params Type[]`): One or more `Type` objects representing the DbContext classes to analyze.

**Returns:**  
The same `EfMigrationDiffOptions` instance, enabling method chaining.

**Throws:**  
- `ArgumentNullException` if `options` is `null`.  
- `ArgumentException` if any element in `dbContextTypes` is `null` or does not derive from `DbContext`.

---

### `WithBranches`

```csharp
public static EfMigrationDiffOptions WithBranches(this EfMigrationDiffOptions options, string sourceBranch, string targetBranch)
```

**Purpose:**  
Sets the source and target Git branches whose migration snapshots will be compared. The diff is computed between the migration state of the source branch and that of the target branch.

**Parameters:**  
- `options` (`EfMigrationDiffOptions`): The options instance to configure.  
- `sourceBranch` (`string`): The name of the source branch (e.g., `"main"`).  
- `targetBranch` (`string`): The name of the target branch (e.g., `"feature/new-migration"`).

**Returns:**  
The same `EfMigrationDiffOptions` instance, enabling method chaining.

**Throws:**  
- `ArgumentNullException` if `options` is `null`.  
- `ArgumentException` if `sourceBranch` or `targetBranch` is `null` or empty.

## Usage

### Example 1: Basic configuration with path validation and report format

```csharp
using EfMigrationDiff;

var options = new EfMigrationDiffOptions()
    .EnsurePathsExist()
    .WithReportFormat(ReportFormat.Json)
    .WithDbContexts(typeof(MyDbContext))
    .WithBranches("main", "feature/add-table");

// The options instance is now fully configured and ready to be passed to the diff engine.
```

### Example 2: Configuring multiple DbContexts and using a custom report format

```csharp
using EfMigrationDiff;

var options = new EfMigrationDiffOptions()
    .WithDbContexts(typeof(OrdersDbContext), typeof(InventoryDbContext))
    .WithBranches("release/2.0", "release/2.1")
    .WithReportFormat(ReportFormat.Html)
    .EnsurePathsExist();

// The order of calls is flexible; EnsurePathsExist is called last to validate all paths after configuration.
```

## Notes

- **Edge cases:**  
  - `EnsurePathsExist` performs validation only at the time it is called. If paths are modified after this method is invoked, the validation is not repeated. It is recommended to call `EnsurePathsExist` as the final step in the fluent chain.  
  - `WithDbContexts` accepts a `params` array; passing an empty array will result in no DbContexts being configured, which may cause the diff operation to fail later. Always provide at least one valid DbContext type.  
  - `WithBranches` does not verify that the specified branches actually exist in the repository; that check is deferred to the diff execution phase.

- **Thread safety:**  
  The `EfMigrationDiffOptions` class is not thread-safe. Each extension method modifies the same instance, and concurrent calls from multiple threads may produce inconsistent state. If configuration must be performed from multiple threads, synchronize access to the options instance or create separate instances per thread.
