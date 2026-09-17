# EfMigrationDiffExceptionExtensions

The `EfMigrationDiffExceptionExtensions` static class provides extension methods for `EfMigrationDiffException` to enhance error handling and reporting. These methods help format a full exception chain, extract the root cause, and detect migration conflicts without manually traversing the `InnerException` hierarchy.

## API

All methods are static and extend the `EfMigrationDiffException` type. They throw `ArgumentNullException` if the `exception` argument is `null`.

### `FormatDetailedMessage`

```csharp
public static string FormatDetailedMessage(this EfMigrationDiffException exception)
```

Formats the exception and all inner exceptions into a single, detailed message string. Each exception in the chain is rendered as a line in the form `[TypeName] Message`, and the lines are joined with `Environment.NewLine`.

- **Parameters**: `exception` – the `EfMigrationDiffException` instance.
- **Returns**: A formatted string containing the full exception chain.
- **Throws**: `ArgumentNullException` if `exception` is `null`.
- **Remarks**: The traversal follows `InnerException` only when it is an `EfMigrationDiffException`. Inner exceptions of other types terminate the chain and are not included.

### `GetRootCause`

```csharp
public static Exception GetRootCause(this EfMigrationDiffException exception)
```

Extracts the root cause exception from the chain of inner exceptions by following `InnerException` to the deepest level.

- **Parameters**: `exception` – the `EfMigrationDiffException` instance.
- **Returns**: The deepest inner exception, or the original exception if no inner exceptions exist.
- **Throws**: `ArgumentNullException` if `exception` is `null`.
- **Remarks**: Unlike `FormatDetailedMessage` and `HasMigrationConflict`, this method follows `InnerException` regardless of its type, so it can reach the true root cause even when it is not an `EfMigrationDiffException`.

### `HasMigrationConflict`

```csharp
public static bool HasMigrationConflict(this EfMigrationDiffException exception)
```

Determines if the exception chain contains any `MigrationConflictException` instances.

- **Parameters**: `exception` – the `EfMigrationDiffException` instance.
- **Returns**: `true` if the exception or any inner exception is a `MigrationConflictException`; otherwise `false`.
- **Throws**: `ArgumentNullException` if `exception` is `null`.
- **Remarks**: The traversal follows `InnerException` only when it is an `EfMigrationDiffException`. Inner exceptions of other types terminate the chain and are not inspected.

## Usage

The following examples demonstrate typical usage of `EfMigrationDiffExceptionExtensions` methods.

### Example 1: Logging a detailed exception chain

```csharp
using EfMigrationDiff.Exceptions;

try
{
    RunMigrationDiff();
}
catch (EfMigrationDiffException ex)
{
    Console.Error.WriteLine(ex.FormatDetailedMessage());
}
```

### Example 2: Extracting and reporting the root cause

```csharp
using EfMigrationDiff.Exceptions;

try
{
    RunMigrationDiff();
}
catch (EfMigrationDiffException ex)
{
    Exception root = ex.GetRootCause();
    Console.WriteLine($"Root cause: {root.GetType().Name}: {root.Message}");
}
```

### Example 3: Branching on migration conflicts

```csharp
using EfMigrationDiff.Exceptions;

try
{
    RunMigrationDiff();
}
catch (EfMigrationDiffException ex)
{
    if (ex.HasMigrationConflict())
    {
        Console.WriteLine("Migration conflict detected; manual resolution required.");
    }
    else
    {
        Console.WriteLine(ex.FormatDetailedMessage());
    }
}
```

## Notes

- All methods are static and operate on an `EfMigrationDiffException` instance. They do not modify the exception.
- If the `exception` argument is `null`, every method throws `ArgumentNullException`.
- `FormatDetailedMessage` and `HasMigrationConflict` only traverse inner exceptions that are themselves `EfMigrationDiffException` instances. If a non-`EfMigrationDiffException` inner exception is encountered, traversal stops. Use `GetRootCause` when the root cause may be an arbitrary `Exception` type.
- `GetRootCause` returns the original exception when the chain has no inner exceptions, so the return value is never `null` for a non-null input.
- `FormatDetailedMessage` returns an empty string when the exception has no message; the `[TypeName]` prefix is always present for each traversed exception.