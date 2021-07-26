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


## MigrationServicesTests

The `MigrationServicesTests` class provides unit tests for the `MigrationServices` class, which detects and validates changes between Entity Framework Core migrations. It tests various scenarios including table creation conflicts, column modification conflicts, and safe migration execution.

Here's an example of how to use the `MigrationServices` class:

```csharp
// Create a migration services instance
var migrationServices = new MigrationServices();

// Test detecting changes with a CreateTable operation
var createTableChange = migrationServices.DetectChanges_WithCreateTableContent_DetectsOneCreateTableChange();
Assert.Equal(1, createTableChange.Count);

// Test checking if a migration is safe when dropping a table
var isSafe = migrationServices.IsMigrationSafe_WithDropTableContent_ReturnsFalse();
Assert.False(isSafe);

// Test detecting naming conflicts when same table is created with different schemas
var namingConflict = migrationServices.DetectConflicts_WhenSameTableCreatedWithDifferentSchema_ReturnsNamingConflict();
Assert.NotNull(namingConflict);

// Test detecting column conflicts when same column is modified with different default values
var columnConflict = migrationServices.DetectConflicts_WhenSameColumnModifiedWithDifferentDefaultValue_ReturnsColumnConflict();
Assert.NotNull(columnConflict);

// Test that no conflicts are detected when same column is modified with same default value
var noConflicts = migrationServices.DetectConflicts_WhenSameColumnModifiedWithSameDefaultValue_ReturnsNoConflicts();
Assert.Null(noConflicts);

// Test async execution with a registered mocked command
await migrationServices.ExecuteAsync_WithRegisteredMockedCommand_InvokesCommandExactlyOnce();
Assert.True(commandWasInvoked);
```

These tests ensure that migration changes are properly detected, conflicts are identified, and migrations can be safely executed.


