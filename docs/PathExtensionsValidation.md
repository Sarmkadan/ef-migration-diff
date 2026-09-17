# PathExtensionsValidation

Provides validation helpers for path operations from `PathExtensions`. It is a static class that validates path strings for correctness, safety, and common issues, exposing three entry points: `Validate`, `IsValid`, and `EnsureValid`.

## API

### Validate
**Purpose:** Validates a path string and returns a read-only list of human-readable validation problems. Returns an empty list when the path is valid.
**Signature:** `IReadOnlyList<string> Validate(this string? path)`
**Throws:**
- `ArgumentNullException` if `path` is `null`.

The method never throws for invalid data; it accumulates every problem it finds and returns them all in a single list.

### IsValid
**Purpose:** Determines whether a path string is valid.
**Signature:** `bool IsValid(this string? path)`
**Return:** `true` when `Validate` returns no errors; otherwise `false`.
**Throws:**
- `ArgumentNullException` if `path` is `null`.

### EnsureValid
**Purpose:** Ensures a path string is valid, throwing if it is not.
**Signature:** `void EnsureValid(this string? path)`
**Throws:**
- `ArgumentNullException` if `path` is `null`.
- `ArgumentException` if the path is invalid. The message contains the full list of validation problems joined by spaces.

## Validation Rules

`Validate` checks the following conditions and reports a distinct message for each violation. All checks run independently, so a single call can return multiple errors.

### Null, empty, or whitespace
A `null`, empty, or whitespace-only path produces `"Path is null, empty, or whitespace."`. This is the only check that short-circuits the others — when the path is null/empty/whitespace, no further checks run.

### Invalid characters
The path is checked against the union of `Path.GetInvalidPathChars()` and `Path.GetInvalidFileNameChars()`. If any character is present, the path produces `"Path contains invalid characters."`.

### Relative path starting with `.`
A path that starts with `.` but not with `./` or `../` produces `"Path appears to be a relative path starting with '.'. Consider using './' prefix."`. This flags ambiguous forms such as `.foo` or `..bar` while allowing the conventional `./` and `../` prefixes.

### Path length
A path longer than 260 characters produces `"Path is longer than 260 characters, which may cause issues on some systems."`. The 260-character threshold reflects the Windows `MAX_PATH` limit, though longer paths are tolerated on modern systems.

### Consecutive slashes
A path containing `//` or `\\` produces `"Path contains consecutive slashes."`.

### Trailing whitespace
A path ending in a space or in `. ` produces `"Path ends with whitespace, which may cause issues."`. The `. ` case catches a trailing space that follows a final dot.

## Usage

```csharp
using System;
using System.Linq;
using EfMigrationDiff.Extensions;

var path = "/repo/src/App/Program.cs";

// Inspect problems without throwing.
var errors = path.Validate();
if (errors.Count > 0)
{
    Console.WriteLine(string.Join(Environment.NewLine, errors));
}

// Boolean check.
if (!path.IsValid())
{
    Console.WriteLine("Path is invalid.");
}

// Throw on invalid input at a boundary.
path.EnsureValid();
```

## Notes

- `Validate` is the single source of truth; `IsValid` and `EnsureValid` are thin wrappers over it, so the rules stay consistent across all three entry points.
- Validation is purely syntactic and cross-platform — it inspects the string itself and does not touch the filesystem, so it never checks whether a path exists or is accessible.
- The class is static and stateless; it holds no configuration and performs no I/O.