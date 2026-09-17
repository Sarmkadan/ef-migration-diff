# MigrationParserServiceValidation

`MigrationParserServiceValidation` is a static class of extension methods that provide validation helpers for [`MigrationParserService`](MigrationParserService.md). It centralizes the argument and structural checks that the parser relies on, returning human-readable problem lists instead of throwing on every invalid input.

The service itself is stateless, so validation of the instance is limited to a null-reference check; all real validation logic lives in the parameter-specific methods below.

## Validation model

Every validation method follows the same contract:

- Returns `IReadOnlyList<string>` of human-readable problems; an **empty list means valid**.
- Throws `ArgumentNullException` for null service/parameter references.
- Some methods throw `ArgumentException` for null-or-whitespace string parameters (via `ArgumentException.ThrowIfNullOrEmpty`).
- Validation is additive — a method collects *all* problems it can find rather than failing fast, so callers get a complete picture in one pass.

## API

### `IReadOnlyList<string> Validate(this MigrationParserService value)`

Validates the service instance itself.

- **Rules**
  - `value` must not be null.
- **Return value**
  - Always an empty list when `value` is non-null (the service is stateless).
- **Exceptions**
  - `ArgumentNullException` if `value` is null.

### `bool IsValid(this MigrationParserService value)`

Convenience wrapper returning `true` when `Validate(value)` produces no problems.

### `void EnsureValid(this MigrationParserService value)`

Throws if the service is invalid.

- **Exceptions**
  - `ArgumentNullException` if `value` is null.
  - `ArgumentException` if validation fails, with the joined problem list in the message.

### `IReadOnlyList<string> Validate(this MigrationParserService value, MigrationFile migrationFile)`

Validates a migration file before parsing.

- **Rules**
  - `FilePath` must not be null/whitespace; if set, the file must exist on disk.
  - `FileName` must not be null/whitespace; if set, it must end with `.cs` or `.Designer.cs`.
  - `DbContextName` must not be null/whitespace.
  - `FileSize` must not be negative.
  - `LastModified` must be set (not `default`).
  - `Content` must not be null/whitespace.
- **Exceptions**
  - `ArgumentNullException` if `value` or `migrationFile` is null.

### `IReadOnlyList<string> ValidateMigrationFile(this MigrationParserService value, MigrationFile migrationFile)`

Validates the *content* of a migration file. This is the structural/content check distinct from the property-level `Validate(MigrationFile)`.

- **Rules**
  - `Content` must not be empty (short-circuits, returning immediately if empty).
  - Content must contain a `public partial class` declaration.
  - Content must contain a `Down(` method.
  - Content must contain an `Up(` method.
  - Content must contain **exactly one** `public partial class` (matched via regex `public\s+partial\s+class\s+\w+\s*:`).
  - The migration ID extracted from the filename (`ExtractMigrationId()`) must be non-empty and a 14-digit timestamp in `YYYYMMDDHHmmss` format.
- **Exceptions**
  - `ArgumentNullException` if `value` or `migrationFile` is null.

### `IReadOnlyList<string> Validate(this MigrationParserService value, Migration migration)`

Validates a `Migration` object.

- **Rules**
  - `Id` must not be null/whitespace; if set, it must be a 14-digit timestamp in `YYYYMMDDHHmmss` format.
  - `Name` must not be null/whitespace.
  - `DbContextName` must not be null/whitespace.
  - `CreatedAt` must be set (not `default`).
  - `Content` must not be null/whitespace.
  - `Sequence` must not be negative.
- **Exceptions**
  - `ArgumentNullException` if `value` or `migration` is null.

### `IReadOnlyList<string> ValidateGetMigrationDependencies(this MigrationParserService value, Migration migration)`

Validates a migration before resolving its dependencies.

- **Rules**
  - All rules from `Validate(Migration)`.
  - `Content` must not be empty (dependencies cannot be extracted from empty content).
- **Exceptions**
  - `ArgumentNullException` if `value` or `migration` is null.

### `IReadOnlyList<string> ValidateCompareMigrations(this MigrationParserService value, Migration migration1, Migration migration2)`

Validates two migrations before comparing them.

- **Rules**
  - All rules from `Validate(Migration)` for both migrations.
  - `migration1.GetContentSize()` must be positive.
  - `migration2.GetContentSize()` must be positive.
- **Exceptions**
  - `ArgumentNullException` if `value`, `migration1`, or `migration2` is null.

### `IReadOnlyList<string> ValidateExtractSqlOperations(this MigrationParserService value, Migration migration)`

Validates a migration before extracting SQL operations.

- **Rules**
  - All rules from `Validate(Migration)`.
  - `Content` must not be empty (SQL cannot be extracted from empty content).
- **Exceptions**
  - `ArgumentNullException` if `value` or `migration` is null.

### `IReadOnlyList<string> ValidateGetMigrationSequence(this MigrationParserService value, string migrationId)`

Validates a migration ID before deriving its sequence number.

- **Rules**
  - `migrationId` must be at least 14 characters long.
  - The first 14 characters (the timestamp portion) must be numeric.
- **Exceptions**
  - `ArgumentNullException` if `value` is null.
  - `ArgumentException` if `migrationId` is null or empty.

### `IReadOnlyList<string> ValidateLoadMigrationsFromDirectoryAsync(this MigrationParserService value, string directoryPath, string dbContextName)`

Validates a directory before loading migrations from it.

- **Rules**
  - `directoryPath` must not be null/whitespace (throws `ArgumentException`).
  - `dbContextName` must not be null/whitespace (throws `ArgumentException`).
  - The directory must exist.
  - The directory must be accessible — a `*.cs` listing is attempted; `UnauthorizedAccessException` and `PathTooLongException` are caught and reported as problems.
  - `dbContextName` must not be null/whitespace (reported as a problem).
- **Exceptions**
  - `ArgumentNullException` if `value` is null.
  - `ArgumentException` if `directoryPath` or `dbContextName` is null/whitespace.

### `IReadOnlyList<string> ValidateParseMigrationFiles(this MigrationParserService value, List<MigrationFile> migrationFiles)`

Validates a batch of migration files before parsing.

- **Rules**
  - The list must not be empty.
  - Each file is validated via `Validate(MigrationFile)`; problems are prefixed with `File[i]:` to identify the offending index.
- **Exceptions**
  - `ArgumentNullException` if `value` or `migrationFiles` is null.

### `IReadOnlyList<string> ValidateParseMigrationFile(this MigrationParserService value, MigrationFile migrationFile)`

Validates a single migration file before parsing. Delegates directly to `Validate(MigrationFile)`.

- **Exceptions**
  - `ArgumentNullException` if `value` or `migrationFile` is null.

## Usage

```csharp
var service = new MigrationParserService();
var file = new MigrationFile("Migrations/20240315120000_InitialCreate.cs", "AppDbContext");

var problems = service.Validate(file);
if (problems.Count > 0)
{
    foreach (var problem in problems)
    {
        Console.WriteLine(problem);
    }
}
```