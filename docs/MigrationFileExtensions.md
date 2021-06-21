# MigrationFileExtensions

Provides static utility methods for inspecting and formatting EF Core migration source files. The type operates on `Type` objects representing compiled migration classes, extracting metadata such as the migration name, timestamp, and a human-readable display string without requiring instantiation or reflection into private members beyond what the public API already exposes.

## API

### `IsMigrationClass`

```csharp
public static bool IsMigrationClass(Type type)
```

Determines whether the supplied `Type` represents an EF Core migration class. The check verifies that the type is a non-abstract, non-generic class that inherits from `Migration` and is decorated with the `[Migration]` attribute.

**Parameters**
- `type` (`Type`): The candidate type to inspect. Must not be `null`.

**Returns**
- `true` if the type qualifies as a migration class; otherwise `false`.

**Exceptions**
- `ArgumentNullException`: Thrown when `type` is `null`.

---

### `GetMigrationName`

```csharp
public static string GetMigrationName(Type migrationType)
```

Extracts the logical migration name (e.g., `"InitialCreate"`) from a migration class. The value is read from the `[Migration]` attribute applied to the type.

**Parameters**
- `migrationType` (`Type`): A type that has already been confirmed as a migration class via `IsMigrationClass`. Must not be `null`.

**Returns**
- The migration name string as declared in the attribute.

**Exceptions**
- `ArgumentNullException`: Thrown when `migrationType` is `null`.
- `InvalidOperationException`: Thrown when the type does not carry the `[Migration]` attribute, which typically indicates the caller did not validate the type with `IsMigrationClass` first.

---

### `GetMigrationTimestamp`

```csharp
public static string GetMigrationTimestamp(Type migrationType)
```

Extracts the timestamp portion of the migration identifier (e.g., `"20250101120000"`) from the migration class. The value is read from the `[Migration]` attribute.

**Parameters**
- `migrationType` (`Type`): A type that has already been confirmed as a migration class. Must not be `null`.

**Returns**
- The timestamp string as declared in the attribute.

**Exceptions**
- `ArgumentNullException`: Thrown when `migrationType` is `null`.
- `InvalidOperationException`: Thrown when the type does not carry the `[Migration]` attribute.

---

### `GetFormattedDisplay`

```csharp
public static string GetFormattedDisplay(Type migrationType)
```

Returns a formatted string combining the migration timestamp and name, suitable for log output or user-facing lists. The format is `"{timestamp} - {name}"`.

**Parameters**
- `migrationType` (`Type`): A type that has already been confirmed as a migration class. Must not be `null`.

**Returns**
- A string in the form `"20250101120000 - InitialCreate"`.

**Exceptions**
- `ArgumentNullException`: Thrown when `migrationType` is `null`.
- `InvalidOperationException`: Thrown when the type does not carry the `[Migration]` attribute.

## Usage

**Example 1: Filtering and displaying all migrations in an assembly**

```csharp
using System;
using System.Linq;
using System.Reflection;

var assembly = typeof(MyDbContext).Assembly;

var migrations = assembly.GetTypes()
    .Where(MigrationFileExtensions.IsMigrationClass)
    .OrderBy(MigrationFileExtensions.GetMigrationTimestamp)
    .Select(MigrationFileExtensions.GetFormattedDisplay);

foreach (var display in migrations)
{
    Console.WriteLine(display);
}
// Output:
// 20250101120000 - InitialCreate
// 20250102183000 - AddCustomerTable
```

**Example 2: Comparing two migration files by metadata**

```csharp
Type left = typeof(InitialCreate);
Type right = typeof(AddCustomerTable);

if (!MigrationFileExtensions.IsMigrationClass(left) ||
    !MigrationFileExtensions.IsMigrationClass(right))
{
    throw new ArgumentException("Both types must be migration classes.");
}

string leftName = MigrationFileExtensions.GetMigrationName(left);
string rightName = MigrationFileExtensions.GetMigrationName(right);
string leftTimestamp = MigrationFileExtensions.GetMigrationTimestamp(left);
string rightTimestamp = MigrationFileExtensions.GetMigrationTimestamp(right);

bool sameName = string.Equals(leftName, rightName, StringComparison.Ordinal);
bool sameTimestamp = string.Equals(leftTimestamp, rightTimestamp, StringComparison.Ordinal);

Console.WriteLine($"Name match: {sameName}");
Console.WriteLine($"Timestamp match: {sameTimestamp}");
Console.WriteLine($"Left:  {MigrationFileExtensions.GetFormattedDisplay(left)}");
Console.WriteLine($"Right: {MigrationFileExtensions.GetFormattedDisplay(right)}");
```

## Notes

- All methods that accept a `Type` parameter throw `ArgumentNullException` when passed `null`. Callers should guard against this before invoking any member.
- `GetMigrationName`, `GetMigrationTimestamp`, and `GetFormattedDisplay` assume the caller has already validated the type with `IsMigrationClass`. Passing an arbitrary type that lacks the `[Migration]` attribute will result in an `InvalidOperationException`.
- The methods are stateless and do not cache results. Repeated calls with the same `Type` argument will re-read the attribute each time.
- The type is entirely static and contains no mutable shared state. All members are safe to call concurrently from multiple threads without synchronization.
- The `[Migration]` attribute is expected to follow the standard EF Core convention where the full migration identifier is composed of a timestamp followed by an underscore and the migration name. The methods rely on this convention when parsing the attribute value.
- `IsMigrationClass` returns `false` for abstract classes, generic type definitions, and types not directly inheriting from `Migration`, even if they carry the attribute. This prevents false positives on base classes or incorrectly decorated types.
