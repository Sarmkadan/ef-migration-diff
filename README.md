// ... existing content ...

## ValidationMiddleware

The `ValidationMiddleware` class validates command context before execution, checking for required options, valid argument counts, and application configuration state. It allows registering validators for specific commands and short-circuits command execution if validation fails.

Here's an example of how to use the `ValidationMiddleware` class:

```csharp
var middleware = new ValidationMiddleware();
middleware.RegisterValidator("myCommand", new CommandValidator()
  .RequireMinArguments(2)
  .RequireOption("--option")
  .ValidateOptionValue("--option", errorMessage: "Option value is required")
);

var context = new CommandContext("myCommand", new[] { "arg1", "arg2" }, new Dictionary<string, object> { { "--option", "optionValue" } });
var result = await middleware.InvokeAsync(context);

// Use AddRule for custom validation
middleware.RegisterValidator("myCustomCommand", new CommandValidator()
  .AddRule(ctx => ctx.ParsedArguments.Count > 0 ? null : "At least one argument is required")
);
```

The `ValidationMiddleware` uses validators to perform the actual validation. Validators can be created using the `CommandValidator` class which provides methods like `RequireMinArguments`, `RequireOption`, `ValidateOptionValue`, and `AddRule` to define validation rules.

## CommandParser

`CommandParser` converts raw command‑line arguments into a `CommandContext`. It lets you register known options (short and long names, descriptions, and whether they are flags) and later retrieve the defined options for help generation.

```csharp
using EfMigrationDiff.CLI;
using Microsoft.Extensions.DependencyInjection;

// Create and configure the parser
var parser = new CommandParser()
  .RegisterOption("f", "force", "Force the operation", isFlag: true)
  .RegisterOption("o", "output", "Path to the output file");

// Parse a sample argument list
var context = parser.Parse(
  commandName: "migrate",
  args: new[] { "--force", "-o", "result.txt", "src/db" },
  serviceProvider: new ServiceCollection().BuildServiceProvider());

// Inspect parsed arguments and options
Console.WriteLine($"Positional arguments: {context.ParsedArguments.Count}");
foreach (var opt in context.ParsedOptions)
{
  Console.WriteLine($"{opt.Key} = {opt.Value}");
}

// List the registered option definitions (useful for help text)
foreach (var optDef in parser.GetRegisteredOptions())
{
  Console.WriteLine($"{optDef.ShortName}/--{optDef.LongName}: {optDef.Description} (Flag: {optDef.IsFlag})");
}
```

The parser's option definitions expose `ShortName`, `LongName`, `Description`, and `IsFlag` properties, allowing callers to generate user‑friendly documentation or perform additional validation.




## CommandContext

`CommandContext` represents the execution context for a CLI command, providing access to parsed arguments, options, services, and I/O streams. It serves as the central data structure passed through command execution pipelines, middleware, and validators, enabling consistent argument handling and dependency injection across the application.

Here's a realistic example of creating and using a `CommandContext`:

```csharp
using EfMigrationDiff.CLI;
using Microsoft.Extensions.DependencyInjection;
using System;

// Create a service provider with required services
var services = new ServiceCollection()
    .AddLogging()
    .AddSingleton<IMyService, MyService>()
    .BuildServiceProvider();

// Create a command context with parsed arguments and options
var context = new CommandContext(
    commandName: "migrate",
    rawArguments: new[] { "--target", "v2.0.0", "--force", "src/Migrations" },
    parsedOptions: new Dictionary<string, string>
    {
        { "--target", "v2.0.0" },
        { "--force", "true" }
    },
    parsedArguments: new List<string> { "src/Migrations" },
    serviceProvider: services,
    output: Console.Out,
    errorOutput: Console.Error,
    cancellationToken: CancellationToken.None
);

// Access context properties
Console.WriteLine($"Command: {context.CommandName}");
Console.WriteLine($"Raw arguments: {string.Join(" ", context.RawArguments)}");
Console.WriteLine($"Target version: {context.GetOption("--target")}");
Console.WriteLine($"Force flag: {context.HasOption("--force")}");
Console.WriteLine($"Migration path: {context.ParsedArguments[0]}");

// Write to output streams
context.WriteOutput("Starting migration...");
context.WriteError("Warning: Database connection may be slow");

// Use metadata for command-specific data
context.SetMetadata("MigrationId", Guid.NewGuid());
if (context.TryGetMetadata<Guid>(out var migrationId))
{
    context.WriteOutput($"Migration ID: {migrationId}");
}
```

`CommandContext` is designed to be passed through middleware chains and command executors, providing a consistent interface for argument parsing, service resolution, and output handling throughout the CLI application.



## CommandExecutor

`CommandExecutor` provides a fluent API for registering and executing CLI commands with middleware support. It maintains a collection of registered commands and middleware components, executes commands asynchronously, and provides detailed execution results including success status, exit codes, and custom data payloads.

```csharp
using EfMigrationDiff.CLI;

// Create a command executor
var executor = new CommandExecutor()
  .RegisterCommand("migrate", "Migrate database to latest version")
  .RegisterCommand("rollback", "Rollback database to specified version")
  .RegisterMiddleware(async (context, next) => {
    Console.WriteLine($"Executing middleware for command: {context.CommandName}");
    return await next();
  });

// Execute a command asynchronously
var result = await executor.ExecuteAsync("migrate", new[] { "--target", "v2.0.0", "--force" });

// Check execution result
if (result.Success)
{
  Console.WriteLine($"Command succeeded with exit code: {result.ExitCode}");
  Console.WriteLine($"Message: {result.Message}");
  Console.WriteLine($"Data: {result.Data}");
}
else
{
  Console.WriteLine($"Command failed: {result.Message}");
}

// Get information about registered commands
Console.WriteLine($"Registered commands: {executor.GetRegisteredCommandCount()}");
foreach (var cmdName in executor.GetRegisteredCommandNames())
{
  Console.WriteLine($"- {cmdName}");
}

// Use static factory methods for quick results
var okResult = CommandExecutor.Ok("Operation completed successfully", 0);
var errorResult = CommandExecutor.Error("Invalid arguments provided", 1);

// Check if execution was short-circuited by middleware
if (executor.IsShortCircuited)
{
  Console.WriteLine($"Execution short-circuited with result: {executor.Result?.Message}");
}
```

The `CommandExecutor` supports middleware chaining, command registration, and provides detailed execution feedback through the `CommandResult` type which includes properties like `Success`, `Message`, `ExitCode`, `Data`, and `IsShortCircuited`.



## MigrationParserService

`MigrationParserService` is a utility service that reads Entity Framework migration files, extracts their metadata, validates their structure, and provides helper methods for comparing and analysing migrations. It can parse individual files, batches of files, or load all migrations from a directory.

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;

var parser = new MigrationParserService();

// -------------------------------------------------
// Parse a single migration file
// -------------------------------------------------
var singleFile = new MigrationFile("./Migrations/20230101120000_AddUserTable.cs", "MyDbContext");
await singleFile.LoadContentAsync();

Migration? migration = parser.ParseMigrationFile(singleFile);
Console.WriteLine($"Parsed migration: {migration?.Name ?? "null"}");

// -------------------------------------------------
// Parse multiple migration files at once
// -------------------------------------------------
var manyFiles = new List<MigrationFile> { singleFile };
List<Migration> manyMigrations = parser.ParseMigrationFiles(manyFiles);
Console.WriteLine($"Parsed {manyMigrations.Count} migrations");

// -------------------------------------------------
// Load all migrations from a directory
// -------------------------------------------------
List<Migration> loaded = await parser.LoadMigrationsFromDirectoryAsync("./Migrations", "MyDbContext");
Console.WriteLine($"Loaded {loaded.Count} migrations from directory");

// -------------------------------------------------
// Validate a migration file
// -------------------------------------------------
List<string> validationErrors = parser.ValidateMigrationFile(singleFile);
if (validationErrors.Count > 0)
{
    Console.WriteLine("Validation errors:");
    validationErrors.ForEach(Console.WriteLine);
}

// -------------------------------------------------
// Get declared dependencies
// -------------------------------------------------
if (migration != null)
{
    List<string> deps = parser.GetMigrationDependencies(migration);
    Console.WriteLine($"Dependencies: {string.Join(", ", deps)}");
}

// -------------------------------------------------
// Compare two migrations (if at least two are loaded)
// -------------------------------------------------
if (loaded.Count >= 2)
{
    var comparison = parser.CompareMigrations(loaded[0], loaded[1]);
    Console.WriteLine($"Same name: {comparison["SameName"]}");
    Console.WriteLine($"Statement difference: {comparison["StatementDifference"]}");
}

// -------------------------------------------------
// Extract raw SQL operations from a migration
// -------------------------------------------------
if (migration != null)
{
    List<string> sqlOps = parser.ExtractSqlOperations(migration);
    Console.WriteLine($"SQL operations found: {sqlOps.Count}");
}

// -------------------------------------------------
// Get a numeric sequence value from the migration ID
// -------------------------------------------------
int sequence = parser.GetMigrationSequence(migration?.Id ?? string.Empty);
Console.WriteLine($"Migration sequence: {sequence}");
```

The example demonstrates the most common public members of `MigrationParserService`, showing how to parse, validate, compare, and analyse EF migration files in a real‑world scenario.

## SchemaChangeDetectorService

The `SchemaChangeDetectorService` analyzes Entity Framework migration files to detect and categorize schema changes. It parses migration content to identify operations like table creation/dropping, column additions/modifications/deletions, index operations, and foreign key changes. The service provides methods to query changes by type, count destructive operations, check migration safety, and extract comprehensive metadata about schema modifications.

```csharp
using System;
using System.Collections.Generic;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;

var detector = new SchemaChangeDetectorService();

// -------------------------------------------------
// Detect all schema changes in a migration
// -------------------------------------------------
migration.Content = """
protected override void Up(MigrationBuilder migrationBuilder) 
{
migrationBuilder.CreateTable(
  name: "Users",
  columns: table => new
  {
    table.Column<int>("Id").PrimaryKey();
    table.Column<string>("Username").NotNullable();
    table.Column<DateTime>("CreatedAt").NotNullable();
  });
}
"""

var migration = new Migration { Id = "20240101120000_AddUsers", Content = migration.Content };

// Detect all changes
List<SchemaChange> allChanges = detector.DetectChanges(migration);
Console.WriteLine($"Total changes detected: {allChanges.Count}");

// -------------------------------------------------
// Get changes by specific type
// -------------------------------------------------
List<SchemaChange> tableCreations = detector.GetChangesByType(migration, SqlChangeType.CreateTable);
Console.WriteLine($"Tables created: {tableCreations.Count}");

// -------------------------------------------------
// Get all affected tables
// -------------------------------------------------
List<string> affectedTables = detector.GetAffectedTables(migration);
Console.WriteLine($"Affected tables: {string.Join(", ", affectedTables)}");

// -------------------------------------------------
// Count destructive changes
// -------------------------------------------------
int destructiveCount = detector.CountDestructiveChanges(migration);
Console.WriteLine($"Destructive changes: {destructiveCount}");

// -------------------------------------------------
// Check if migration is safe
// -------------------------------------------------
bool isSafe = detector.IsMigrationSafe(migration);
Console.WriteLine($"Is migration safe: {isSafe}");

// -------------------------------------------------
// Get comprehensive migration metadata
// -------------------------------------------------
Dictionary<string, object> metadata = detector.GetMigrationMetadata(migration);
foreach (var kvp in metadata)
{
  Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

The service's public API includes methods for detecting schema changes (`DetectChanges`), filtering by change type (`GetChangesByType`), identifying affected tables (`GetAffectedTables`), counting destructive operations (`CountDestructiveChanges`), validating migration safety (`IsMigrationSafe`), and extracting detailed metadata (`GetMigrationMetadata`).


## SchemaDiffPipelineService

The `SchemaDiffPipelineService` orchestrates end-to-end schema comparison workflows by integrating the v1 migration infrastructure with the v2 visual diff engine. It bridges the gap between branch-relative migration collection and schema visualization, producing comprehensive diff reports that include side-by-side, unified, and merge editor HTML outputs.

Here's a realistic usage example based on the service's public API:

```csharp
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;

// Create required dependencies (these would typically come from DI)
var migrationDiffService = new MigrationDiffService();
var diffEngine = new SchemaDiffEngine();
var renderer = new VisualDiffRenderer();

// Initialize the pipeline
var pipeline = new SchemaDiffPipelineService(
    migrationDiffService,
    diffEngine,
    renderer
);

// -------------------------------------------------
// Two-way schema diff between source and target branches
// -------------------------------------------------
var sourceBranch = new BranchInfo("feature/new-users", "main");
var targetBranch = new BranchInfo("main", "main");

var twoWayResult = pipeline.RunTwoWayDiff(sourceBranch, targetBranch);

Console.WriteLine($"Two-way diff completed: {twoWayResult.SourceBranch} vs {twoWayResult.TargetBranch}");
Console.WriteLine($"Side-by-side HTML length: {twoWayResult.SideBySideHtml.Length}");
Console.WriteLine($"Unified HTML length: {twoWayResult.UnifiedHtml.Length}");
Console.WriteLine($"Has destructive changes: {twoWayResult.HasDestructiveChanges}");

// -------------------------------------------------
// Three-way schema diff with merge analysis
// -------------------------------------------------
var baseBranch = new BranchInfo("release/v1.2", "main");
var featureBranch = new BranchInfo("feature/user-auth", "main");
var integrationBranch = new BranchInfo("main", "main");

var threeWayResult = pipeline.RunThreeWayDiff(
    baseBranch,
    featureBranch,
    integrationBranch
);

Console.WriteLine($"Three-way diff completed: {threeWayResult.BaseBranch} -> {threeWayResult.SourceBranch} vs {threeWayResult.TargetBranch}");
Console.WriteLine($"Merge editor HTML length: {threeWayResult.MergeEditorHtml.Length}");
Console.WriteLine($"Conflict count: {threeWayResult.ThreeWayDiff?.ConflictCount}");

// -------------------------------------------------
// Auto-merge trivially resolvable conflicts
// -------------------------------------------------
var mergeEditor = new MergeEditor();
var mergeResult = pipeline.TryAutoMerge(
    baseBranch,
    featureBranch,
    integrationBranch,
    mergeEditor
);

Console.WriteLine($"Auto-merge completed with {mergeResult.ResolvedCount} resolved conflicts");
Console.WriteLine($"Remaining unresolved: {mergeResult.UnresolvedWarnings.Count}");
```

The `SchemaDiffPipelineService` provides methods for two-way diffs (`RunTwoWayDiff`), three-way diffs (`RunThreeWayDiff`), and auto-merge attempts (`TryAutoMerge`), returning comprehensive results that include schema diff data, HTML visualizations, and metadata about the branches involved.

```csharp
using System;
using System.Collections.Generic;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;

var detector = new SchemaChangeDetectorService();

// -------------------------------------------------
// Detect all schema changes in a migration
// -------------------------------------------------
migration.Content = """
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "Users",
        columns: table => new
        {
            table.Column<int>("Id").PrimaryKey();
            table.Column<string>("Username").NotNullable();
            table.Column<DateTime>("CreatedAt").NotNullable();
        });
}
""";

var migration = new Migration { Id = "20240101120000_AddUsers", Content = migration.Content };

// Detect all changes
List<SchemaChange> allChanges = detector.DetectChanges(migration);
Console.WriteLine($"Total changes detected: {allChanges.Count}");

// -------------------------------------------------
// Get changes by specific type
// -------------------------------------------------
List<SchemaChange> tableCreations = detector.GetChangesByType(migration, SqlChangeType.CreateTable);
Console.WriteLine($"Tables created: {tableCreations.Count}");

// -------------------------------------------------
// Get all affected tables
// -------------------------------------------------
List<string> affectedTables = detector.GetAffectedTables(migration);
Console.WriteLine($"Affected tables: {string.Join(", ", affectedTables)}");

// -------------------------------------------------
// Count destructive changes
// -------------------------------------------------
int destructiveCount = detector.CountDestructiveChanges(migration);
Console.WriteLine($"Destructive changes: {destructiveCount}");

// -------------------------------------------------
// Check if migration is safe
// -------------------------------------------------
bool isSafe = detector.IsMigrationSafe(migration);
Console.WriteLine($"Is migration safe: {isSafe}");

// -------------------------------------------------
// Get comprehensive migration metadata
// -------------------------------------------------
Dictionary<string, object> metadata = detector.GetMigrationMetadata(migration);
foreach (var kvp in metadata)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

The service's public API includes methods for detecting schema changes (`DetectChanges`), filtering by change type (`GetChangesByType`), identifying affected tables (`GetAffectedTables`), counting destructive operations (`CountDestructiveChanges`), validating migration safety (`IsMigrationSafe`), and extracting detailed metadata (`GetMigrationMetadata`).