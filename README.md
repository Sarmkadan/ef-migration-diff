

# Getting Started

This section explains how to quickly start comparing Entity Framework Core migrations between branches using the `ef-migration-diff` tool.

## Installation

The tool is distributed as a .NET 10 console application. You can add it as a NuGet package or install the dotnet tool:

```bash
# Option 1: Add as project reference (recommended for customization)
dotnet add package Zaiets.ef.migration.diff

# Option 2: Install as global dotnet tool
dotnet tool install --global Zaiets.ef.migration.diff
```

## Quick Start Example

The following example demonstrates the most common workflow: parse two sets of EF migrations from disk, compare them between two branches, and print a human-readable diff report.

```csharp
#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using EfMigrationDiff.Repositories;
using Microsoft.Extensions.DependencyInjection;

// 1. Setup dependency injection
var services = new ServiceCollection();
services.AddScoped<GitRepository>();
services.AddScoped<MigrationRepository>();
services.AddScoped<MigrationDiffService>();
services.AddScoped<SchemaChangeDetectorService>();
services.AddScoped<ConflictDetectionService>();

var provider = services.BuildServiceProvider();
var diffService = provider.GetRequiredService<MigrationDiffService>();
var parser = provider.GetRequiredService<MigrationParserService>();
var formatter = new EfMigrationDiff.Formatters.MarkdownFormatter();

// 2. Parse migrations from both branches
// In a real scenario, you'd load these from git branches via LibGit2Sharp
var sourceBranchPath = Path.Combine(Directory.GetCurrentDirectory(), "migrations/source");
var targetBranchPath = Path.Combine(Directory.GetCurrentDirectory(), "migrations/target");

var sourceMigrations = parser.LoadMigrationsFromDirectoryAsync(sourceBranchPath, "MyDbContext").GetAwaiter().GetResult();
var targetMigrations = parser.LoadMigrationsFromDirectoryAsync(targetBranchPath, "MyDbContext").GetAwaiter().GetResult();

// 3. Build BranchInfo objects for each branch
// (Typically you'd get these from git, but for this example we construct them manually)
var sourceBranch = new EfMigrationDiff.Models.BranchInfo("feature/my-feature", "abc123def");
sourceBranch.AddMigration("20240101000000_CreateUsersTable");
sourceBranch.AddMigration("20240102000000_AddEmailColumn");

// Set migration IDs so the repository can look them up
var sourceRepo = provider.GetRequiredService<MigrationRepository>();
foreach (var migration in sourceMigrations)
{
    sourceRepo.Add(migration);
}

var targetBranch = new EfMigrationDiff.Models.BranchInfo("main", "def456ghi");
targetBranch.AddMigration("20240101000000_CreateUsersTable");

// Set migration IDs for target migrations
var targetRepo = new MigrationRepository();
foreach (var migration in targetMigrations)
{
    targetRepo.Add(migration);
}

// Replace the repo in the diff service with our target repo
// (In a real app, you'd configure this via DI or the GitRepository)
// For this demo, we'll use the diff service's Compare method directly

// 4. Compare the branches
var diff = diffService.CompareBranches(sourceBranch, targetBranch);

// 5. Generate and print the report
var report = diffService.GenerateReport(diff);
Console.WriteLine(report);
```

### Expected Console Output

Running the example above (with migrations that have actual schema changes) produces output similar to:

```
=== Migration Diff Report ===
Result: Different

Source Only Migrations: 1
  - 20240102000000_AddEmailColumn

Target Only Migrations: 0

Common Migrations: 1

Schema Changes: 2
Destructive Changes: 0

=== Schema Changes ===
### AddColumn
| Table | Column | Migration | SQL Preview | Destructive |
|-------|--------|-----------|-------------|-------------|
| [Users] | [Email] | `20240102...` | `AddColumn(...)` | ❌ No

### Migrations Only in Source Branch
| Migration ID | Description |
|-------------|-------------|
| `20240102...` | AddEmailColumn

=== Migration Diff Report ===
Result: Different

Source Only Migrations: 1
  - 20240102000000_AddEmailColumn

Target Only Migrations: 0

Common Migrations: 1

Schema Changes: 2
Destructive Changes: 0
```

## Advanced: Three-Way Diff with Common Ancestor

For comparing two branches against a common base, use the `CompareDbContextMigrations` method with `SchemaDiffOptions.ForMerge`:

```csharp
var options = EfMigrationDiff.Configuration.SchemaDiffOptions.ForMerge(
    baseBranch: "development",
    source: "feature/my-feature",
    target: "main"
);

var threeWayDiff = diffService.CompareDbContextMigrations(
    sourceBranch: sourceBranch,
    targetBranch: targetBranch,
    dbContextName: "MyDbContext"
);

// Render the three-way diff as HTML
var htmlRenderer = new EfMigrationDiff.Formatters.VisualDiffFormatter();
var htmlReport = htmlRenderer.RenderSideBySide(
    new EfMigrationDiff.Models.SchemaDiffResult
    {
        Id = Guid.NewGuid(),
        SourceLabel = "feature/my-feature",
        TargetLabel = "main",
        BaseLabel = "development",
        SourceOnlyChanges = threeWayDiff.SourceOnlySchemaChanges,
        TargetOnlyChanges = threeWayDiff.TargetOnlySchemaChanges,
        ModifiedChanges = threeWayDiff.ModifiedSchemaChanges
    }
);

File.WriteAllText("diff-report.html", htmlReport);
Console.WriteLine("Three-way diff written to diff-report.html");
```

## Key Types and APIs

| Type | Purpose |
|------|---------|
| `Migration` | Represents a single EF migration with `Id`, `Name`, `DbContextName`, and `SchemaChanges` |
| `SchemaChange` | Represents a schema operation (`CreateTable`, `AddColumn`, `DropColumn`, etc.) with `TableName`, `ColumnName`, `ChangeType`, and `Sql` |
| `MigrationDiff` | Result of comparing two branches: contains `OnlyInSource`, `OnlyInTarget`, `InBoth`, `SourceSchemaChanges`, `TargetSchemaChanges`, `Conflicts` |
| `MigrationDiffService.CompareBranches(BranchInfo, BranchInfo)` | Compares two branches and returns a `MigrationDiff` |
| `MigrationParserService.LoadMigrationsFromDirectoryAsync(string, string)` | Parses all `.cs` migration files from a directory for a given `DbContext` |
| `MarkdownFormatter.GenerateMarkdownReport(MigrationDiff)` | Generates a human-readable Markdown report |
| `VisualDiffFormatter.RenderSideBySide(SchemaDiffResult)` | Generates an HTML side-by-side visual diff |
| `SchemaDiffOptions.ForMerge(string, string, string)` | Configures three-way diff labels (base, source, target) |

## Next Steps

- Run `dotnet tool install --global Zaiets.ef.migration.diff && ef-migration-diff --help` to see the CLI command-line tool
- Explore the `examples/` directory for more usage patterns
- Use `dotnet format` and `dotnet build --no-restore /p:TreatWarningsAsErrors=true` to verify code quality

## MarkdownFormatter

The `MarkdownFormatter` class is used to generate detailed, human-readable migration diff reports in Markdown format. It processes a `MigrationDiff` object to produce comprehensive tables summarizing migrations, conflicts, schema changes, and recommendations for branch management.

Example usage:
```csharp
using EfMigrationDiff.Formatters;
using EfMigrationDiff.Models;

// Assuming 'diff' is an existing MigrationDiff object
var formatter = new MarkdownFormatter(includeDestructiveWarnings: true);

// Generate the report as a string
string report = formatter.GenerateMarkdownReport(diff);

// Alternatively, write directly to a file
formatter.WriteToFile("migration-diff-report.md", diff);
```

## BreakingChangeDetector

The `BreakingChangeDetector` service analyses schema changes to identify breaking changes that could affect downstream applications. It classifies individual changes, provides a collection of classifications, and produces a summary of the overall diff.

Example usage:
```csharp
using EfMigrationDiff.Services;
using EfMigrationDiff.Models;

// Create the detector (parameter‑less constructor)
var detector = new BreakingChangeDetector();

// Classify a collection of schema changes (returns a list of classifications)
var classifications = detector.ClassifyChanges();

// Classify a single schema change (example with a default‑constructed SchemaChange)
var singleClassification = detector.ClassifyChange(new SchemaChange());

// Get a summary of the breaking‑change analysis for the whole diff
var summary = detector.ClassifyDiffResult();
```

## BreakingChangeDetectorTests

The `BreakingChangeDetectorTests` class contains tests for the breaking change detector service. These tests verify how schema changes are classified: dropping columns, tables, indexes, foreign keys, or stored procedures is reported as breaking, while adding nullable columns or widening data types is considered safe. They also cover warning-level classifications such as renames and newly added foreign keys, batch processing of multiple changes, and the correctness of the overall diff result counts and safety summary.

Example usage:
```csharp
using EfMigrationDiff.Tests;

// Create the test class instance
var tests = new BreakingChangeDetectorTests();

// Run the individual classification tests
tests.ClassifyChange_DropColumn_IsBreaking();
tests.ClassifyChange_DropTable_IsBreaking();
tests.ClassifyChange_AddNullableColumn_IsSafe();
tests.ClassifyChange_ModifyColumn_NarrowingIntToSmallint_IsBreaking();
tests.ClassifyChange_Rename_IsWarning();
tests.ClassifyChanges_ProcessesMultipleChanges();
tests.ClassifyDiffResult_CalculatesCorrectCounts();
tests.ClassifyDiffResult_IsSafe_WhenNoBreakingOrWarnings();
```

## SchemaChangeDetectorServiceTests

The `SchemaChangeDetectorServiceTests` class contains tests for the schema change detector service. These tests verify that creating and dropping tables, columns, indexes, and foreign keys are detected correctly, including table renames and raw SQL statements. They also confirm that the detector reports the affected tables as a distinct list.

Example usage:
```csharp
using EfMigrationDiff.Tests;

// Create the test class instance
var tests = new SchemaChangeDetectorServiceTests();

// Run the individual detection tests
tests.Detect_Create_And_Drop_Table();
tests.Detect_Add_And_Drop_Column_With_Metadata();
tests.Detect_Create_And_Drop_Index();
tests.Detect_Rename_Table();
tests.Detect_Add_And_Drop_ForeignKey();
tests.Detect_Raw_Sql_Create_Table();
tests.Get_Affected_Tables_Returns_Distinct_List();
```

## ConflictResolutionEngineTests

The `ConflictResolutionEngineTests` class contains tests for the conflict resolution engine. These tests verify that the engine initializes with default strategies and maps each conflict type (table, column, index, constraint, operation, dependency, and name) to the appropriate resolution strategy with the expected severity, including critical severity for column conflicts and high or medium severity for blocking and non-blocking conflicts. They also confirm that batch resolution produces complete reports, that custom strategies can be registered to override the defaults, and that every resolution includes recommendations.

Example usage:
```csharp
using EfMigrationDiff.Tests;

// Create the test class instance
var tests = new ConflictResolutionEngineTests();

// Run the individual resolution tests
tests.ConflictResolutionEngine_InitializesWithDefaultStrategies();
tests.ResolveConflict_WithTableConflict_ReturnsManualResolutionStrategy();
tests.ResolveConflict_WithColumnConflict_ReturnsReviewResolutionStrategyWithCriticalSeverity();
tests.ResolveConflict_WithIndexConflict_ReturnsAutomaticResolutionStrategy();
tests.ResolveConflict_WithConstraintConflict_ReturnsReviewResolutionStrategy();
tests.ResolveConflict_WithOperationConflict_ReturnsManualResolutionStrategy();
tests.ResolveConflict_WithDependencyConflict_ReturnsManualResolutionStrategy();
tests.ResolveConflict_WithNameConflict_ReturnsManualResolutionStrategy();
tests.ResolveConflict_WithUnknownConflictType_ReturnsDefaultManualStrategy();
tests.ResolveConflict_ColumnConflictAlwaysHasCriticalSeverity();
tests.ResolveConflict_WithBlockingConflict_ReturnsHighSeverity();
tests.ResolveConflict_WithNonBlockingConflict_ReturnsMediumSeverity();
tests.ResolveBatch_WithMultipleConflicts_ReturnsCompleteReport();
tests.ResolveBatch_WithEmptyList_ReturnsEmptyReport();
tests.ResolveBatch_CanProceedWithoutManualIntervention_ReturnsCorrectValue();
tests.RegisterStrategy_WithCustomStrategy_OverridesDefaultStrategy();
tests.ResolveConflict_AllConflictTypes_HaveResolutionStrategies();
tests.ResolveConflict_RecommendationsGeneratedForEachConflictType();
tests.ConflictResolution_AllPropertiesSetCorrectly();
```
