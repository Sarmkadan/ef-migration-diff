# Architecture Guide

This document explains the internal architecture and design of ef-migration-diff.

## Overview

ef-migration-diff is built using a layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────┐
│             CLI / Entry Point                       │
│  (Program.cs, CommandParser, CommandExecutor)      │
└─────────────────────┬───────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────┐
│         Business Logic Layer                        │
│  • MigrationDiffService                            │
│  • ConflictDetectionService                        │
│  • SchemaChangeDetectorService                     │
│  • MigrationParserService                          │
│  • ReportGenerationService                         │
└─────────────────────┬───────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────┐
│         Data Access Layer                           │
│  • GitRepository                                    │
│  • MigrationRepository                             │
│  • DbContextRepository                             │
└──────────┬──────────────────┬──────────────┬───────┘
           │                  │              │
    ┌──────▼──┐      ┌───────▼──┐   ┌──────▼──┐
    │ Git Ops │      │ Migrations│   │DbContext│
    │         │      │ Files     │   │Classes  │
    └─────────┘      └───────────┘   └─────────┘
```

## Layer Responsibilities

### CLI Layer (Presentation)

**Files**: `Program.cs`, `CLI/CommandParser.cs`, `CLI/CommandExecutor.cs`, `CLI/Commands/`

Responsibilities:
- Parse command-line arguments
- Validate user input
- Format and display output
- Handle CLI context and state

Key classes:
- `CommandParser`: Converts CLI arguments into command objects
- `CommandExecutor`: Executes parsed commands
- `CompareCommand`, `ValidateCommand`, `HelpCommand`: Specific command implementations

### Business Logic Layer (Services)

**Files**: `Services/`, `Analysis/`

#### MigrationDiffService
Orchestrates the comparison process:
1. Gets migrations from both branches
2. Parses migration files
3. Detects conflicts
4. Analyzes schema changes
5. Generates results

#### ConflictDetectionService
Identifies conflicts between migrations:
- Duplicate names
- Dependency issues
- Orphaned migrations
- Incompatible changes

#### SchemaChangeDetectorService
Analyzes schema operations:
- Extracts SQL operations from migrations
- Categorizes changes (CREATE, ALTER, DROP)
- Detects breaking changes
- Identifies data loss scenarios

#### MigrationParserService
Parses migration C# files:
- Extracts migration metadata
- Reads Up/Down methods
- Identifies dependencies
- Builds migration graph

#### ReportGenerationService
Creates output reports:
- HTML formatting with charts
- JSON serialization
- CSV export
- Console formatting

### Data Access Layer (Repositories)

**Files**: `Repositories/`

#### GitRepository
Interacts with Git:
- Checks out branches
- Reads file contents
- Lists branches
- Gets commit history

#### MigrationRepository
Manages migration files:
- Scans migration directories
- Loads migration metadata
- Tracks migration history
- Validates file structure

#### DbContextRepository
Discovers and analyzes DbContext:
- Finds DbContext classes
- Extracts entity configurations
- Builds schema metadata
- Analyzes relationships

## Key Design Patterns

### Dependency Injection

The application uses Microsoft.Extensions.DependencyInjection for IoC:

```csharp
var services = new ServiceCollection();
services.AddScoped<MigrationDiffService>();
services.AddScoped<GitRepository>();
// ...
var provider = services.BuildServiceProvider();
```

### Repository Pattern

Data access is abstracted through repositories:
- `IRepository<T>` interface (implicit)
- Concrete implementations: `GitRepository`, `MigrationRepository`
- Allows testing with mock repositories

### Strategy Pattern

Different comparison strategies:
- `ConflictDetectionService`: Conflict detection strategy
- `SchemaChangeDetectorService`: Schema analysis strategy
- Formatters: Output formatting strategies

### Pipeline Pattern

Migration analysis follows a pipeline:
1. **Load**: Fetch migrations from Git
2. **Parse**: Extract metadata from files
3. **Analyze**: Detect conflicts and schema changes
4. **Report**: Format and output results

## Data Models

### Core Models

**Migration**
- Name (e.g., "20250504_AddUsers")
- Timestamp
- Dependencies
- Content (C# code)

**MigrationDiff**
- Conflicts: List of conflicts found
- SchemaChanges: List of schema operations
- AddedMigrations: New migrations in branch2
- RemovedMigrations: Deleted migrations in branch1

**ConflictInfo**
- MigrationName: Which migration has the conflict
- ConflictType: "DuplicateNames", "DependencyIssue", etc.
- Severity: Critical, Warning, Info
- Description: Human-readable explanation

**SchemaChange**
- TableName: Affected table
- OperationType: CreateTable, DropColumn, AlterColumn, etc.
- ColumnName: Column affected (if applicable)
- ColumnType: Data type changes
- IsNullable: Nullable property changes

## Extension Points

### Custom Plugins

Extend via plugin system:

```csharp
var pluginSystem = new PluginSystem();
pluginSystem.LoadPlugin("./plugins/my-analyzer.dll");
```

### Custom Formatters

Add custom output formats:

```csharp
public class CustomFormatter : IFormatter
{
    public string Format(MigrationDiff diff) { /* ... */ }
}
```

### Custom Analysis Rules

Implement custom analysis:

```csharp
public class CustomAnalyzer : IAnalyzer
{
    public List<ConflictInfo> Analyze(List<Migration> migrations)
    { /* ... */ }
}
```

## Performance Considerations

### Caching

- Results cached by branch pair and timestamp
- TTL configurable via `CacheTtlSeconds`
- Reduces repeated analysis of same branches

### Background Processing

- Long-running operations use `BackgroundTaskQueue`
- Non-blocking analysis for large migrations
- Status tracking for async operations

### Optimization Techniques

- Lazy-load migration content
- Stream-based report generation for large datasets
- Efficient reflection-based DbContext discovery
- Parallel conflict detection when possible

## Error Handling

### Custom Exceptions

**MigrationConflictException**
- Thrown when migration conflicts detected in strict mode

**InvalidMigrationException**
- Thrown for malformed migration files

**GitOperationException**
- Thrown for Git-related failures

### Middleware

Error handling middleware:
- `ErrorHandlingMiddleware`: Catches and logs all exceptions
- `ValidationMiddleware`: Validates input before processing
- `RequestLoggingMiddleware`: Tracks request flow for debugging

## Testing Strategy

### Unit Tests
- Service logic
- Parser functionality
- Report generation

### Integration Tests
- End-to-end comparison workflows
- Git repository operations
- File system interactions

### Example Structure
```
Tests/
  ├── Services/
  │   ├── MigrationDiffServiceTests.cs
  │   ├── ConflictDetectionTests.cs
  │   └── SchemaChangeDetectorTests.cs
  ├── Repositories/
  │   └── MigrationRepositoryTests.cs
  └── Integration/
      └── EndToEndTests.cs
```

## Configuration Architecture

### Layered Configuration

1. **Default values** in code
2. **appsettings.json** overrides defaults
3. **Environment variables** override JSON
4. **Command-line arguments** highest priority

Example priority:
```
CLI args > Env vars > appsettings.json > Code defaults
```

### Configuration Schema

```json
{
  "EfMigrationDiff": {
    "DefaultOutputFormat": "console",
    "MigrationsPath": "Migrations",
    "CacheEnabled": true,
    "CacheTtlSeconds": 3600,
    "StrictModeDefault": false,
    "Logging": { "LogLevel": "Information" },
    "GitHub": { "Enabled": false, "ApiUrl": "https://api.github.com" },
    "Performance": {
      "MaxConcurrentAnalysis": 4,
      "EnableCaching": true,
      "CacheDirectory": ".cache"
    }
  }
}
```

## Future Architecture Considerations

### Planned Enhancements

1. **Plugin System v2**: Enhanced plugin discovery and versioning
2. **Distributed Caching**: Redis support for distributed teams
3. **Database Backends**: Support for different database provider-specific analysis
4. **Real-time Monitoring**: WebSocket support for live analysis updates
5. **Advanced Reporting**: Integration with BI tools and dashboards

### Scalability Roadmap

- Support for monorepos with multiple DbContexts
- Parallel migration analysis across CPU cores
- Streaming report generation for 10k+ migrations
- Cloud storage integration for reports
