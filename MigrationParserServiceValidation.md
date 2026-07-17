# MigrationParserServiceValidation

`MigrationParserServiceValidation` provides a collection of extension methods that validate the public API of **MigrationParserService**.  
Each method inspects a specific operation (e.g., loading files, parsing migrations, extracting SQL) and returns a list of validation error messages.  
The helper methods `IsValid` and `EnsureValid` are convenience wrappers that interpret the result of `Validate` and either return a boolean or throw an exception when validation fails.

## API

| Member | Signature | Description |
|--------|-----------|-------------|
| **Validate** | `public static IReadOnlyList<string> Validate(this MigrationParserService value)` | Performs a comprehensive validation of the `MigrationParserService` instance, checking that required dependencies are set and that the service is in a usable state. Returns an empty list when the instance is valid. |
| **IsValid** | `public static bool IsValid(this MigrationParserService value)` | Returns `true` if `Validate` yields no errors; otherwise returns `false`. |
| **EnsureValid** | `public static void EnsureValid(this MigrationParserService value)` | Calls `Validate` and throws an `ValidationException` containing the first error message if any validation errors are present. |
| **ValidateMigrationFile** | `public static IReadOnlyList<string> ValidateMigrationFile(this MigrationParserService value, string filePath)` | Validates that the supplied migration file exists, is readable, and conforms to the expected naming convention. Returns a list of error messages describing any problems. |
| **ValidateGetMigrationDependencies** | `public static IReadOnlyList<string> ValidateGetMigrationDependencies(this MigrationParserService value, string migrationId)` | Checks that the `migrationId` is non‑null, non‑empty, and corresponds to a known migration in the repository. Returns validation errors if the ID cannot be resolved. |
| **ValidateCompareMigrations** | `public static IReadOnlyList<string> ValidateCompareMigrations(this MigrationParserService value, Migration left, Migration right)` | Ensures both `Migration` instances are non‑null and that they belong to the same `DbContext`. Returns errors when the inputs are invalid or when the comparison cannot be performed. |
| **ValidateExtractSqlOperations** | `public static IReadOnlyList<string> ValidateExtractSqlOperations(this MigrationParserService value, string sql)` | Validates that the supplied SQL string is not null or whitespace and that it can be parsed by the internal SQL parser. Returns a list of parsing‑related error messages. |
| **ValidateGetMigrationSequence** | `public static IReadOnlyList<string> ValidateGetMigrationSequence(this MigrationParserService value, IEnumerable<string> migrationIds)` | Checks that the collection of migration identifiers is non‑null, contains no null/empty entries, and that each identifier exists in the repository. Returns errors for missing or duplicate IDs. |
| **ValidateLoadMigrationsFromDirectoryAsync** | `public static IReadOnlyList<string> ValidateLoadMigrationsFromDirectoryAsync(this MigrationParserService value, string directoryPath)` | Validates that `directoryPath` points to an existing directory, that the directory is readable, and that it contains at least one file matching the migration file pattern. Returns validation errors for I/O or pattern mismatches. |
| **ValidateParseMigrationFiles** | `public static IReadOnlyList<string> ValidateParseMigrationFiles(this MigrationParserService value, IEnumerable<string> filePaths)` | Ensures the collection of file paths is non‑null, each path points to an existing file, and each file conforms to the migration file format. Returns a list of errors for any invalid entries. |
| **ValidateParseMigrationFile** | `public static IReadOnlyList<string> ValidateParseMigrationFile(this MigrationParserService value, string filePath)` | Performs the same checks as `ValidateParseMigrationFiles` but for a single file. Returns an empty list when the file is valid. |

**Exceptions** – All validation methods return error messages rather than throwing; however, `EnsureValid` throws a `ValidationException` when validation fails. Individual methods may propagate `ArgumentNullException` if required arguments are `null`, but this is considered part of the validation result rather than an unhandled exception.

## Usage

