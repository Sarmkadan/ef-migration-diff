## MigrationParserServiceTests

The `MigrationParserServiceTests` class provides a set of unit tests for the `MigrationParserService` class, which parses Entity Framework Core migration files to extract metadata such as migration ID, name, and content. It tests various scenarios including valid migration files, designer files, invalid timestamps, empty content, and complex migration scenarios.

Here's an example of how to use the `MigrationParserService` class:

```csharp
// Create a parser instance
var parser = new MigrationParserService();

// Parse a valid migration file
var migrationFile = new MigrationFile
{
    FileName = "20240115093045_CreateUsersTable.cs",
    Content = "migrationBuilder.CreateTable(name: \"Users\", table => new { Id = table.Column<int>() })",
    DbContextName = "ApplicationDbContext"
};

var migration = parser.ParseMigrationFile(migrationFile);

// Extract migration metadata
Console.WriteLine($"Migration ID: {migration.Id}");           // "20240115093045"
Console.WriteLine($"Migration Name: {migration.Name}");       // "CreateUsersTable"
Console.WriteLine($"DbContext: {migration.DbContextName}");    // "ApplicationDbContext"
Console.WriteLine($"Content Length: {migration.Content.Length}");

// Parse a designer file (extracts the same migration ID)
var designerFile = new MigrationFile
{
    FileName = "20240115093045_CreateUsersTable.Designer.cs",
    Content = "// Designer file content",
    DbContextName = "ApplicationDbContext"
};

var designerMigration = parser.ParseMigrationFile(designerFile);
Console.WriteLine(designerMigration.Id); // "20240115093045"

// Handle invalid timestamp
var invalidFile = new MigrationFile
{
    FileName = "InvalidTimestamp_CreateUsersTable.cs",
    Content = "migrationBuilder.CreateTable(...)",
    DbContextName = "ApplicationDbContext"
};

var invalidResult = parser.ParseMigrationFile(invalidFile);
Console.WriteLine(invalidResult); // null

// Parse empty content migration
var emptyFile = new MigrationFile
{
    FileName = "20240115093045_EmptyMigration.cs",
    Content = string.Empty,
    DbContextName = "ApplicationDbContext"
};

var emptyMigration = parser.ParseMigrationFile(emptyFile);
Console.WriteLine(emptyMigration.Content); // ""
```

These tests ensure that the migration parser correctly extracts metadata from various migration file formats and handles edge cases appropriately.


