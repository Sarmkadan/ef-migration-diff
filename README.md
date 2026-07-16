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

Here's an example of how to use the `MigrationFile` class:

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