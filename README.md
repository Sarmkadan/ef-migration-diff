// ... existing content ...

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
```