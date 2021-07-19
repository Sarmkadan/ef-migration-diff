// ... existing content ...

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
