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


## AppSettings

The `AppSettings` class provides centralized configuration for the EF Migration Diff tool, exposing settings that control repository paths, migration analysis behavior, output formats, and validation rules. It serves as the single source of truth for application configuration across all CLI commands and services.

## EfMigrationDiffOptions

The `EfMigrationDiffOptions` class provides strongly-typed configuration for the ef-migration-diff tool, controlling repository paths, migration analysis behavior, output formats, and validation rules. It supports branch comparison, concurrent analysis, and comprehensive report generation with validation capabilities.

Here's a realistic usage example based on the class's public members:

```csharp
using EfMigrationDiff.Configuration;

// Create configuration with required values
var options = new EfMigrationDiffOptions
{
    RepositoryPath = "./my-repository",
    MigrationsPath = "./src/Migrations",
    OutputPath = "./reports",
    ReportFormat = "html",
    EnableDetailedLogging = true,
    MaxConcurrentAnalysis = 8,
    GenerateHtmlReport = true,
    GenerateJsonReport = true,
    DbContextNames = new[] { "ApplicationDbContext", "IdentityDbContext" },
    SourceBranch = "feature/new-users",
    TargetBranch = "main",
    SchemaDiff = new SchemaDiffOptions
    {
        SourceLabel = "feature/new-users",
        TargetLabel = "main",
        IgnoreWhitespace = true
    }
};

// Validate configuration
try
{
    options.ValidateAndThrow();
    Console.WriteLine("Configuration is valid");
}
catch (Exception ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
}

// Access configuration values
Console.WriteLine($"Repository path: {options.RepositoryPath}");
Console.WriteLine($"Migrations path: {options.MigrationsPath}");
Console.WriteLine($"Output path: {options.OutputPath}");
Console.WriteLine($"Source branch: {options.SourceBranch}");
Console.WriteLine($"Target branch: {options.TargetBranch}");
Console.WriteLine($"Max concurrent: {options.MaxConcurrentAnalysis}");

// Check report generation settings
Console.WriteLine($"Generate HTML: {options.GenerateHtmlReport}");
Console.WriteLine($"Generate JSON: {options.GenerateJsonReport}");
Console.WriteLine($"DbContexts: {string.Join(", ", options.DbContextNames)}");
```

Here's a realistic usage example based on the class's public members:

```csharp
using EfMigrationDiff.Configuration;

// Create configuration with default values
var settings = new AppSettings
{
    RepositoryPath = "./my-repo",
    MigrationsPath = "./src/Migrations",
    OutputPath = "./output",
    ReportFormat = "html",
    EnableDetailedLogging = true,
    MaxConcurrentAnalysis = 4,
    GenerateHtmlReport = true,
    GenerateJsonReport = true,
    DbContextNames = new[] { "ApplicationDbContext", "IdentityDbContext" },
    SourceBranch = "feature/new-feature",
    TargetBranch = "main"
};

// Access configuration values
Console.WriteLine($"Repository path: {settings.RepositoryPath}");
Console.WriteLine($"Migrations path: {settings.MigrationsPath}");
Console.WriteLine($"Output path: {settings.OutputPath}");
Console.WriteLine($"Report format: {settings.ReportFormat}");
Console.WriteLine($"Max concurrent analysis: {settings.MaxConcurrentAnalysis}");

// Use helper methods
string migrationsDir = settings.GetMigrationsDirectory();
Console.WriteLine($"Migrations directory: {migrationsDir}");

string outputDir = settings.GetOutputDirectory();
Console.WriteLine($"Output directory: {outputDir}");

// Validate configuration
try
{
    settings.ValidateAndThrow();
    Console.WriteLine("Configuration is valid");
}
catch (Exception ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
}

// Convert to migration options
var migrationOptions = settings.ToEfMigrationDiffOptions();
Console.WriteLine($"Migration options created: {migrationOptions != null}");

// Ensure output directory exists
settings.EnsureOutputDirectory();
```

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

## SchemaDiffEngine

The `SchemaDiffEngine` is the core diff computation and three-way merge engine for Entity Framework migration schemas. It implements both schema comparison (via `ISchemaDiffEngine`) and merge resolution planning (via `IMergeEditor`), providing a unified API for detecting changes, computing diffs between branches, and resolving merge conflicts. The engine supports two-way diffs for comparing any two migration sets, three-way diffs for merge scenarios with a common base, and automated resolution strategies for trivially resolvable conflicts.

Here's a realistic usage example based on the engine's public API:

## DependencyInjection

The `DependencyInjection` class provides centralized dependency injection configuration for the EF Migration Diff tool. It registers all application services, repositories, and configuration options using the Microsoft.Extensions.DependencyInjection framework, enabling consistent service resolution across CLI commands and services.

Here's a realistic usage example based on the class's public members:

```csharp
using EfMigrationDiff.Configuration;
using EfMigrationDiff.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Create a service collection and register all application services
var services = new ServiceCollection()
    .AddApplicationServices(settings =>
    {
        settings.RepositoryPath = "./my-repository";
        settings.MigrationsPath = "./src/Migrations";
        settings.OutputPath = "./output";
    })
    .AddLogging(configure => configure.AddConsole());

// Build the service provider
var serviceProvider = services.BuildServiceProvider();

// Resolve services from the container
var migrationParser = serviceProvider.GetService<MigrationParserService>();
var schemaDetector = serviceProvider.GetService<SchemaChangeDetectorService>();
var conflictDetector = serviceProvider.GetService<ConflictDetectionService>();
var gitRepository = serviceProvider.GetService<GitRepository>();
var appSettings = serviceProvider.GetService<AppSettings>();

// Use the resolved services
Console.WriteLine($"Repository path: {appSettings?.RepositoryPath}");
Console.WriteLine($"Migration parser: {migrationParser != null}");
Console.WriteLine($"Schema detector: {schemaDetector != null}");

// Create a service provider with a specific repository path
var provider1 = DependencyInjection.CreateServiceProvider("./another-repo");
var migrationDiffService = provider1.GetService<MigrationDiffService>();

// Create a service provider with custom settings
var provider2 = DependencyInjection.CreateServiceProvider(settings =>
{
    settings.RepositoryPath = "./custom-repo";
    settings.EnableDetailedLogging = true;
});
var gitRepo = provider2.GetService<GitRepository>();

// Create a service provider with IOptions configuration
var provider3 = DependencyInjection.CreateServiceProviderWithOptions(options =>
{
    options.MaxConcurrentAnalysis = 8;
    options.IgnoreWhitespace = true;
});
```

The `DependencyInjection` class exposes several extension methods and factory methods for service registration and resolution, including `AddApplicationServices()` for registering all application components, `CreateServiceProvider()` for creating configured service providers, and `GetService<T>()` for retrieving services from the container.

```csharp
using System;
using System.Collections.Generic;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using Microsoft.Extensions.Logging;

// Create required dependencies (typically from DI container)
var conflictDetection = new ConflictDetectionService();
var loggerFactory = LoggerFactory.Create(builder => {});
var logger = loggerFactory.CreateLogger<SchemaDiffEngine>();

// Initialize the engine
var engine = new SchemaDiffEngine(conflictDetection, logger);

// -------------------------------------------------
// Two-way schema diff between source and target changes
// -------------------------------------------------
var sourceChanges = new List<SchemaChange>
{
    new SchemaChange("20240101120000_AddUsers", SqlChangeType.CreateTable, 
        "CREATE TABLE [Users] ([Id] int PRIMARY KEY, [Username] nvarchar(max) NOT NULL)"),
    new SchemaChange("20240101120100_AddPosts", SqlChangeType.CreateTable,
        "CREATE TABLE [Posts] ([Id] int PRIMARY KEY, [UserId] int FOREIGN KEY REFERENCES [Users]([Id])")
};

var targetChanges = new List<SchemaChange>
{
    new SchemaChange("20240101120000_AddUsers", SqlChangeType.CreateTable,
        "CREATE TABLE [Users] ([Id] int PRIMARY KEY, [Username] nvarchar(max) NOT NULL, [Email] nvarchar(max) NOT NULL)"),
    new SchemaChange("20240101120200_AddComments", SqlChangeType.CreateTable,
        "CREATE TABLE [Comments] ([Id] int PRIMARY KEY, [PostId] int FOREIGN KEY REFERENCES [Posts]([Id])")
};

// Compute diff with custom options
var options = new SchemaDiffOptions
{
    SourceLabel = "feature/new-users",
    TargetLabel = "main",
    IgnoreWhitespace = true
};

var diffResult = engine.ComputeDiff(sourceChanges, targetChanges, options);

Console.WriteLine($"Diff computed: {diffResult.SourceOnlyChanges.Count} source-only changes, " +
                $"{diffResult.TargetOnlyChanges.Count} target-only changes, " +
                $"{diffResult.ModifiedChanges.Count} modified changes");
Console.WriteLine($"Side-by-side hunks: {diffResult.Hunks.Count}");

// -------------------------------------------------
// Three-way schema diff with merge analysis
// -------------------------------------------------
var baseChanges = new List<SchemaChange>
{
    new SchemaChange("20240101120000_AddUsers", SqlChangeType.CreateTable,
        "CREATE TABLE [Users] ([Id] int PRIMARY KEY)")
};

var threeWayResult = engine.ComputeThreeWayDiff(
    baseChanges,
    sourceChanges,
    targetChanges,
    new SchemaDiffOptions { BaseLabel = "release/v1.2", SourceLabel = "feature/user-auth", TargetLabel = "main" }
);

Console.WriteLine($"Three-way diff completed: {threeWayResult.ConflictRegions.Count} conflict regions detected");
Console.WriteLine($"Base→Source changes: {threeWayResult.BaseToSource.Hunks.Count} hunks");
Console.WriteLine($"Base→Target changes: {threeWayResult.BaseToTarget.Hunks.Count} hunks");

// -------------------------------------------------
// Auto-merge trivially resolvable conflicts
// -------------------------------------------------
var autoMergePlan = engine.AutoMerge(threeWayResult);
Console.WriteLine($"Auto-merge identified {autoMergePlan.Resolutions.Count(r => r.Value != MergeResolutionStrategy.Unresolved)} " +
                $"resolvable conflicts out of {threeWayResult.ConflictRegions.Count} total");

// -------------------------------------------------
// Apply merge resolution and validate
// -------------------------------------------------
var mergeResult = engine.ApplyMergeResolution(threeWayResult, autoMergePlan);
Console.WriteLine($"Merge result: {mergeResult.IsSuccessful}, {mergeResult.ResolvedChanges.Count} resolved, " +
                $"{mergeResult.UnresolvedCount} unresolved");

// Validate the resolution plan
var validationErrors = engine.ValidateResolution(autoMergePlan, threeWayResult);
if (validationErrors.Count > 0)
{
    Console.WriteLine("Validation errors:");
    foreach (var error in validationErrors) Console.WriteLine($"- {error}");
}

// -------------------------------------------------
// Manual resolution strategies
// -------------------------------------------------
var acceptSourcePlan = engine.AcceptSource(threeWayResult);
var acceptTargetPlan = engine.AcceptTarget(threeWayResult);

Console.WriteLine($"AcceptSource plan: {acceptSourcePlan.Resolutions.Count} resolutions");
Console.WriteLine($"AcceptTarget plan: {acceptTargetPlan.Resolutions.Count} resolutions");
```

The `SchemaDiffEngine` exposes public members for diff computation (`ComputeDiff`, `ComputeThreeWayDiff`), merge resolution (`ApplyMergeResolution`, `AcceptSource`, `AcceptTarget`, `AutoMerge`), and validation (`ValidateResolution`). It serves as the central component for schema comparison and merge workflows in the EF Migration Diff tool.


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