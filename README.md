

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
| `SqlGenerator` | Generates SQL scripts from schema changes (see "Generating SQL" section below) |

## Generating SQL

The `SqlGenerator` service converts a list of `SchemaChange` objects into executable SQL scripts. It supports generating forward migration scripts, rollback scripts, and validation.

### Basic Usage

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EfMigrationDiff.Configuration;

var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
services.Configure<EfMigrationDiffOptions>(opts => { });
services.AddSingleton<SqlGenerator>();

var provider = services.BuildServiceProvider();
var generator = provider.GetRequiredService<SqlGenerator>();

// Create sample schema changes (typically from MigrationDiff.SourceSchemaChanges or TargetSchemaChanges)
var changes = new List<SchemaChange>
{
    new SchemaChange
    {
        Id = Guid.NewGuid().ToString(),
        MigrationId = "20240101000000_CreateUsersTable",
        ChangeType = SqlChangeType.CreateTable,
        TableName = "Users",
        Sql = "[Id] INT IDENTITY(1,1) NOT NULL,\n    [Email] NVARCHAR(256) NOT NULL,\n    [Name] NVARCHAR(100) NOT NULL,\n    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),\n    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)",
        LineNumber = 10
    },
    new SchemaChange
    {
        Id = Guid.NewGuid().ToString(),
        MigrationId = "20240102000000_AddEmailIndex",
        ChangeType = SqlChangeType.CreateIndex,
        TableName = "Users",
        Sql = "[IX_Users_Email] ON [Users] ([Email])",
        LineNumber = 15
    },
    new SchemaChange
    {
        Id = Guid.NewGuid().ToString(),
        MigrationId = "20240103000000_AddPhoneColumn",
        ChangeType = SqlChangeType.AddColumn,
        TableName = "Users",
        ColumnName = "PhoneNumber",
        NewValue = "NVARCHAR(20)",
        DefaultValue = "NULL",
        LineNumber = 20
    },
    new SchemaChange
    {
        Id = Guid.NewGuid().ToString(),
        MigrationId = "20240104000000_RenameEmailColumn",
        ChangeType = SqlChangeType.Rename,
        TableName = "Users",
        OldValue = "Email",
        NewValue = "EmailAddress",
        LineNumber = 25
    },
    new SchemaChange
    {
        Id = Guid.NewGuid().ToString(),
        MigrationId = "20240105000000_DropPhoneColumn",
        ChangeType = SqlChangeType.DropColumn,
        TableName = "Users",
        ColumnName = "PhoneNumber",
        LineNumber = 30
    }
};

// Generate forward migration script (wrapped in transaction)
var script = generator.GenerateScript(changes, includeTransaction: true);
Console.WriteLine(script);

// Generate rollback script (reverses changes in reverse order)
var rollback = generator.GenerateRollback(changes);
Console.WriteLine(rollback);

// Validate changes before generating
var errors = generator.ValidateChanges(changes);
if (errors.Count == 0)
{
    Console.WriteLine("All changes are valid!");
}
else
{
    foreach (var error in errors)
    {
        Console.WriteLine($"ERROR: {error}");
    }
}

generator.Dispose();
```

### Sample Output

**Forward Script (`GenerateScript`):**
```sql
BEGIN TRANSACTION;

-- CreateTable: Users
CREATE TABLE [Users] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Email] NVARCHAR(256) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- CreateIndex: Users
CREATE INDEX [IX_Users_Email] ON [Users] ([Email]);

-- AddColumn: Users
ALTER TABLE [Users] ADD [PhoneNumber] NVARCHAR(20) DEFAULT NULL;

-- Rename: Users
EXEC sp_rename 'Email', 'EmailAddress';

-- DropColumn: Users
ALTER TABLE [Users] DROP COLUMN [PhoneNumber];

COMMIT;
```

**Rollback Script (`GenerateRollback`):**
```sql
BEGIN TRANSACTION;

-- AddColumn: Users
ALTER TABLE [Users] ADD [PhoneNumber] NVARCHAR(MAX);

-- Rename: Users
EXEC sp_rename 'EmailAddress', 'Email';

-- DropColumn: Users
ALTER TABLE [Users] DROP COLUMN [PhoneNumber];

-- DropIndex: Users
DROP INDEX [IX_Users_Email] ON [Users] ([Email]);

-- DropTable: Users
DROP TABLE [Users];

COMMIT;
```

### Common Scenarios

| Scenario | One-liner Recipe |
|----------|------------------|
| **Compare two migration folders** | `var diff = diffService.CompareBranches(sourceBranch, targetBranch); var sql = generator.GenerateScript(diff.SourceSchemaChanges);` |
| **Diff a snapshot against a live model** | `var changes = parser.LoadMigrationsFromDirectoryAsync(snapshotPath, "MyDbContext").Result.SelectMany(m => m.SchemaChanges); var sql = generator.GenerateScript(changes);` |
| **Dry-run output for CI** | `var errors = generator.ValidateChanges(diff.SourceSchemaChanges); if (errors.Count > 0) { Console.Error.WriteLine(string.Join("\n", errors)); Environment.Exit(1); }` |

## Compare Command

`ef-migration-diff compare [source] [target]` (alias `diff`) reads the migrations committed on two branches, detects schema changes and conflicts between them, and writes a report. It is implemented in `CLI/Commands/CompareCommand.cs`.

What it does:

1. Opens the git repository in the current directory and resolves both branches. Unknown branches fail with exit code `64`.
2. Parses the `.cs` migration files under `MigrationsPath` on each branch's tip. `*.Designer.cs` files are skipped.
3. Compares the two sets, reports the number of conflicts, schema changes and table, column and index statistics, and writes a report to `OutputPath` (the default is `./reports`).

Arguments and options:

| Argument / option | Default | Description |
|-------------------|---------|-------------|
| `[source]` | `SourceBranch` from configuration (`develop`) | Branch to compare from. |
| `[target]` | `TargetBranch` from configuration (`main`) | Branch to compare against. |
| `--format <fmt>`, `-f <fmt>` | `text` | Report format: `text`, `json` or `html`. Any other value produces the text report. Ignored with `--summary`. |
| `--dot <path>` | none | Also writes the migration dependency graph as a Graphviz DOT file. The path must be relative and stay inside the repository directory. |
| `--summary` | off | Prints the counts only and does not write a report file. |

Notes:

- The report file name is `migration-comparison-<UTC timestamp>.<ext>`, where the extension follows the format (`.txt`, `.json` or `.html`).
- With `--format json`, the report is written to stdout so it can be piped. Status lines go to stderr.
- The exit code follows the contract in [CI Gating with Exit Codes](#ci-gating-with-exit-codes).

Sample invocations:

```bash
# Compare develop against main and write a text report to ./reports
ef-migration-diff compare develop main

# Print the JSON report to stdout and capture it
ef-migration-diff compare develop main --format json > migration-diff.json

# Print counts only, with no report file
ef-migration-diff compare feature/orders main --summary

# Write an HTML report and a DOT dependency graph
ef-migration-diff compare develop main --format html --dot graphs/migrations.dot
```

## CI Gating with Exit Codes

`ef-migration-diff compare <source> <target>` (alias `diff`) exits with a code that describes the result, so a pipeline can fail on breaking changes without parsing output:

| Exit code | Meaning |
|-----------|---------|
| `0` | No schema changes and no conflicts between the branches |
| `1` | Non-breaking changes only (for example, new tables or nullable columns) |
| `2` | At least one breaking schema change (for example, a dropped table, column, index or foreign key, or a non-nullable column added without a default) |
| `3` | At least one blocking conflict between the branches' changes |
| `64` | Usage error: invalid arguments or options, unknown branch, or invalid configuration |
| `70` | Unexpected internal error, including a failure while reading or analysing migrations |

When several conditions hold, the most severe code wins: conflicts (`3`), then breaking changes (`2`), then non-breaking changes (`1`). Only schema changes introduced by migrations that exist on one branch but not the other are classified. Shared history is ignored.

Treat `0` and `1` as success and fail on anything above `1`. That covers breaking changes, conflicts and the error codes, so a broken invocation cannot pass the gate:

```bash
ef-migration-diff compare develop main --summary
status=$?
if [ "$status" -gt 1 ]; then
  echo "Migration gate failed: exit code $status" >&2
  exit 1
fi
```

> **Upgrading from earlier versions:** `compare` used to exit with `1` for blocking conflicts and `2` for configuration errors. Under this contract `1` means non-breaking changes, conflicts exit with `3`, and configuration errors exit with `64`. Update any pipeline that matched the old values.

## Validate Command

`ef-migration-diff validate` (alias `check`) checks the structure of the migration files in the current repository. It does not read git history and does not compare branches, so it works on a single checkout.

What it does:

1. Resolves the migrations folder as `MigrationsPath` (default `Migrations`) under the current directory. If the folder does not exist, the command stops with a configuration error and exit code `64`.
2. Takes each `*.cs` file directly in that folder and skips `*.Designer.cs` files. Subfolders are not searched.
3. Runs these structural checks on each file:
   - the file is not empty
   - the file contains a `public partial class` declaration, and exactly one class declaration of that form
   - the file contains `Up(` and `Down(` methods
   - the file name follows `YYYYMMDDHHmmss_MigrationName.cs`
4. Prints `✓ <file>` for each file that passes, or `✗ <file>` followed by one indented `-` line per problem for each file that fails.
5. Prints a summary line, `Validation Summary: <valid> valid, <invalid> invalid`.

The checks are text heuristics over the source, not a compiler pass. A file that compiles can still fail them, and a file that fails them is not necessarily broken for EF Core.

Arguments and options:

The command takes no arguments or options. Anything after `validate` is ignored. The folder it checks comes from configuration: set `MigrationsPath` in `efmigrationdiff.json` to use a different folder.

Exit codes:

| Exit code | Meaning |
|-----------|---------|
| `0` | Every migration file passed, or the folder contains no migration files |
| `1` | At least one migration file failed a check, or an unexpected error occurred |
| `64` | The migrations folder does not exist or the configuration is invalid |

Sample invocations:

```bash
# Run from the repository root, where Migrations/ lives
ef-migration-diff validate

# Same command under its alias
ef-migration-diff check

# Use the exit code as a pipeline gate
ef-migration-diff validate || exit 1
```

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
