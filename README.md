# Architecture

For the big picture - how branches get parsed into migrations, where the v1/v2
diff pipelines split, extension points and known limitations - see
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). The sections below are per-class
reference docs.

## Migration

The `Migration` class represents an Entity Framework Core migration with comprehensive metadata and content analysis capabilities. It serves as the core data structure for tracking, comparing, and analyzing migrations throughout the ef-migration-diff library. The class includes properties for migration identification (Id, Name, Timestamp), timestamps (CreatedAt), database context association (DbContextName), content storage (Content, MetadataContent), status tracking (Status, Description), sequencing (Sequence), and conflict detection results (SchemaChanges, DetectedConflicts).

Here's an example of how to use the `Migration` class:

```csharp
// Create a migration instance
var migration = new Migration
{
Id = "20240115093045",
Name = "CreateUsersTable",
Timestamp = "20240115093045",
CreatedAt = DateTime.Parse("2024-01-15T09:30:45"),
DbContextName = "ApplicationDbContext",
Content = "migrationBuilder.CreateTable(\n    name: \"Users\",
    table => table.Column<int>(name: \"Id\")
);",
MetadataContent = "{\"Author\":\"System\",\"TargetDatabase\":\"Production\"}",
Status = MigrationStatus.Pending,
Description = "Initial migration creating Users table",
Sequence = 1
};

// Generate a unique timestamp
var timestamp = Migration.GenerateTimestamp();
Console.WriteLine($"Generated timestamp: {timestamp}"); // e.g., "20240115093045"

// Validate the migration
var isValid = migration.IsValid();
Console.WriteLine($"Is valid: {isValid}"); // true

// Clone the migration with a new ID
var clonedMigration = migration.Clone();
Console.WriteLine($"Cloned migration ID: {clonedMigration.Id}"); // new GUID

// Get content size in bytes
var contentSize = migration.GetContentSize();
Console.WriteLine($"Content size: {contentSize} bytes");

// Count SQL statements
var statementCount = migration.CountStatements();
Console.WriteLine($"Statement count: {statementCount}");

// Use ToString() for debugging/logging
Console.WriteLine(migration.ToString()); // "CreateUsersTable (20240115093045) - Pending"

// Create from constructor
var newMigration = new Migration("20240115093046", "AddEmailToUsers", "ApplicationDbContext")
{
Description = "Add email column to Users table",
Status = MigrationStatus.Pending,
Sequence = 2
};
Console.WriteLine(newMigration.ToString()); // "AddEmailToUsers (20240115093046) - Pending"
```

## MigrationFile

The `MigrationFile` class represents an Entity Framework Core migration file, storing metadata and content about a migration. It provides properties for file system information (file path, size, timestamps), migration identification (migration ID, context name), and content management (content loading, hashing, validation). This class is used throughout the ef-migration-diff library for parsing, comparing, and analyzing EF Core migrations.

## MergeAttempt

The `MergeAttempt` class records the outcome of a single automated conflict resolution attempt during migration merging. It captures the conflict identifier, type, strategy applied, success status, and any failure reasons or merged content. This class is used by the `MergeResult` class to track individual resolution attempts within a batch merge operation.

Here's an example of how to use the `MergeAttempt` class:

```csharp
// Create a merge attempt for a column conflict
var attempt = new MergeAttempt
{
    ConflictId = "conf-20240615-001",
    ConflictType = ConflictType.ColumnConflict,
    StrategyApplied = MergeStrategy.LastWins,
    Succeeded = true,
    MergedContent = "migrationBuilder.AddColumn<int>(\"Age\", \"Users\");",
    AttemptedAt = DateTime.UtcNow
};

// Check if the attempt succeeded
Console.WriteLine($"Attempt succeeded: {attempt.Succeeded}"); // true

// Access the strategy that was applied
Console.WriteLine($"Strategy: {attempt.StrategyApplied}"); // LastWins

// Get the human-readable description
Console.WriteLine(attempt.ToString()); // "[OK] ColumnConflict — resolved via LastWins"

// Create a failed attempt for demonstration
var failedAttempt = new MergeAttempt
{
    ConflictId = "conf-20240615-002",
    ConflictType = ConflictType.TableConflict,
    StrategyApplied = MergeStrategy.Combine,
    Succeeded = false,
    FailureReason = "Incompatible schema changes detected",
    AttemptedAt = DateTime.UtcNow
};

Console.WriteLine(failedAttempt.ToString()); // "[FAIL] TableConflict — Incompatible schema changes detected"
```

## DbContextMetadata

The `DbContextMetadata` class represents metadata about a DbContext and its configuration. It tracks context identification (Id, ContextName), assembly information (AssemblyName, Namespace), database configuration (DatabaseProvider, ConnectionString), migration history (MigrationIds), entity types (EntityTypes), and custom properties (Properties). This class is used throughout the ef-migration-diff library for scanning, analyzing, and comparing DbContext configurations.

Here's an example of how to use the `DbContextMetadata` class:

```csharp
// Create metadata for a DbContext
var metadata = new DbContextMetadata("ApplicationDbContext", "MyApp.Data")
{
    Namespace = "MyApp.Data.Contexts",
    DatabaseProvider = "SqlServer",
    ConnectionString = "Server=localhost;Database=MyApp;Trusted_Connection=True;"
};

// Add migrations to the context's history
metadata.AddMigration("20240115093045_CreateUsersTable");
metadata.AddMigration("20240116104530_AddRolesTable");
metadata.AddMigration("20240201142015_AddEmailToUsers");

// Add entity types managed by the context
metadata.AddEntityType("User");
metadata.AddEntityType("Role");
metadata.AddEntityType("Permission");

// Add custom properties
metadata.AddProperty("Version", "1.2.3");
metadata.AddProperty("Environment", "Development");

// Check metadata validity
var isValid = metadata.IsValid();
Console.WriteLine($"Metadata is valid: {isValid}"); // true

// Get counts
Console.WriteLine($"Migration count: {metadata.GetMigrationCount()}"); // 3
Console.WriteLine($"Entity type count: {metadata.GetEntityTypeCount()}"); // 3

// Check if a migration exists
var hasMigration = metadata.HasMigration("20240115093045_CreateUsersTable");
Console.WriteLine($"Has migration: {hasMigration}"); // true

// Get a property value
var version = metadata.GetProperty("Version");
Console.WriteLine($"Version: {version}"); // "1.2.3"

// Get the last migration
var lastMigration = metadata.GetLastMigration();
Console.WriteLine($"Last migration: {lastMigration}"); // "20240201142015_AddEmailToUsers"

// Get provider display name
var providerName = metadata.GetProviderDisplayName();
Console.WriteLine($"Provider: {providerName}"); // "SQL Server"

// Mark as recently scanned
metadata.MarkAsScanned();
Console.WriteLine($"Last scanned: {metadata.LastScannedAt}");

// Use ToString() for debugging/logging
Console.WriteLine(metadata.ToString()); // "ApplicationDbContext (MyApp.Data) - SQL Server"
```

## ConflictInfo

The `ConflictInfo` class represents a detected conflict between two Entity Framework Core migrations during comparison or merging operations. It tracks conflict identification (Id, FirstMigrationId, SecondMigrationId), severity levels (Severity), type classification (ConflictType), descriptive information (Description, Details), affected schema elements (AffectedElements), and resolution status (IsResolved, ResolutionStrategy, DetectedAt). This class is used throughout the ef-migration-diff library to identify, categorize, and resolve conflicts that arise when comparing migration histories.

Here's an example of how to use the `ConflictInfo` class:

```csharp
// Create a conflict between two migrations
var conflict = new ConflictInfo(
    firstMigrationId: "20240115093045_CreateUsersTable",
    secondMigrationId: "20240116104530_AddRolesTable",
    conflictType: ConflictType.ColumnConflict
);

// Set additional properties
conflict.Id = Guid.NewGuid().ToString();
conflict.Description = "Column 'Email' already exists in table 'Users'";
conflict.Severity = ConflictSeverity.Error;

// Add affected elements
conflict.AddAffectedElement("Users.Email");
conflict.AddAffectedElement("Users.Username");

// Add detailed context
conflict.AddDetail("MigrationAChange", "Added Email column with type nvarchar(255)");
conflict.AddDetail("MigrationBChange", "Added Email column with type varchar(100)");

// Validate the conflict
var isValid = conflict.IsValid();
Console.WriteLine($"Conflict is valid: {isValid}"); // true

// Get human-readable title
var title = conflict.GetTitle();
Console.WriteLine($"Conflict title: {title}"); // "Column Definition Conflict"

// Check if blocking
var isBlocking = conflict.IsBlocking();
Console.WriteLine($"Is blocking: {isBlocking}"); // true

// Mark as resolved with a strategy
conflict.MarkResolved("ManualReviewRequired");
Console.WriteLine($"Is resolved: {conflict.IsResolved}"); // true
Console.WriteLine($"Resolution strategy: {conflict.ResolutionStrategy}"); // "ManualReviewRequired"

// Get other migration involved
var otherMigration = conflict.GetOtherMigration("20240115093045_CreateUsersTable");
Console.WriteLine($"Other migration: {otherMigration}"); // "20240116104530_AddRolesTable"

// Use ToString() for debugging/logging
Console.WriteLine(conflict.ToString()); // "[Error] Column Definition Conflict between 20240115093045_CreateUsersTable and 20240116104530_AddRolesTable"

// Create from constructor with different severity
var criticalConflict = new ConflictInfo(
    firstMigrationId: "20240201142015_AddEmailToUsers",
    secondMigrationId: "20240202153020_RemoveEmailColumn",
    conflictType: ConflictType.OperationConflict
);
criticalConflict.Severity = ConflictSeverity.Critical;
Console.WriteLine(criticalConflict.ToString()); // "[Critical] Operation Order Conflict between 20240201142015_AddEmailToUsers and 20240202153020_RemoveEmailColumn"
```

## MergeAttempt

```csharp
// Create a merge attempt for a column conflict
var attempt = new MergeAttempt
{
    ConflictId = "conf-20240615-001",
    ConflictType = ConflictType.ColumnConflict,
    StrategyApplied = MergeStrategy.LastWins,
    Succeeded = true,
    MergedContent = "migrationBuilder.AddColumn<int>(\"Age\", \"Users\");",
    AttemptedAt = DateTime.UtcNow
};

// Check if the attempt succeeded
Console.WriteLine($"Attempt succeeded: {attempt.Succeeded}"); // true

// Access the strategy that was applied
Console.WriteLine($"Strategy: {attempt.StrategyApplied}"); // LastWins

// Get the human-readable description
Console.WriteLine(attempt.ToString()); // "[OK] ColumnConflict — resolved via LastWins"

// Create a failed attempt for demonstration
var failedAttempt = new MergeAttempt
{
    ConflictId = "conf-20240615-002",
    ConflictType = ConflictType.TableConflict,
    StrategyApplied = MergeStrategy.Combine,
    Succeeded = false,
    FailureReason = "Incompatible schema changes detected",
    AttemptedAt = DateTime.UtcNow
};

Console.WriteLine(failedAttempt.ToString()); // "[FAIL] TableConflict — Incompatible schema changes detected"
```

## MigrationDiff

The `MigrationDiff` class represents the complete diff result between two branches' migrations. It captures the differences in migration histories, schema changes, and conflicts between source and target branches. This class is used throughout the ef-migration-diff library for comparing migration histories and identifying potential conflicts during branch merges.

Here's an example of how to use the `MigrationDiff` class:

```csharp
// Create a migration diff between two branches
var migrationDiff = new MigrationDiff(sourceBranchId: "main-branch", targetBranchId: "feature-branch")
{
    Id = Guid.NewGuid().ToString(),
    CreatedAt = DateTime.UtcNow
};

// Add migrations that exist only in the source branch
migrationDiff.AddSourceOnlyMigration(new Migration
{
    Id = "20240115093045",
    Name = "CreateUsersTable",
    DbContextName = "ApplicationDbContext",
    Description = "Initial migration creating Users table"
});

// Add migrations that exist only in the target branch  
migrationDiff.AddTargetOnlyMigration(new Migration
{
    Id = "20240116104530",
    Name = "AddRolesTable",
    DbContextName = "ApplicationDbContext",
    Description = "Add Roles table to the database"
});

// Add migrations that exist in both branches
migrationDiff.AddCommonMigration(new Migration
{
    Id = "20240201142015",
    Name = "AddEmailToUsers",
    DbContextName = "ApplicationDbContext",
    Description = "Add email column to Users table"
});

// Add schema changes detected in each branch
migrationDiff.SourceSchemaChanges.Add(new SchemaChange
{
    ChangeType = SchemaChangeType.Added,
    ObjectType = SchemaObjectType.Table,
    ObjectName = "Users",
    Details = "Created Users table"
});

migrationDiff.TargetSchemaChanges.Add(new SchemaChange
{
    ChangeType = SchemaChangeType.Added,
    ObjectType = SchemaObjectType.Column,
    ObjectName = "Users.Email",
    Details = "Added Email column to Users table"
});

// Add a conflict between migrations
migrationDiff.AddConflict(new ConflictInfo(
    firstMigrationId: "20240115093045",
    secondMigrationId: "20240116104530", 
    conflictType: ConflictType.ColumnConflict
)
{
    Description = "Column Email already exists in table Users",
    Severity = ConflictSeverity.Error
});

// Generate summary statistics
migrationDiff.GenerateSummary();

// Check the comparison result
Console.WriteLine($"Result: {migrationDiff.Result}"); // ComparisonResult.Different
Console.WriteLine($"Total schema changes: {migrationDiff.GetTotalSchemaChanges()}"); // 2
Console.WriteLine($"Blocking conflicts: {migrationDiff.HasBlockingConflicts()}"); // false

// Get result description
Console.WriteLine(migrationDiff.GetResultDescription());

// Use ToString() for debugging/logging
Console.WriteLine(migrationDiff.ToString()); // "Diff main-branch..feature-branch: Migrations differ significantly"
```

## MigrationGraphNode

The `MigrationGraphNode` class represents a single node in the migration dependency graph. It captures essential metadata about an Entity Framework Core migration including its identifier, human-readable name, associated DbContext, sequence position, and current status. This class is used throughout the ef-migration-diff library to build dependency graphs that model the relationships between migrations and support topological sorting, cycle detection, and impact analysis.

Here's an example of how to use the `MigrationGraphNode` class:

```csharp
// Create migration nodes for a simple migration chain
var migration1 = new MigrationGraphNode
{
    MigrationId = "20240115093045",
    Name = "CreateUsersTable",
    DbContextName = "ApplicationDbContext",
    Sequence = 1,
    Status = MigrationStatus.Applied
};

var migration2 = new MigrationGraphNode
{
    MigrationId = "20240116104530",
    Name = "AddRolesTable",
    DbContextName = "ApplicationDbContext",
    Sequence = 2,
    Status = MigrationStatus.Pending
};

var migration3 = new MigrationGraphNode
{
    MigrationId = "20240201142015",
    Name = "AddEmailToUsers",
    DbContextName = "ApplicationDbContext",
    Sequence = 3,
    Status = MigrationStatus.Pending
};

// Display node information
Console.WriteLine(migration1.ToString()); // "[0001] 20240115093045 — CreateUsersTable"
Console.WriteLine(migration2.ToString()); // "[0002] 20240116104530 — AddRolesTable"

// Access properties
Console.WriteLine($"Migration ID: {migration1.MigrationId}"); // "20240115093045"
Console.WriteLine($"Context: {migration1.DbContextName}"); // "ApplicationDbContext"
Console.WriteLine($"Sequence: {migration1.Sequence}"); // 1
Console.WriteLine($"Status: {migration1.Status}"); // "Applied"

// Create a graph and add nodes
var graph = new MigrationDependencyGraph();
graph.AddNode(migration1);
graph.AddNode(migration2);
graph.AddNode(migration3);

// Verify nodes were added
Console.WriteLine($"Node count: {graph.Nodes.Count}"); // 3
```

## RequestLoggingMiddleware

The `RequestLoggingMiddleware` class provides request logging functionality for command execution, tracking command invocation details, arguments, execution time, and results. It supports both file-based and console logging with configurable verbosity levels. This middleware is useful for debugging, auditing, and monitoring command execution within the ef-migration-diff library.

Here's an example of how to use the `RequestLoggingMiddleware` class:

```csharp
// Create a console logger with verbose logging
var consoleLogger = new ConsoleLogger();
var consoleMiddleware = new RequestLoggingMiddleware(consoleLogger, isVerbose: true);

// Create a command context
var context = new CommandContext(
    commandName: "migrate",
    rawArguments: new[] { "--source", "main", "--target", "feature-branch" },
    parsedOptions: new Dictionary<string, object> { { "source", "main" }, { "target", "feature-branch" } },
    parsedArguments: new[] { "main", "feature-branch" }
);

// Invoke the middleware
var result = await consoleMiddleware.InvokeAsync(context);

// Output will be logged to console:
// [INFO] [a1b2c3d4] Command started: migrate
// [DEBUG] Arguments: --source, main, --target, feature-branch
// [DEBUG] Parsed options: source=main, target=feature-branch
// [DEBUG] Positional args: main, feature-branch

// Create a file logger for persistent logging
var fileLogger = new FileLogger("./logs/commands.log");
var fileMiddleware = new RequestLoggingMiddleware(fileLogger, isVerbose: false);

// Use the file-based middleware
var fileResult = await fileMiddleware.InvokeAsync(context);
// Logs will be written to ./logs/commands.log with timestamps
```

## BranchInfo

The `BranchInfo` class represents metadata and state information about a Git branch containing Entity Framework Core migrations. It tracks branch identification (Id, BranchName), commit details (CommitHash, CommitMessage, CommitDate, Author), migration content (MigrationIds, DbContexts, MigrationsPath), and remote status (IsRemote). This class is used throughout the ef-migration-diff library for comparing migration histories between branches and analyzing differences in database schema evolution.



Here's an example of how to use the `BranchInfo` class:

```csharp
// Create a BranchInfo instance for a local development branch
var localBranch = new BranchInfo("main")
{
    Id = Guid.NewGuid().ToString(),
    BranchName = "main",
    CommitHash = "a1b2c3d4e5f67890",
    CommitMessage = "Update user model with email validation",
    CommitDate = DateTime.Parse("2024-06-15T10:30:00"),
    Author = "developer@company.com",
    MigrationsPath = @"/src/MyProject/Migrations",
    IsRemote = false
};

// Add migrations to the branch history
localBranch.AddMigration("20240115093045_CreateUsersTable");
localBranch.AddMigration("20240116104530_AddRolesTable");
localBranch.AddMigration("20240201142015_AddEmailToUsers");

// Add DbContexts managed by this branch
localBranch.AddDbContext("ApplicationDbContext");
localBranch.AddDbContext("IdentityDbContext");

// Validate the branch information
var isValid = localBranch.IsValid();
Console.WriteLine($"Branch is valid: {isValid}"); // true

// Get counts
Console.WriteLine($"Migration count: {localBranch.GetMigrationCount()}"); // 3
Console.WriteLine($"DbContext count: {localBranch.GetDbContextCount()}"); // 2

// Check if a migration exists
var hasMigration = localBranch.HasMigration("20240115093045_CreateUsersTable");
Console.WriteLine($"Has migration: {hasMigration}"); // true

// Check if a DbContext exists
var hasContext = localBranch.HasDbContext("ApplicationDbContext");
Console.WriteLine($"Has DbContext: {hasContext}"); // true

// Get a short commit hash for display
var shortHash = localBranch.GetShortCommitHash();
Console.WriteLine($"Short commit hash: {shortHash}"); // "a1b2c3d"

// Create a remote branch instance
var remoteBranch = new BranchInfo("origin/develop")
{
    Id = Guid.NewGuid().ToString(),
    CommitHash = "z9y8x7w6v5u43210",
    CommitMessage = "Fix production deployment issue",
    CommitDate = DateTime.Parse("2024-06-14T16:45:00"),
    Author = "ci@company.com",
    MigrationsPath = @"/src/MyProject/Migrations",
    IsRemote = true
};

// Use ToString() for debugging/logging
Console.WriteLine(localBranch.ToString()); // "main (a1b2c3d) - 3 migrations, 2 contexts"
Console.WriteLine(remoteBranch.ToString()); // "origin/develop (z9y8x7w) - remote branch"
```

## PluginSystem

The `PluginSystem` class provides a flexible plugin architecture for extending the ef-migration-diff library's functionality. It enables dynamic loading, initialization, and execution of plugins that implement the `IPlugin` interface, allowing for custom migration analysis, conflict resolution strategies, and additional processing capabilities without modifying the core library code.

Here's an example of how to use the `PluginSystem` class:

```csharp
// Create a plugin system instance
var pluginSystem = new PluginSystem();

// Load plugins asynchronously from a directory
await pluginSystem.LoadPluginsAsync(@"./Plugins");

// Get statistics about loaded plugins
var stats = pluginSystem.GetStats();
Console.WriteLine($"Total plugins: {stats.TotalPlugins}");
Console.WriteLine($"Plugin names: {string.Join(", ", stats.PluginNames)}");

// Get a specific plugin by name
var myPlugin = pluginSystem.GetPlugin("MyCustomPlugin");
if (myPlugin != null)
{
    Console.WriteLine($"Found plugin: {myPlugin.Name} v{myPlugin.Version} by {myPlugin.Author}");
}

// Get all loaded plugins
var allPlugins = pluginSystem.GetAllPlugins().ToList();
Console.WriteLine($"Loaded {allPlugins.Count} plugins");

// Execute a hook across all plugins
try
{
    await pluginSystem.ExecuteHookAsync("OnMigrationAnalyzed", migration);
}
catch (Exception ex)
{
    Console.WriteLine($"Hook execution failed: {ex.Message}");
}

// Initialize all plugins
foreach (var plugin in allPlugins)
{
    await plugin.InitializeAsync();
}

// Unload all plugins when done
await pluginSystem.UnloadAllAsync();
```

## MigrationFile

```csharp
// Create a migration file instance from a physical file
var migrationFile = new MigrationFile
{
FilePath = @"/home/project/Migrations/20240115093045_CreateUsersTable.cs",
FileName = "20240115093045_CreateUsersTable.cs",
DirectoryPath = @"/home/project/Migrations",
FileSize = 1024,
LastModified = DateTime.Parse("2024-01-15T09:30:45"),
DbContextName = "ApplicationDbContext",
MigrationId = "20240115093045",
IsDesigner = false
};

// Load the content asynchronously
await migrationFile.LoadContentAsync();

// Calculate hash for change detection
migrationFile.CalculateHash();

// Extract migration ID from filename
var extractedId = migrationFile.ExtractMigrationId();
Console.WriteLine($"Extracted Migration ID: {extractedId}"); // "20240115093045"

// Validate the migration file
var isValid = migrationFile.IsValid();
Console.WriteLine($"Is valid: {isValid}");

// Compare content with another migration file
var otherMigrationFile = new MigrationFile
{
FilePath = @"/home/project/Migrations/20240115093045_CreateUsersTable.Designer.cs",
Content = "// Designer file content"
};

var hasSameContent = migrationFile.HasSameContent(otherMigrationFile);
Console.WriteLine($"Has same content: {hasSameContent}");

// Get display path for user-friendly output
var displayPath = migrationFile.GetDisplayPath();
Console.WriteLine($"Display path: {displayPath}");

// Use ToString() for debugging/logging
Console.WriteLine(migrationFile.ToString());

// Create from a migration ID and context name
var newMigrationFile = new MigrationFile("20240115093046", "AddEmailToUsers", "ApplicationDbContext");
Console.WriteLine(newMigrationFile.FileName); // "20240115093046_AddEmailToUsers.cs"
```