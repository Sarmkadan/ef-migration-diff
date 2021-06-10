# MigrationDiffService
The `MigrationDiffService` provides utilities for comparing Entity Framework Core migrations between different branches or DbContext configurations and generating human‑readable reports of the differences.

## API
### MigrationDiffService()
Creates a new instance of the service. The constructor has no parameters and does not throw under normal circumstances.

### CompareBranches
**Purpose:** Compares the migrations defined in two branches of a repository and returns a structured representation of their differences.  
**Parameters:**  
- `sourceBranch` – identifier (e.g., branch name, commit hash) of the branch to treat as the baseline.  
- `targetBranch` – identifier of the branch to compare against the baseline.  
- `repositoryPath` (optional) – file system path to the Git repository; if omitted the service attempts to locate the repository relative to the current working directory.  
**Return value:** A `MigrationDiff` object detailing added, removed, and altered migrations between the two branches.  
**Exceptions:**  
- `ArgumentNullException` if either `sourceBranch` or `targetBranch` is null.  
- `ArgumentException` if the identifiers do not resolve to valid commits/branches.  
- `InvalidOperationException` if the repository cannot be accessed or migration projects cannot be loaded.

### CompareDbContextMigrations
**Purpose:** Compares the migrations associated with two `DbContext` types and returns a `MigrationDiff` describing the differences.  
**Parameters:**  
- `sourceContext` – the first `DbContext` type to compare.  
- `targetContext` – the second `DbContext` type to compare.  
**Return value:** A `MigrationDiff` object representing the migration differences between the two contexts.  
**Exceptions:**  
- `ArgumentNullException` if either context parameter is null.  
- `InvalidOperationException` if the migrations for either context cannot be discovered or loaded.

### GenerateReport
**Purpose:** Produces a textual report from a `MigrationDiff` instance, suitable for display or logging.  
**Parameters:**  
- `diff` – the `MigrationDiff` to report on.  
- `format` (optional) – specifies the output format (e.g., Markdown, plain text); defaults to Markdown.  
**Return value:** A string containing the formatted report.  
**Exceptions:**  
- `ArgumentNullException` if `diff` is null.  
- `NotSupportedException` if an unsupported `format` value is supplied.

## Usage
```csharp
using EfMigrationDiff;

// Example 1: Comparing two Git branches
var service = new MigrationDiffService();
MigrationDiff diff = service.CompareBranches(
    sourceBranch: "main",
    targetBranch: "feature/add-auth",
    repositoryPath: @"C:\projects\MyApp");

string report = service.GenerateReport(diff);
Console.WriteLine(report);
```

```csharp
using EfMigrationDiff;
using Microsoft.EntityFrameworkCore;

// Example 2: Comparing migrations from two DbContexts
var service = new MigrationDiffService();
MigrationDiff diff = service.CompareDbContextMigrations(
    sourceContext: typeof(AppDbContext),
    targetContext: typeof(LegacyDbContext));

string markdown = service.GenerateReport(diff, format: ReportFormat.Markdown);
File.WriteAllText("migration-diff.md", markdown);
```

## Notes
- The service does not retain state between calls; each method operates solely on its input parameters. Consequently, instances are thread‑safe for concurrent use as long as the supplied arguments are not mutated by other threads during execution.  
- If a `MigrationDiffService` instance is wrapped with caching or repository‑lookup logic (not exposed in the public API), thread‑safety guarantees may differ; consult the implementation details of any derived or decorated types.  
- Supplying invalid branch identifiers or DbContext types will result in exceptions as described; callers should validate inputs or handle exceptions appropriately.  
- The `GenerateReport` method assumes the supplied `MigrationDiff` is consistent; passing a diff produced from incompatible sources may yield incomplete or misleading reports.  
- The service relies on the underlying Git provider and EF Core migration discovery mechanisms; interruptions such as missing repositories, inaccessible files, or EF Core configuration errors will surface as `InvalidOperationException`.
