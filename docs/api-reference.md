# API Reference

Complete API documentation for ef-migration-diff.

## Core Services

### MigrationDiffService

Main service for comparing migrations between Git branches.

```csharp
public class MigrationDiffService
{
    public async Task<MigrationDiff> CompareBranchesAsync(
        string branch1,
        string branch2,
        ComparisonOptions? options = null,
        CancellationToken cancellationToken = default)
    
    public async Task<List<Migration>> GetMigrationsAsync(
        string branch,
        CancellationToken cancellationToken = default)
    
    public async Task<bool> ValidateMigrationsAsync(
        string branch,
        bool strict = false,
        CancellationToken cancellationToken = default)
}
```

#### Example Usage

```csharp
var service = serviceProvider.GetRequiredService<MigrationDiffService>();

var result = await service.CompareBranchesAsync(
    branch1: "main",
    branch2: "feature/users",
    options: new ComparisonOptions
    {
        IncludeSchemaPreview = true,
        DetectBreakingChanges = true
    });

if (result.HasConflicts)
{
    foreach (var conflict in result.Conflicts)
    {
        Console.WriteLine($"Conflict: {conflict.MigrationName}");
    }
}
```

### ConflictDetectionService

Detects conflicts between migration sets.

```csharp
public class ConflictDetectionService
{
    public async Task<List<ConflictInfo>> DetectConflictsAsync(
        List<Migration> migrations1,
        List<Migration> migrations2,
        CancellationToken cancellationToken = default)
    
    public async Task<List<string>> GetConflictingNamesAsync(
        List<Migration> migrations1,
        List<Migration> migrations2)
    
    public async Task<List<ConflictInfo>> CheckDependenciesAsync(
        List<Migration> migrations)
}
```

#### Conflict Types

- `DuplicateNames`: Migrations with identical names
- `DependencyIssue`: Broken or circular dependencies
- `OrphanedMigration`: Migration without parent reference
- `IncompatibleChange`: Incompatible schema modifications

#### Example Usage

```csharp
var service = serviceProvider.GetRequiredService<ConflictDetectionService>();

var migrations1 = await migrationRepo.GetMigrationsAsync("main");
var migrations2 = await migrationRepo.GetMigrationsAsync("feature/users");

var conflicts = await service.DetectConflictsAsync(migrations1, migrations2);

var criticalConflicts = conflicts
    .Where(c => c.Severity == ConflictSeverity.Critical)
    .ToList();
```

### SchemaChangeDetectorService

Analyzes schema changes between migration sets.

```csharp
public class SchemaChangeDetectorService
{
    public async Task<List<SchemaChange>> DetectChangesAsync(
        List<Migration> baseMigrations,
        List<Migration> newMigrations,
        CancellationToken cancellationToken = default)
    
    public async Task<List<SchemaChange>> DetectBreakingChangesAsync(
        List<SchemaChange> changes)
    
    public async Task<SchemaPreview> GenerateSchemaPreviewAsync(
        List<Migration> migrations,
        CancellationToken cancellationToken = default)
}
```

#### Operation Types

- `CreateTable`: New table creation
- `DropTable`: Table deletion
- `AddColumn`: New column addition
- `DropColumn`: Column deletion
- `AlterColumn`: Column modification
- `AddIndex`: Index creation
- `DropIndex`: Index deletion
- `AddConstraint`: Constraint addition
- `DropConstraint`: Constraint removal

#### Example Usage

```csharp
var service = serviceProvider.GetRequiredService<SchemaChangeDetectorService>();

var changes = await service.DetectChangesAsync(
    baseMigrations,
    newMigrations);

var breakingChanges = await service.DetectBreakingChangesAsync(changes);

if (breakingChanges.Any())
{
    Console.WriteLine("⚠️  Breaking changes detected!");
}
```

### MigrationParserService

Parses and extracts information from migration files.

```csharp
public class MigrationParserService
{
    public async Task<Migration> ParseMigrationAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    
    public string ExtractMigrationName(string filePath)
    
    public async Task<List<MigrationOperation>> ExtractOperationsAsync(
        string migrationCode)
    
    public async Task<List<string>> ExtractDependenciesAsync(
        string migrationCode)
}
```

#### Example Usage

```csharp
var parser = serviceProvider.GetRequiredService<MigrationParserService>();

var migration = await parser.ParseMigrationAsync(
    "./Migrations/20250504120000_AddUsers.cs");

Console.WriteLine($"Migration: {migration.Name}");
Console.WriteLine($"Dependencies: {string.Join(", ", migration.Dependencies)}");
```

### ReportGenerationService

Generates various report formats.

```csharp
public class ReportGenerationService
{
    public async Task<string> GenerateHtmlReportAsync(
        MigrationDiff diff,
        ReportOptions? options = null)
    
    public async Task<string> GenerateJsonReportAsync(
        MigrationDiff diff)
    
    public async Task<string> GenerateCsvReportAsync(
        MigrationDiff diff)
    
    public async Task<string> GenerateMarkdownReportAsync(
        MigrationDiff diff)
}
```

#### ReportOptions

```csharp
public class ReportOptions
{
    public bool IncludeTimestamp { get; set; }
    public bool IncludeStatistics { get; set; }
    public bool IncludeRecommendations { get; set; }
    public string? Title { get; set; }
    public string? Theme { get; set; } // "light" or "dark"
    public bool IncludeTableOfContents { get; set; }
}
```

#### Example Usage

```csharp
var service = serviceProvider.GetRequiredService<ReportGenerationService>();

var htmlReport = await service.GenerateHtmlReportAsync(
    diff,
    new ReportOptions
    {
        IncludeTimestamp = true,
        IncludeStatistics = true,
        Theme = "light"
    });

await File.WriteAllTextAsync("report.html", htmlReport);
```

## Data Models

### Migration

```csharp
public class Migration
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? ParentId { get; set; }
    public List<string> Dependencies { get; set; }
    public string Content { get; set; }
    public MigrationStatus Status { get; set; }
}
```

### MigrationDiff

```csharp
public class MigrationDiff
{
    public List<ConflictInfo> Conflicts { get; set; }
    public List<SchemaChange> SchemaChanges { get; set; }
    public List<Migration> AddedMigrations { get; set; }
    public List<Migration> RemovedMigrations { get; set; }
    public List<Migration> ModifiedMigrations { get; set; }
    public DateTime GeneratedAt { get; set; }
    
    public bool HasDifferences => 
        Conflicts.Any() || SchemaChanges.Any() || 
        AddedMigrations.Any() || RemovedMigrations.Any();
}
```

### ConflictInfo

```csharp
public class ConflictInfo
{
    public string MigrationName { get; set; }
    public string ConflictType { get; set; }
    public ConflictSeverity Severity { get; set; }
    public string Description { get; set; }
    public List<string> AffectedMigrations { get; set; }
    public string? Recommendation { get; set; }
}

public enum ConflictSeverity
{
    Info,
    Warning,
    Critical
}
```

### SchemaChange

```csharp
public class SchemaChange
{
    public string TableName { get; set; }
    public string OperationType { get; set; }
    public string? ColumnName { get; set; }
    public string? ColumnType { get; set; }
    public bool? IsNullable { get; set; }
    public bool? HasDefault { get; set; }
    public List<string> AffectedColumns { get; set; }
    public bool IsBreakingChange { get; set; }
}
```

## Repositories

### GitRepository

```csharp
public class GitRepository
{
    public async Task<List<string>> GetAllBranchesAsync()
    
    public async Task<string> GetFileContentAsync(
        string branch,
        string filePath)
    
    public async Task<List<string>> GetMigrationFilesAsync(string branch)
    
    public async Task CheckoutBranchAsync(string branch)
    
    public async Task<bool> BranchExistsAsync(string branch)
}
```

### MigrationRepository

```csharp
public class MigrationRepository
{
    public async Task<List<Migration>> GetMigrationsAsync(string branch)
    
    public async Task<Migration?> GetMigrationByNameAsync(
        string branch,
        string migrationName)
    
    public async Task<List<string>> GetMigrationFilePathsAsync(string branch)
    
    public async Task<MigrationFile> LoadMigrationFileAsync(string filePath)
}
```

### DbContextRepository

```csharp
public class DbContextRepository
{
    public async Task<DbContextMetadata> GetDbContextMetadataAsync(
        string projectPath)
    
    public List<Type> DiscoverDbContextTypes()
    
    public async Task<SchemaMetadata> ExtractSchemaMetadataAsync(
        DbContext context)
}
```

## Extension Points

### Custom Analysis

Implement `IAnalyzer` interface:

```csharp
public interface IAnalyzer
{
    Task<List<ConflictInfo>> AnalyzeAsync(
        List<Migration> migrations1,
        List<Migration> migrations2);
}

public class CustomAnalyzer : IAnalyzer
{
    public async Task<List<ConflictInfo>> AnalyzeAsync(
        List<Migration> migrations1,
        List<Migration> migrations2)
    {
        // Custom analysis logic
        return new List<ConflictInfo>();
    }
}
```

### Custom Formatters

Implement `IFormatter` interface:

```csharp
public interface IFormatter
{
    string Format(MigrationDiff diff);
}

public class CustomFormatter : IFormatter
{
    public string Format(MigrationDiff diff)
    {
        // Custom formatting logic
        return "formatted output";
    }
}
```

## Constants

### Migration Status

```csharp
public enum MigrationStatus
{
    Pending,
    Applied,
    Failed,
    Skipped,
    Unknown
}
```

## Error Handling

### Custom Exceptions

```csharp
public class MigrationConflictException : Exception
{
    public List<ConflictInfo> Conflicts { get; }
}

public class InvalidMigrationException : Exception
{
    public string MigrationName { get; }
}

public class GitOperationException : Exception
{
    public string Operation { get; }
}
```

## Async/Await

All I/O operations support:
- `CancellationToken` for graceful cancellation
- `async/await` for non-blocking operations
- Timeout configuration per operation

Example with cancellation:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

try
{
    var result = await service.CompareBranchesAsync(
        "main",
        "feature/users",
        cancellationToken: cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation timed out");
}
```

## Dependency Injection Registration

```csharp
var services = new ServiceCollection();

// Register all services
services.AddScoped<MigrationDiffService>();
services.AddScoped<ConflictDetectionService>();
services.AddScoped<SchemaChangeDetectorService>();
services.AddScoped<MigrationParserService>();
services.AddScoped<ReportGenerationService>();
services.AddScoped<GitRepository>();
services.AddScoped<MigrationRepository>();
services.AddScoped<DbContextRepository>();

// Add configuration
services.AddSingleton(configuration);

var provider = services.BuildServiceProvider();
```
