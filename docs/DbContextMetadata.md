# DbContextMetadata

Represents metadata collected for a DbContext during migration analysis, storing identifiers, provider information, and collections of migrations, entity types, and custom properties.

## API

### Id
**Purpose:** Unique identifier for the metadata instance.  
**Type:** `string`  
**Remarks:** Should be set to a value that distinguishes this metadata from others (e.g., a GUID). Getting or setting this property does not throw exceptions.

### ContextName
**Purpose:** Name of the DbContext type.  
**Type:** `string`  
**Remarks:** Typically matches the class name of the DbContext. No validation is performed by the property.

### AssemblyName
**Purpose:** Name of the assembly containing the DbContext.  
**Type:** `string`  
**Remarks:** Useful for locating the context at runtime. No exceptions are thrown on get/set.

### Namespace
**Purpose:** Namespace of the DbContext type.  
**Type:** `string`  
**Remarks:** May be empty if the context resides in the global namespace. No exceptions are thrown.

### DatabaseProvider
**Purpose:** Identifier of the EF Core database provider in use (e.g., `Microsoft.EntityFrameworkCore.SqlServer`).  
**Type:** `string`  
**Remarks:** Reflects the provider used when the metadata was scanned. No validation is performed.

### ConnectionString
**Purpose:** Connection string used to connect to the database.  
**Type:** `string`  
**Remarks:** May contain sensitive information; handle according to security policies. No exceptions are thrown on get/set.

### MigrationIds
**Purpose:** Collection of migration identifiers associated with the context.  
**Type:** `List<string>`  
**Remarks:** The list is expected to be initialized by the constructor; accessing it before initialization may throw a `NullReferenceException`. Adding duplicates is allowed unless prevented by custom logic.

### EntityTypes
**Purpose:** Collection of CLR type names representing entity types in the model.  
**Type:** `List<string>`  
**Remarks:** Similar to `MigrationIds`, the list is initialized by the constructor. Access before initialization may throw `NullReferenceException`.

### Properties
**Purpose:** Arbitrary key‑value pairs attached to the metadata.  
**Type:** `Dictionary<string, string>`  
**Remarks:** The dictionary is initialized by the constructor. Attempting to access a non‑existent key returns `null`; attempting to add a `null` key throws `ArgumentNullException`.

### LastScannedAt
**Purpose:** Timestamp of the last time the metadata was scanned or refreshed.  
**Type:** `DateTime`  
**Remarks:** Defaults to `DateTime.MinValue` if never set. No exceptions are thrown.

### IsValid
**Purpose:** Indicates whether the metadata instance contains sufficient data to be considered valid.  
**Type:** `bool`  
**Remarks:** Returns `true` when `Id`, `ContextName`, and `DatabaseProvider` are non‑null and non‑empty; otherwise returns `false`. This property does not throw.

### DbContextMetadata (constructor)
**Purpose:** Initializes a new instance of the `DbContextMetadata` class with default values.  
**Parameters:** none  
**Remarks:** The constructor initializes `MigrationIds`, `EntityTypes`, and `Properties` to empty collections. No exceptions are thrown under normal conditions.

### DbContextMetadata (constructor)
**Purpose:** Provides an alternative constructor (typically used for copying or deserialization).  
**Parameters:** none  
**Remarks:** Behaves similarly to the parameterless constructor; the exact semantics depend on the overload defined in the source. No exceptions are thrown under normal conditions.

### AddMigration
**Purpose:** Adds a migration identifier to the `MigrationIds` collection.  
**Parameters:** none  
**Return:** `void`  
**Throws:**  
- `InvalidOperationException` if `MigrationIds` is `null`.  
- `ArgumentException` if the internal logic determines the migration cannot be added (e.g., duplicate not allowed).

### AddEntityType
**Purpose:** Adds an entity type name to the `EntityTypes` collection.  
**Parameters:** none  
**Return:** `void`  
**Throws:**  
- `InvalidOperationException` if `EntityTypes` is `null`.  
- `ArgumentException` if the entity type cannot be added (e.g., duplicate not allowed).

### AddProperty
**Purpose:** Adds a key‑value pair to the `Properties` dictionary.  
**Parameters:** none  
**Return:** `void`  
**Throws:**  
- `InvalidOperationException` if `Properties` is `null`.  
- `ArgumentNullException` if the key or value to be added is `null`.  
- `ArgumentException` if the key already exists and the implementation disallows overwriting.

### GetProperty
**Purpose:** Retrieves a value from the `Properties` dictionary.  
**Parameters:** none  
**Return:** `string` – the value associated with the internal key, or `null` if not present.  
**Throws:**  
- `InvalidOperationException` if `Properties` is `null`.  
- `ArgumentNullException` if the internal key is `null`.

### GetMigrationCount
**Purpose:** Returns the number of migrations in the `MigrationIds` collection.  
**Parameters:** none  
**Return:** `int` – count of migration identifiers.  
**Throws:**  
- `InvalidOperationException` if `MigrationIds` is `null`.

### GetEntityTypeCount
**Purpose:** Returns the number of entity types in the `EntityTypes` collection.  
**Parameters:** none  
**Return:** `int` – count of entity type names.  
**Throws:**  
- `InvalidOperationException` if `EntityTypes` is `null`.

### HasMigration
**Purpose:** Determines whether a specific migration identifier is present in the `MigrationIds` collection.  
**Parameters:** none  
**Return:** `bool` – `true` if the migration exists; otherwise `false`.  
**Throws:**  
- `InvalidOperationException` if `MigrationIds` is `null`.  
- `ArgumentNullException` if the internal migration identifier to check is `null`.

## Usage

```csharp
using System;
using System.Collections.Generic;

// Example 1: Creating and populating metadata
var metadata = new DbContextMetadata
{
    Id = Guid.NewGuid().ToString(),
    ContextName = "BloggingContext",
    AssemblyName = "MyApp.Data",
    Namespace = "MyApp.Data",
    DatabaseProvider = "Microsoft.EntityFrameworkCore.SqlServer",
    ConnectionString = "Server=.;Database=Blogging;Trusted_Connection=True;",
    LastScannedAt = DateTime.UtcNow
};

metadata.AddMigration(); // assumes internal state provides migration id
metadata.AddEntityType(); // assumes internal state provides entity type name
metadata.AddProperty();   // assumes internal state provides key/value

Console.WriteLine($"Migration count: {metadata.GetMigrationCount()}");
Console.WriteLine($"Is valid: {metadata.IsValid}");
```

```csharp
using System;
using System.Linq;

// Example 2: Reading metadata and checking for a migration
DbContextMetadata metadata = GetMetadataFromSomeSource(); // hypothetical helper

if (metadata.HasMigration())
{
    var firstMigration = metadata.MigrationIds.FirstOrDefault();
    Console.WriteLine($"First migration: {firstMigration}");
}
else
{
    Console.WriteLine("No migrations recorded.");
}

// Safe iteration despite possible nulls
foreach (var entity in metadata.EntityTypes ?? Enumerable.Empty<string>())
{
    Console.WriteLine($"Entity: {entity}");
}
```

## Notes

- The collections (`MigrationIds`, `EntityTypes`, `Properties`) are initialized to empty instances by the constructors; however, if they are explicitly set to `null` later, the accessor methods will throw `InvalidOperationException`.  
- The type does **not** provide built‑in thread safety. Concurrent modifications to the collections or properties from multiple threads require external synchronization (e.g., locking or using concurrent collections).  
- `Id`, `ContextName`, `AssemblyName`, `Namespace`, `DatabaseProvider`, and `ConnectionString` are simple get/set properties; they perform no validation, so callers must ensure meaningful values are supplied if validation is required elsewhere.  
- The `Add*` and `Get*` methods rely on internal state that is not exposed in the public API; therefore, their behavior (such as what identifier or key/value they operate on) depends on how the instance is prepared before invocation.  
- `IsValid` does not consider the contents of the collections; a metadata instance can be valid while having zero migrations or entity types.  
- The two constructors both produce a new instance; the second overload is intended for scenarios such as copying an existing metadata object or deserialization, but without parameter details the exact semantics should be verified against the source code.  
- All string members accept `null` values; however, setting them to `null` may cause `IsValid` to return `false` and may lead to `NullReferenceException` if the code assumes non‑null strings when accessing members like `Id.Length`.  
- No members are marked obsolete or deprecated members are present in the current set; future versions may introduce additional validation or overloads.
