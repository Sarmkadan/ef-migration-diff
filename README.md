// ... existing content ...

## CacheService

The `CacheService` class provides an in-memory caching solution with expiration support, thread-safe operations, and comprehensive statistics. It stores cached values with optional time-to-live (TTL), supports generic types, and automatically removes expired entries during access or via scheduled cleanup.



```csharp
using EfMigrationDiff.Caching;
using System;

// Create a cache service with automatic cleanup every 5 minutes
var cache = new CacheService(cleanupInterval: TimeSpan.FromMinutes(5));

// -------------------------------------------------
// Basic caching operations
// -------------------------------------------------

// Set a value with 1 hour expiration
cache.Set("user:123", new User { Id = 123, Name = "John Doe", Email = "john@example.com" }, 
         expiration: TimeSpan.FromHours(1));

// Set a value without expiration (must be manually removed)
cache.Set("config:appSettings", new AppSettings { RepositoryPath = "./my-repo" });

// Try to get a value (returns false if not found or expired)
if (cache.TryGet<User>("user:123", out var user))
{
    Console.WriteLine($"Found user: {user.Name}");
}

// Get a value (throws if not found or expired)
try
{
    var user = cache.Get<User>("user:123");
    Console.WriteLine($"Retrieved user: {user.Name}");
}
catch (KeyNotFoundException)
{
    Console.WriteLine("User not found in cache");
}

// Get a value or default if not found
var settings = cache.GetOrDefault<AppSettings>("config:appSettings");
Console.WriteLine(settings != null ? "Settings found" : "Settings not found");

// -------------------------------------------------
// Advanced operations
// -------------------------------------------------

// Remove a specific key
bool removed = cache.Remove("user:123");
Console.WriteLine($"Key removed: {removed}");

// Remove all keys matching a pattern
int removedCount = cache.RemoveByPattern("user:");
Console.WriteLine($"Keys matching 'user:' removed: {removedCount}");

// Clear all cache entries
cache.Clear();
Console.WriteLine("Cache cleared");

// Remove expired entries manually
int expiredCount = cache.RemoveExpiredEntries();
Console.WriteLine($"Expired entries removed: {expiredCount}");

// -------------------------------------------------
// Cache statistics
// -------------------------------------------------

// Set some test data
cache.Set("temp:1", "value1");
cache.Set("temp:2", "value2");
cache.Set("temp:3", "value3", TimeSpan.FromSeconds(1));

// Get statistics
var stats = cache.GetStatistics();
Console.WriteLine($"Total entries: {stats.TotalEntries}");
Console.WriteLine($"Valid entries: {stats.ValidEntries}");
Console.WriteLine($"Expired entries: {stats.ExpiredEntries}");
Console.WriteLine($"Oldest entry: {stats.OldestEntry}");

// Dispose when done (stops cleanup timer)
cache.Dispose();
```

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


## ConfigurationBuilder

The `ConfigurationBuilder` class provides a fluent API for configuring the application's dependency injection container and middleware pipeline. It allows you to register services, middleware components, command validators, logging, validation, and error handling in a type-safe, chainable manner, then build the final service provider and command executor.

Here's a realistic usage example based on the class's public members:

```csharp
using EfMigrationDiff.Configuration;
using EfMigrationDiff.CLI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Create a configuration builder
var builder = new ConfigurationBuilder()
    .WithAppSettings(settings =>
    {
        settings.RepositoryPath = "./my-repository";
        settings.MigrationsPath = "./src/Migrations";
        settings.OutputPath = "./output";
    })
    .AddCommand("migrate", "Migrate database to latest version")
    .AddCommand("rollback", "Rollback database to specified version")
    .AddMiddleware(async (context, next) =>
    {
        Console.WriteLine($"Executing middleware for command: {context.CommandName}");
        return await next();
    })
    .AddCommandValidator("migrate", new CommandValidator()
        .RequireMinArguments(1)
        .RequireOption("--target")
    )
    .AddLogging(configure => configure.AddConsole())
    .AddValidation()
    .AddErrorHandling()
    .AddSingleton<IMyService, MyService>()
    .AddSingleton<IAnotherService, AnotherService>(ServiceLifetime.Singleton)
    .AddSingleton<IDatabaseService, DatabaseService>(sp => new DatabaseService("connection-string"));

// Build the service provider and command executor
var (executor, services) = builder.Build();

// Resolve services from the container
var myService = services.GetRequiredService<IMyService>();
var logger = services.GetRequiredService<ILogger<Program>>();
var appSettings = services.GetRequiredService<AppSettings>();

// Use the resolved services
Console.WriteLine($"Repository path: {appSettings.RepositoryPath}");
Console.WriteLine($"Migration parser: {services.GetService<MigrationParserService>() != null}");

// Execute a command
var result = await executor.ExecuteAsync("migrate", new[] { "--target", "v2.0.0", "--force" });
if (result.Success)
{
    Console.WriteLine($"Command succeeded: {result.Message}");
}
```

## MigrationRepository

The `MigrationRepository` class provides data access and CRUD operations for managing Entity Framework migrations. It serves as an in-memory repository that stores migrations and provides methods for querying, filtering, and managing migration data across different DbContexts and statuses.

```csharp
using EfMigrationDiff.Models;
using EfMigrationDiff.Repositories;

// Create a new migration repository
var repository = new MigrationRepository();

// -------------------------------------------------
// Add migrations to the repository
// -------------------------------------------------
var migration1 = new Migration
{
    Id = "20240101120000_AddUsers",
    Name = "AddUsers",
    DbContextName = "ApplicationDbContext",
    Status = MigrationStatus.Pending,
    Content = "CREATE TABLE [Users] ([Id] int PRIMARY KEY, [Username] nvarchar(max) NOT NULL)",
    CreatedAt = DateTime.UtcNow
};

var migration2 = new Migration
{
    Id = "20240101120100_AddPosts",
    Name = "AddPosts",
    DbContextName = "ApplicationDbContext",
    Status = MigrationStatus.Completed,
    Content = "CREATE TABLE [Posts] ([Id] int PRIMARY KEY, [UserId] int FOREIGN KEY REFERENCES [Users]([Id])",
    CreatedAt = DateTime.UtcNow.AddHours(1)
};

var migration3 = new Migration
{
    Id = "20240101120200_AddComments",
    Name = "AddComments",
    DbContextName = "BlogDbContext",
    Status = MigrationStatus.Pending,
    Content = "CREATE TABLE [Comments] ([Id] int PRIMARY KEY, [PostId] int FOREIGN KEY REFERENCES [Posts]([Id])",
    CreatedAt = DateTime.UtcNow.AddHours(2)
};

repository.Add(migration1);
repository.Add(migration2);
repository.Add(migration3);

// -------------------------------------------------
// Query migrations by various criteria
// -------------------------------------------------

// Get a single migration by ID
Migration? userMigration = repository.GetById("20240101120000_AddUsers");
Console.WriteLine($"Found migration: {userMigration?.Name}");

// Get all migrations for a DbContext
List<Migration> appMigrations = repository.GetByDbContext("ApplicationDbContext");
Console.WriteLine($"Application migrations count: {appMigrations.Count}");

// Get migrations by status
List<Migration> pendingMigrations = repository.GetByStatus(MigrationStatus.Pending);
Console.WriteLine($"Pending migrations: {pendingMigrations.Count}");

// Get all migrations sorted by creation date
List<Migration> allMigrations = repository.GetAll();
Console.WriteLine($"Total migrations: {allMigrations.Count}");

// Search migrations by name
List<Migration> userMigrations = repository.SearchByName("User");
Console.WriteLine($"Migrations with 'User' in name: {userMigrations.Count}");

// Get paginated results
List<Migration> firstPage = repository.GetPaginated(0, 2);
Console.WriteLine($"First page has {firstPage.Count} migrations");

// Check if migration exists
bool exists = repository.Exists("20240101120000_AddUsers");
Console.WriteLine($"Migration exists: {exists}");

// Get count
int totalCount = repository.Count();
Console.WriteLine($"Total migration count: {totalCount}");

// Get latest migration for a DbContext
Migration? latestAppMigration = repository.GetLatestByDbContext("ApplicationDbContext");
Console.WriteLine($"Latest Application migration: {latestAppMigration?.Name}");

// Get migrations by date range
List<Migration> recentMigrations = repository.GetByDateRange(
    DateTime.UtcNow.AddDays(-1),
    DateTime.UtcNow
);
Console.WriteLine($"Migrations in last 24 hours: {recentMigrations.Count}");

// Get migrations for multiple DbContexts
List<Migration> multiContextMigrations = repository.GetByDbContexts("ApplicationDbContext", "BlogDbContext");
Console.WriteLine($"Migrations across multiple contexts: {multiContextMigrations.Count}");

// -------------------------------------------------
// Update and delete operations
// -------------------------------------------------

// Update a migration
migration2.Status = MigrationStatus.Completed;
repository.Update(migration2);

// Delete a migration
bool deleted = repository.Delete("20240101120100_AddPosts");
Console.WriteLine($"Migration deleted: {deleted}");

// Clear all migrations
repository.Clear();
Console.WriteLine($"Repository cleared. Count: {repository.Count()}");
```

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

## SchemaDiffOptions

The `SchemaDiffOptions` record provides immutable configuration options that control how schema diffs and merge operations are computed and rendered by the `SchemaDiffEngine` and `VisualDiffFormatter`. It defines display labels for branches, rendering behavior, and limits for diff output formatting.

Key features include:
- Branch labeling for clear diff output
- Context line control for unified views
- SQL content inclusion control
- Metadata display options
- Whitespace normalization
- Hunk size limits

Here's a realistic usage example based on the class's public members:

```csharp
using EfMigrationDiff.Configuration;

// Create custom options for branch comparison
var branchOptions = new SchemaDiffOptions
{
    SourceLabel = "feature/new-users",
    TargetLabel = "main",
    ContextLines = 5,
    IncludeSqlContent = true,
    IncludeMetadata = true,
    IgnoreWhitespace = true,
    MaxHunkLines = 500
};

// Use factory methods for common scenarios
var branchComparison = SchemaDiffOptions.ForBranches("feature/auth-improvements", "main");
var mergeScenario = SchemaDiffOptions.ForMerge("release/v1.2", "feature/user-auth", "integration");

// Access default options
var defaultOptions = SchemaDiffOptions.Default;
Console.WriteLine($"Default base label: {defaultOptions.BaseLabel}");
Console.WriteLine($"Default context lines: {defaultOptions.ContextLines}");
Console.WriteLine($"Default SQL inclusion: {defaultOptions.IncludeSqlContent}");

// Use with SchemaDiffEngine
var engine = new SchemaDiffEngine();
var diffResult = engine.ComputeDiff(sourceChanges, targetChanges, branchOptions);
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

## FileHelper

The `FileHelper` utility class provides robust file system operations with comprehensive error handling and validation. It includes methods for reading, writing, copying, and deleting files, as well as directory management and file metadata operations. The class is designed to safely handle file operations with proper exception handling and validation checks.

```csharp
using EfMigrationDiff.Utilities;

// -------------------------------------------------
// File reading and writing operations
// -------------------------------------------------

// Read a file safely (returns null if file doesn't exist)
string? fileContent = FileHelper.ReadFileAsync("./Migrations/20240101120000_AddUsers.cs");
if (fileContent != null)
{
    Console.WriteLine($"File size: {FileHelper.GetHumanReadableFileSize(FileHelper.GetFileSize("./Migrations/20240101120000_AddUsers.cs"))}");
}

// Write content to a file (automatically creates directories if needed)
FileHelper.WriteFile("./output/generated-migration.cs", "public class GeneratedMigration { ... }");

// Check if a file exists and get its size
long fileSize = FileHelper.GetFileSize("./Migrations/20240101120000_AddUsers.cs");
Console.WriteLine($"File size: {FileHelper.GetHumanReadableFileSize(fileSize)}");

// -------------------------------------------------
// Directory operations
// -------------------------------------------------

// Ensure a directory exists
FileHelper.EnsureDirectoryExists("./output/migrations");

// Get all migration files from a directory
List<string> migrationFiles = FileHelper.GetMigrationFiles("./Migrations");
Console.WriteLine($"Found {migrationFiles.Count} migration files");

// Get all subdirectories matching a pattern
List<string> subdirectories = FileHelper.GetSubdirectories("./src", "*");
Console.WriteLine($"Found {subdirectories.Count} subdirectories");

// Check if a directory is a valid migration directory
bool isValidMigrationDir = FileHelper.IsValidMigrationDirectory("./Migrations");
Console.WriteLine($"Is valid migration directory: {isValidMigrationDir}");

// -------------------------------------------------
// File management operations
// -------------------------------------------------

// Copy a file with automatic directory creation
FileHelper.CopyFile(
    sourcePath: "./templates/migration-template.cs",
    destinationPath: "./Migrations/20240101120001_AddPosts.cs"
);

// Delete a file safely
bool deleted = FileHelper.DeleteFile("./temp/old-migration.cs");
Console.WriteLine($"File deleted: {deleted}");

// Get file metadata
DateTime lastModified = FileHelper.GetLastModifiedTime("./Migrations/20240101120000_AddUsers.cs");
Console.WriteLine($"Last modified: {lastModified}");

// -------------------------------------------------
// Path manipulation utilities
// -------------------------------------------------

// Combine multiple path segments
string fullPath = FileHelper.CombinePath(".", "src", "Migrations", "20240101120000_AddUsers.cs");
Console.WriteLine($"Combined path: {fullPath}");

// Get relative path between two paths
string relativePath = FileHelper.GetRelativePath(
    basePath: "./src",
    targetPath: "./src/Migrations/20240101120000_AddUsers.cs"
);
Console.WriteLine($"Relative path: {relativePath}");
```

## DataTableHelper

The `DataTableHelper` class provides utility methods for formatting and displaying data in various table formats including console tables, markdown tables, key-value tables, and formatted statistics. It supports generic collections and provides consistent formatting across different output types.

```csharp
using EfMigrationDiff.Utilities;
using System;
using System.Collections.Generic;

// Sample data for demonstration
var migrations = new List<MigrationInfo>
{
    new MigrationInfo("20240101120000_AddUsers", "ApplicationDbContext", 15, DateTime.Parse("2024-01-01")),
    new MigrationInfo("20240101120100_AddPosts", "ApplicationDbContext", 8, DateTime.Parse("2024-01-02")),
    new MigrationInfo("20240101120200_AddComments", "BlogDbContext", 5, DateTime.Parse("2024-01-03"))
};

// Format as console table with custom column names
string consoleTable = DataTableHelper.FormatAsConsoleTable(
    migrations,
    "Migration ID", "DbContext", "File Count", "Created Date"
);
Console.WriteLine("Console Table:");
Console.WriteLine(consoleTable);

// Format as markdown table (good for documentation)
string markdownTable = DataTableHelper.FormatAsMarkdownTable(migrations);
Console.WriteLine("\nMarkdown Table:");
Console.WriteLine(markdownTable);

// Format key-value data
var stats = new Dictionary<string, object?>
{
    { "Total Migrations", migrations.Count },
    { "Total Files", migrations.Sum(m => m.FileCount) },
    { "DbContexts", migrations.Select(m => m.DbContext).Distinct().Count() },
    { "Average Files per Migration", Math.Round(migrations.Average(m => m.FileCount)) }
};

string keyValueTable = DataTableHelper.FormatKeyValueTable(stats, "Metric", "Value");
Console.WriteLine("\nKey-Value Table:");
Console.WriteLine(keyValueTable);

// Format statistics with borders
var statistics = new Dictionary<string, long>
{
    { "Total Migrations", migrations.Count },
    { "Total Files", migrations.Sum(m => m.FileCount) },
    { "Completed", 15 },
    { "Pending", 3 }
};

string formattedStats = DataTableHelper.FormatStatistics(statistics);
Console.WriteLine(formattedStats);

// Create a progress bar
string progressBar = DataTableHelper.CreateProgressBar(7, migrations.Count);
Console.WriteLine($"\nProgress: {progressBar}");

// Format durations and file sizes
var duration = TimeSpan.FromSeconds(45.67);
var fileSize = 1024 * 1024 * 5; // 5 MB

Console.WriteLine($"\nDuration: {DataTableHelper.FormatDuration(duration)}");
Console.WriteLine($"File Size: {DataTableHelper.FormatFileSize(fileSize)}");

// Helper class for demonstration
public class MigrationInfo
{
    public string MigrationId { get; set; }
    public string DbContext { get; set; }
    public int FileCount { get; set; }
    public DateTime CreatedDate { get; set; }

    public MigrationInfo(string migrationId, string dbContext, int fileCount, DateTime createdDate)
    {
        MigrationId = migrationId;
        DbContext = dbContext;
        FileCount = fileCount;
        CreatedDate = createdDate;
    }
}
```

## DbContextRepository

The `DbContextRepository` class provides data access and CRUD operations for managing Entity Framework DbContext metadata. It serves as an in-memory repository that stores DbContext metadata and provides methods for querying, filtering, and managing DbContext data across different assemblies and database providers.

```csharp
using EfMigrationDiff.Models;
using EfMigrationDiff.Repositories;

// Create a new DbContext repository
var repository = new DbContextRepository();

// -------------------------------------------------
// Add DbContext metadata to the repository
// -------------------------------------------------
var context1 = new DbContextMetadata
{
    Id = "ApplicationDbContext",
    ContextName = "ApplicationDbContext",
    AssemblyName = "MyApp.Data",
    DatabaseProvider = "Microsoft.EntityFrameworkCore.SqlServer",
    EntityTypes = new List<string> { "User", "Post", "Comment" },
    MigrationIds = new List<string> { "20240101120000_AddUsers", "20240101120100_AddPosts" },
    LastScannedAt = DateTime.UtcNow
};

var context2 = new DbContextMetadata
{
    Id = "IdentityDbContext",
    ContextName = "IdentityDbContext",
    AssemblyName = "MyApp.Identity",
    DatabaseProvider = "Microsoft.EntityFrameworkCore.SqlServer",
    EntityTypes = new List<string> { "User", "Role", "UserRole" },
    MigrationIds = new List<string> { "20240101120200_AddIdentity" },
    LastScannedAt = DateTime.UtcNow.AddMinutes(-30)
};

repository.Add(context1);
repository.Add(context2);

// -------------------------------------------------
// Query DbContexts by various criteria
// -------------------------------------------------

// Get a single DbContext by ID
DbContextMetadata? appContext = repository.GetById("ApplicationDbContext");
Console.WriteLine($"Found DbContext: {appContext?.ContextName}");

// Get DbContext by name
DbContextMetadata? identityContext = repository.GetByName("IdentityDbContext");
Console.WriteLine($"Found by name: {identityContext?.Id}");

// Get all DbContexts for a specific assembly
List<DbContextMetadata> dataAssemblyContexts = repository.GetByAssembly("MyApp.Data");
Console.WriteLine($"Data assembly contexts: {dataAssemblyContexts.Count}");

// Get DbContexts by database provider
List<DbContextMetadata> sqlServerContexts = repository.GetByProvider("Microsoft.EntityFrameworkCore.SqlServer");
Console.WriteLine($"SQL Server contexts: {sqlServerContexts.Count}");

// Get all DbContexts
List<DbContextMetadata> allContexts = repository.GetAll();
Console.WriteLine($"Total DbContexts: {allContexts.Count}");

// Search DbContexts by name
List<DbContextMetadata> userContexts = repository.SearchByName("User");
Console.WriteLine($"Contexts with 'User' in name: {userContexts.Count}");

// Get recently scanned DbContexts (last 24 hours)
List<DbContextMetadata> recentContexts = repository.GetRecentlyScanned(TimeSpan.FromHours(24));
Console.WriteLine($"Recently scanned: {recentContexts.Count}");

// Get DbContexts with migrations
List<DbContextMetadata> contextsWithMigrations = repository.GetWithMigrations();
Console.WriteLine($"Contexts with migrations: {contextsWithMigrations.Count}");

// Get DbContexts by entity type
List<DbContextMetadata> userDbContexts = repository.GetByEntityType("User");
Console.WriteLine($"Contexts managing User entity: {userDbContexts.Count}");

// Get DbContexts by provider and assembly
List<DbContextMetadata> contextsByProviderAndAssembly = repository.GetByProviderAndAssembly(
    "Microsoft.EntityFrameworkCore.SqlServer",
    "MyApp.Data"
);
Console.WriteLine($"Contexts by provider+assembly: {contextsByProviderAndAssembly.Count}");

// Check if DbContext exists
bool exists = repository.Exists("ApplicationDbContext");
Console.WriteLine($"ApplicationDbContext exists: {exists}");

// Get count
int totalCount = repository.Count();
Console.WriteLine($"Total count: {totalCount}");

// -------------------------------------------------
// Update and delete operations
// -------------------------------------------------

// Update a DbContext
context1.EntityTypes.Add("Product");
repository.Update(context1);

// Delete a DbContext
bool deleted = repository.Delete("IdentityDbContext");
Console.WriteLine($"IdentityDbContext deleted: {deleted}");

// Clear all DbContexts
repository.Clear();
Console.WriteLine($"Repository cleared. Count: {repository.Count()}");
```

## GitRepository

The `GitRepository` class provides a wrapper around LibGit2Sharp operations, enabling programmatic access to git repositories for branch management, commit history analysis, and file operations. It simplifies common git workflows like retrieving branch information, comparing branches, and reading file contents from specific commits.

```csharp
using EfMigrationDiff.Repositories;
using System;

// Create a new GitRepository instance pointing to a repository path
var gitRepo = new GitRepository("./my-repository");

// Initialize the repository connection
if (gitRepo.Initialize())
{
    Console.WriteLine($"Repository initialized: {gitRepo}");
    
    // Get all branches
    var allBranches = gitRepo.GetAllBranches();
    Console.WriteLine($"Total branches: {allBranches.Count}");
    foreach (var branch in allBranches)
    {
        Console.WriteLine($"- {branch.Name} (SHA: {branch.CommitSha})");
    }
    
    // Get current branch
    var currentBranch = gitRepo.GetCurrentBranch();
    Console.WriteLine($"Current branch: {currentBranch}");
    
    // Get commits between two branches
    var commits = gitRepo.GetCommitsBetween("feature/new-feature", "main");
    Console.WriteLine($"Commits between branches: {commits.Count}");
    
    // Get changed files between branches
    var changedFiles = gitRepo.GetChangedFiles("feature/new-feature", "main");
    Console.WriteLine($"Changed files: {changedFiles.Count}");
    foreach (var file in changedFiles.Take(5))
    {
        Console.WriteLine($"  - {file}");
    }
    
    // Get file content from a specific commit
    var fileContent = gitRepo.GetFileContent(
        "abc123def456",
        "src/Migrations/20240101120000_AddUsers.cs"
    );
    Console.WriteLine($"File content retrieved: {(fileContent != null ? "Yes" : "No"}");
    
    // Check repository clean status
    var isClean = gitRepo.IsClean();
    Console.WriteLine($"Repository is clean: {isClean}");
    
    // Get repository root path
    var repoRoot = gitRepo.GetRepositoryRoot();
    Console.WriteLine($"Repository root: {repoRoot}");
}

// Dispose when done
gitRepo.Dispose();
```

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