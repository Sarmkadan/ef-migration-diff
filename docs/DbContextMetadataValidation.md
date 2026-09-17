# DbContextMetadataValidation

Provides validation and validation-related extension methods for `DbContextMetadata`. It is a static class that extends the metadata model with three entry points: `Validate`, `IsValid`, and `EnsureValid`.

## API

### Validate
**Purpose:** Validates a `DbContextMetadata` instance and returns a read-only list of human-readable validation problems. Returns an empty list when the instance is valid.
**Signature:** `IReadOnlyList<string> Validate(this DbContextMetadata value)`
**Throws:**
- `ArgumentNullException` if `value` is `null`.

The method never throws for invalid data; it accumulates every problem it finds and returns them all in a single list.

### IsValid
**Purpose:** Determines whether a `DbContextMetadata` instance is valid.
**Signature:** `bool IsValid(this DbContextMetadata value)`
**Return:** `true` when `Validate` returns no errors; otherwise `false`.
**Throws:**
- `ArgumentNullException` if `value` is `null`.

### EnsureValid
**Purpose:** Ensures a `DbContextMetadata` instance is valid, throwing if it is not.
**Signature:** `void EnsureValid(this DbContextMetadata value)`
**Throws:**
- `ArgumentNullException` if `value` is `null`.
- `ArgumentException` if the instance is invalid. The message contains the full list of validation errors, one per line.

## Validation Rules

`Validate` checks the following conditions and reports a distinct message for each violation. All checks run independently, so a single call can return multiple errors.

### Required string properties
Each of the following string properties must be non-null and non-whitespace. A violation produces a message of the form `"{Name} is required and cannot be null or whitespace."`:

- `Id`
- `ContextName`
- `AssemblyName`
- `Namespace`
- `DatabaseProvider`
- `ConnectionString`

### LastScannedAt
`LastScannedAt` must not be the default `DateTime` value (`DateTime.MinValue`, i.e. an uninitialized timestamp). A violation produces `"LastScannedAt must be set to a valid DateTime value."`

### Collection properties
Each of the following collection properties must be non-null. A violation produces a message of the form `"{Name} collection cannot be null."`:

- `MigrationIds` → `"MigrationIds collection cannot be null."`
- `EntityTypes` → `"EntityTypes collection cannot be null."`
- `Properties` → `"Properties dictionary cannot be null."`

Note that the collections are only checked for nullness, not for emptiness — an empty collection is valid.

## Usage

```csharp
using System;
using System.Linq;
using EfMigrationDiff.Models;

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

// Inspect problems without throwing.
var errors = metadata.Validate();
if (errors.Count > 0)
{
    Console.WriteLine(string.Join(Environment.NewLine, errors));
}

// Boolean check.
if (!metadata.IsValid())
{
    Console.WriteLine("Metadata is invalid.");
}

// Throw on invalid input at a boundary.
metadata.EnsureValid();
```

## Notes

- The validation rules here are stricter than the `DbContextMetadata.IsValid()` instance method, which only requires `ContextName`, `AssemblyName`, and `DatabaseProvider` to be non-whitespace. `DbContextMetadataValidation` additionally requires `Id`, `Namespace`, `ConnectionString`, a non-default `LastScannedAt`, and non-null collections.
- `Validate` is the single source of truth; `IsValid` and `EnsureValid` are thin wrappers over it, so the rules stay consistent across all three entry points.
- The class is static and stateless; it holds no configuration and performs no I/O.