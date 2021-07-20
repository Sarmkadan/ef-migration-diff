## EfMigrationDiff

A library for analyzing and comparing database migrations in .NET applications.

## EfMigrationDiffException

The `EfMigrationDiffException` class is a custom exception type used to represent errors that occur during the migration diff process. It provides additional information about the error, such as the file path and line number where the error occurred.

Here's an example of how to use this exception:

```csharp
try
{
    // code that may throw EfMigrationDiffException
}
catch (EfMigrationDiffException ex)
{
    Console.WriteLine($"Error occurred at file path: {ex.FilePath}, line number: {ex.LineNumber}");
    Console.WriteLine(ex.Message);
}
```

This exception can be thrown in various scenarios, such as when there's a conflict between migrations or when there's an issue parsing migration data.

```csharp
throw new EfMigrationDiffException("Migration conflict detected", new List<string> { "conflict1", "conflict2" });
throw new EfMigrationDiffException("Error parsing migration data", "path/to/migration/file.cs", 10);
```

These exceptions can be caught and handled accordingly to provide a better user experience and to facilitate debugging.

## MigrationDiffExtensions

The `MigrationDiffExtensions` class provides a set of extension methods for working with migration data. These methods allow you to analyze and summarize migration information, such as the total number of migrations, migrations needing attention, and schema changes.

Here's an example of how to use these extension methods:

```csharp
var migrationDiff = new[] { /* some migration data */ };
var totalMigrations = MigrationDiffExtensions.GetTotalMigrations(migrationDiff);
var migrationsNeedingAttention = MigrationDiffExtensions.GetMigrationsNeedingAttention(migrationDiff);
var hasDestructiveChanges = MigrationDiffExtensions.HasDestructiveChanges(migrationDiff);

Console.WriteLine($"Total Migrations: {totalMigrations}");
Console.WriteLine($"Migrations Needing Attention: {migrationsNeedingAttention}");
Console.WriteLine($"Has Destructive Changes: {hasDestructiveChanges}");

var summary = MigrationDiffExtensions.GetFormattedSummary(migrationDiff);
Console.WriteLine(summary);
```

These extension methods can be used to gain insights into migration data and make it easier to work with migration information in your application.

## MigrationDiffServiceExtensions

The `MigrationDiffServiceExtensions` class provides extension methods for analyzing migration differences and generating reports. It includes methods for checking destructive changes, generating quick and conflict reports, finding common migration names, and determining if migrations can be merged safely.

Example usage:

```csharp
var migrationDiff = new[] { /* some migration data */ };
var migrations1 = new[] { "Migration1", "Migration2" };
var migrations2 = new[] { "Migration2", "Migration3" };

bool hasDestructive = MigrationDiffServiceExtensions.HasDestructiveChanges(migrationDiff);
string quickReport = MigrationDiffServiceExtensions.GenerateQuickReport(migrationDiff);
List<string> commonMigrations = MigrationDiffServiceExtensions.GetCommonMigrationNames(migrations1, migrations2);
string conflictReport = MigrationDiffServiceExtensions.GenerateConflictReport(migrationDiff);
bool canMerge = MigrationDiffServiceExtensions.CanMergeSafely(migrationDiff);

Console.WriteLine($"Has Destructive Changes: {hasDestructive}");
Console.WriteLine($"Quick Report:\n{quickReport}");
Console.WriteLine($"Common Migrations: {string.Join(", ", commonMigrations)}");
Console.WriteLine($"Conflict Report:\n{conflictReport}");
Console.WriteLine($"Can Merge Safely: {canMerge}");
```

These methods help streamline migration analysis by providing actionable insights and reports for different scenarios.

## MigrationAutoResolverServiceExtensions

The `MigrationAutoResolverServiceExtensions` class provides configuration and resolution capabilities for automated migration conflict resolution. It supports strategy-based conflict resolution patterns like skip, first-wins, last-wins, and combine strategies. The service can be configured with custom logging and reset to default settings.

Example usage:
```csharp
var resolver = MigrationAutoResolverServiceExtensions.ConfigureFirstWinsStrategy()
    .CreateWithLogger(new ConsoleLogger())
    .ConfigureCombineStrategy();

bool success = await resolver.TryResolveAllAsync(migrationConflicts);
var currentStrategy = resolver.GetConfiguredStrategy();
Console.WriteLine($"Current strategy: {currentStrategy.Name}");

if (!success)
{
    resolver.ResetToDefaults();
    // Re-attempt resolution with default settings
}
```

This example demonstrates configuring a resolver with a combination of strategies, setting up logging, resolving conflicts, and handling fallback scenarios.