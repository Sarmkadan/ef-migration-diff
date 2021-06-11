# MigrationParserService

The `MigrationParserService` provides utilities to parse, validate, and compare Entity Framework Core migration files. It supports reading migration files from disk, extracting SQL operations, validating file structure, resolving dependencies between migrations, and comparing migration states to detect drift or differences.

## API

### `Migration? ParseMigrationFile(string filePath)`

Parses a single migration file at the specified file path and returns the corresponding `Migration` object if successful, or `null` if parsing fails.

- **Parameters**
  - `filePath` (string): The absolute or relative path to the migration file (e.g., `20240315120000_InitialCreate.cs`).
- **Return value**
  - `Migration?`: A populated `Migration` object on success; `null` if the file is malformed, missing, or not a valid migration.
- **Exceptions**
  - Throws `ArgumentNullException` if `filePath` is `null`.
  - Throws `FileNotFoundException` if the file does not exist.
  - Throws `UnauthorizedAccessException` if the caller lacks file read permissions.

---

### `List<Migration> ParseMigrationFiles(IEnumerable<string> filePaths)`

Parses multiple migration files in one operation and returns a list of successfully parsed `Migration` objects. Skips files that fail to parse.

- **Parameters**
  - `filePaths` (IEnumerable<string>): A collection of file paths to migration files.
- **Return value**
  - `List<Migration>`: A list of parsed `Migration` objects, in the same order as successful parses. Failed files are omitted.
- **Exceptions**
  - Throws `ArgumentNullException` if `filePaths` is `null`.
  - Throws `AggregateException` containing one or more file access exceptions if any file cannot be read.

---

### `async Task<List<Migration>> LoadMigrationsFromDirectoryAsync(string directoryPath, string searchPattern = "*.cs")`

Asynchronously loads and parses all migration files matching a pattern from a directory.

- **Parameters**
  - `directoryPath` (string): The root directory containing migration files.
  - `searchPattern` (string, optional): File search pattern (default: `"*.cs"`).
- **Return value**
  - `Task<List<Migration>>`: A task that resolves to a list of parsed `Migration` objects, ordered by filename (assumes lexicographic order reflects migration sequence).
- **Exceptions**
  - Throws `ArgumentNullException` if `directoryPath` is `null`.
  - Throws `DirectoryNotFoundException` if the directory does not exist.
  - Throws `UnauthorizedAccessException` if the caller lacks directory read permissions.

---

### `List<string> ValidateMigrationFile(string filePath)`

Validates the structure and content of a migration file without parsing it into a full `Migration` object.

- **Parameters**
  - `filePath` (string): The path to the migration file.
- **Return value**
  - `List<string>`: A list of validation error messages. Empty if the file is valid.
- **Exceptions**
  - Throws `ArgumentNullException` if `filePath` is `null`.
  - Throws `FileNotFoundException` if the file does not exist.

---

### `List<string> GetMigrationDependencies(string filePath)`

Determines the migration IDs that a given migration depends on, based on its `DependsOn` property.

- **Parameters**
  - `filePath` (string): The path to the migration file.
- **Return value**
  - `List<string>`: A list of migration IDs that this migration depends on. Returns an empty list if no dependencies are declared.
- **Exceptions**
  - Throws `ArgumentNullException` if `filePath` is `null`.
  - Throws `FileNotFoundException` if the file does not exist.
  - Throws `InvalidOperationException` if the file is not a valid migration.

---

### `Dictionary<string, object> CompareMigrations(Migration left, Migration right)`

Compares two `Migration` objects and returns a dictionary of differences.

- **Parameters**
  - `left` (Migration): The first migration to compare.
  - `right` (Migration): The second migration to compare.
- **Return value**
  - `Dictionary<string, object>`: A dictionary where keys are comparison categories (e.g., `"UpOperations"`, `"DownOperations"`, `"ModelSnapshot"`) and values are diff results (e.g., lists of added/removed operations or serialized snapshots).
- **Exceptions**
  - Throws `ArgumentNullException` if either `left` or `right` is `null`.

---
### `List<string> ExtractSqlOperations(Migration migration, bool up = true)`

Extracts raw SQL statements from the Up or Down operations of a migration.

- **Parameters**
  - `migration` (Migration): The migration to extract SQL from.
  - `up` (bool, optional): If `true`, extracts SQL from the Up method; if `false`, from the Down method. Default: `true`.
- **Return value**
  - `List<string>`: A list of SQL statements in the order they appear in the migration. Returns an empty list if no SQL operations are present.
- **Exceptions**
  - Throws `ArgumentNullException` if `migration` is `null`.

---
### `int GetMigrationSequence(Migration migration)`

Determines the sequence number of a migration based on its filename.

- **Parameters**
  - `migration` (Migration): The migration whose sequence is to be determined.
- **Return value**
  - `int`: The numeric sequence extracted from the filename (e.g., `20240315120000` → `20240315120000`). Returns `0` if the filename does not match the expected pattern.
- **Exceptions**
  - Throws `ArgumentNullException` if `migration` is `null`.

## Usage
