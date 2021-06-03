# Migration
The `Migration` type represents a single Entity Framework Core migration captured by the `ef-migration-diff` library. It encapsulates the migration’s identifier, script content, metadata, and runtime state, allowing consumers to inspect, compare, and manipulate migrations programmatically.

## API
### Properties
- **Id**  
  Gets the unique identifier assigned to the migration (typically a combination of timestamp and name).  
  Type: `string`.  

- **Name**  
  Gets the user‑provided name of the migration.  
  Type: `string`.  

- **Timestamp**  
  Gets the timestamp portion of the migration identifier as a string.  
  Type: `string`.  

- **CreatedAt**  
  Gets the date and time when the migration object was instantiated.  
  Type: `DateTime`.  

- **DbContextName**  
  Gets the name of the `DbContext` associated with this migration.  
  Type: `string`.  

- **Content**  
  Gets the raw SQL or C# script that constitutes the migration’s up/down logic.  
  Type: `string`.  

- **MetadataContent** the migration’s `Up` and `Down` operations.  
  Type: `string`.  

- **MetadataContent**  
  Gets the serialized metadata (model snapshot) associated with the migration.  
  Type: `string`.  

- **Status**  
  Gets the current status of the migration (e.g., `Applied`, `Pending`, `Failed`).  
  Type: `MigrationStatus`.  

- **Description**  
  Gets an optional descriptive comment supplied when the migration was generated.  
  Type: `string`.  

- **Sequence**  
  Gets the ordinal position of the migration within the migration history.  
  Type: `int`.  

- **SchemaChanges**  
  Gets a collection of `SchemaChange` objects describing the individual schema modifications contained in the migration.  
  Type: `List<SchemaChange>`.  

- **DetectedConflicts**  
  Gets a collection of `ConflictInfo` objects representing any conflicts identified when comparing this migration against another source.  
  Type: `List<ConflictInfo>`.  

- **IsValid**  
  Gets a value indicating whether the migration’s content and metadata are internally consistent and can be safely applied.  
  Type: `bool`.  

### Constructors
- **Migration()**  
  Initializes a new, empty instance of the `Migration` class.  

- **Migration(...)**  
  Initializes a new instance of the `Migration` class with the supplied migration data. (Exact parameters are implementation‑specific; the constructor populates all public members.)  

### Methods
- **GenerateTimestamp()**  
  Static method that returns a string suitable for use as a migration timestamp based on the current system date and time.  
  Returns: `string`.  
  Throws: None under normal operation.  

- **Clone()**  
  Creates a deep copy of the current `Migration` instance, duplicating all mutable fields.  
  Returns: `Migration`.  
  Throws: May throw `InvalidOperationException` if the instance is in an inconsistent state that prevents cloning.  

- **GetContentSize()**  
  Returns the size of the migration’s `Content` property, measured in characters.  
  Returns: `int`.  
  Throws: May throw `ArgumentNullException` if `Content` is `null`.  

- **CountStatements()**  
  Parses the `Content` and returns the number of discrete SQL statements it contains.  
  Returns: `int`.  
  Throws: May throw `FormatException` if the content cannot be parsed as valid SQL.  

- **ToString()**  
  Overrides `Object.ToString` to provide a human‑readable representation of the migration, typically including its `Id` and `Name`.  
  Returns: `string`.  
  Throws: None.  

## Usage
```csharp
using EfMigrationDiff;

// Load a migration from a file or database
Migration migration = MigrationLoader.LoadFromFile("20230915120000_AddBlogTable.cs");

// Inspect basic properties
Console.WriteLine($"Id: {migration.Id}");
Console.WriteLine($"Created at: {migration.CreatedAt}");
Console.WriteLine($"Is valid: {migration.IsValid");

// Obtain a copy for safe manipulation
Migration clone = migration.Clone();
clone.Description = "Updated description for testing";
```
```csharp
using System.Linq;
using EfMigrationDiff;

// Compare two migrations and report conflicts
Migration left  = MigrationLoader.LoadFromDatabase(context, "20230915120000_AddBlogTable");
Migration right = MigrationLoader.LoadFromFile("20230915120000_AddBlogTable_v2.cs");

if (!left.IsValid || !right.IsValid)
{
    throw new InvalidOperationException("One of the migrations is invalid.");
}

var conflicts = left.DetectedConflicts.Concat(right.DetectedConflicts).ToList();
if (conflicts.Any())
{
    Console.WriteLine($"Detected {conflicts.Count} conflicts:");
    foreach (var c in conflicts)
    {
        Console.WriteLine($"- {c.Description}");
    }
}
else
{
    Console.WriteLine("No conflicts detected.");
}
```
## Notes
- The `Content` and `MetadataContent` properties may be `null` for migrations that have not yet been materialized; members that depend on these strings (`GetContentSize`, `CountStatements`, `Clone`) will throw exceptions when invoked on a `null` value.  
- `IsValid` is computed lazily; accessing it after mutating `Content` or `MetadataContent` may yield a different result without throwing.  
- The type does not implement any synchronization primitives. Instances are safe to read concurrently from multiple threads, but mutating an instance from more than one thread at a time requires external locking.  
- `GenerateTimestamp` relies on `DateTime.UtcNow`; in environments with altered system clocks the returned string may not be monotonic.  
- `SchemaChanges` and `DetectedConflicts` are returned as mutable lists; altering the contents of these lists directly affects the migration object's state.  
- The two constructors allow creation of a blank migration (useful for scaffolding) or initialization from existing data; the parameter‑rich constructor does not perform validation—callers should verify `IsValid` after construction if correctness is required.
