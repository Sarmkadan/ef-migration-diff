# ValidationHelper

Provides a centralized set of static validation and sanitization utilities for EF Core migration metadata, database object identifiers, and related input strings. All methods operate on string inputs and return boolean results or a sanitized string, with no side effects or state modifications.

## API

### `IsValidMigrationTimestamp(string timestamp)`
Validates whether a string conforms to the expected EF Core migration timestamp format (typically a 14-digit numeric string representing `yyyyMMddHHmmss`).
- **Parameters:** `timestamp` — the string to validate.
- **Returns:** `true` if the string is exactly 14 numeric digits; otherwise `false`.
- **Throws:** `ArgumentNullException` if `timestamp` is `null`.

### `IsValidMigrationId(string migrationId)`
Checks whether a full migration ID (timestamp + underscore + migration name) is well-formed.
- **Parameters:** `migrationId` — the full migration identifier string.
- **Returns:** `true` if the string matches the expected pattern of a valid timestamp followed by an underscore and a valid migration name; otherwise `false`.
- **Throws:** `ArgumentNullException` if `migrationId` is `null`.

### `IsValidTableName(string tableName)`
Determines whether a string is a valid SQL table name according to project conventions.
- **Parameters:** `tableName` — the proposed table name.
- **Returns:** `true` if the name is non-empty, within length limits, alphanumeric (possibly with underscores), and not a reserved keyword; otherwise `false`.
- **Throws:** `ArgumentNullException` if `tableName` is `null`.

### `IsValidColumnName(string columnName)`
Determines whether a string is a valid SQL column name according to project conventions.
- **Parameters:** `columnName` — the proposed column name.
- **Returns:** `true` if the name is non-empty, within length limits, alphanumeric (possibly with underscores), and not a reserved keyword; otherwise `false`.
- **Throws:** `ArgumentNullException` if `columnName` is `null`.

### `IsValidIndexName(string indexName)`
Determines whether a string is a valid SQL index name according to project conventions.
- **Parameters:** `indexName` — the proposed index name.
- **Returns:** `true` if the name is non-empty, within length limits, alphanumeric (possibly with underscores), and not a reserved keyword; otherwise `false`.
- **Throws:** `ArgumentNullException` if `indexName` is `null`.

### `IsValidMigrationName(string migrationName)`
Validates the descriptive name portion of a migration (the part after the timestamp and underscore).
- **Parameters:** `migrationName` — the migration name string.
- **Returns:** `true` if the name is non-empty, starts with a letter, contains only alphanumeric characters or underscores, and falls within allowed length bounds; otherwise `false`.
- **Throws:** `ArgumentNullException` if `migrationName` is `null`.

### `IsValidBranchName(string branchName)`
Validates a Git branch name against typical branch naming rules.
- **Parameters:** `branchName` — the branch name string.
- **Returns:** `true` if the name is non-empty, does not contain prohibited characters (e.g., spaces, tilde, caret, colon, backslash), and does not end with a dot or `.lock`; otherwise `false`.
- **Throws:** `ArgumentNullException` if `branchName` is `null`.

### `IsValidFilePath(string filePath)`
Checks whether a string represents a syntactically valid file path.
- **Parameters:** `filePath` — the path string.
- **Returns:** `true` if the path contains no invalid characters for the current OS and the file name portion (if present) is not reserved; otherwise `false`.
- **Throws:** `ArgumentNullException` if `filePath` is `null`.

### `IsValidDirectoryPath(string directoryPath)`
Checks whether a string represents a syntactically valid directory path.
- **Parameters:** `directoryPath` — the path string.
- **Returns:** `true` if the path contains no invalid characters for the current OS; otherwise `false`.
- **Throws:** `ArgumentNullException` if `directoryPath` is `null`.

### `IsValidCommitSha(string commitSha)`
Validates whether a string is a well-formed Git commit SHA (full 40-character hex or abbreviated 7+ character hex).
- **Parameters:** `commitSha` — the SHA string.
- **Returns:** `true` if the string consists entirely of hexadecimal characters and is either 40 characters long or between 7 and 39 characters inclusive; otherwise `false`.
- **Throws:** `ArgumentNullException` if `commitSha` is `null`.

### `SanitizeInput(string input)`
Removes or replaces potentially dangerous characters from an arbitrary input string to produce a safe, normalized form suitable for use in identifiers or display.
- **Parameters:** `input` — the raw input string.
- **Returns:** A sanitized string with control characters removed, leading/trailing whitespace trimmed, and certain special characters replaced or stripped. Returns `string.Empty` if `input` is `null` or whitespace-only.
- **Throws:** Does not throw.

### `IsValidDateFormat(string dateString)`
Validates whether a string conforms to the expected date format used within the project (e.g., `yyyy-MM-dd`).
- **Parameters:** `dateString` — the date string to validate.
- **Returns:** `true` if the string matches the expected format and represents a valid calendar date; otherwise `false`.
- **Throws:** `ArgumentNullException` if `dateString` is `null`.

### `IsValidDbContextName(string dbContextName)`
Checks whether a string is a valid `DbContext` class name according to project naming conventions.
- **Parameters:** `dbContextName` — the proposed class name.
- **Returns:** `true` if the name is non-empty, starts with a letter, contains only alphanumeric characters, and ends with `"Context"`; otherwise `false`.
- **Throws:** `ArgumentNullException` if `dbContextName` is `null`.

### `IsValidEmail(string email)`
Performs a basic structural validation of an email address.
- **Parameters:** `email` — the email address string.
- **Returns:** `true` if the string contains exactly one `@` symbol, has a non-empty local part and domain part, and the domain contains at least one dot; otherwise `false`.
- **Throws:** `ArgumentNullException` if `email` is `null`.

### `IsReservedKeyword(string word)`
Checks whether a given string is a reserved keyword in the target database system (e.g., SQL Server, PostgreSQL) or in C#.
- **Parameters:** `word` — the string to check.
- **Returns:** `true` if the string matches a known reserved keyword (case-insensitive); otherwise `false`.
- **Throws:** `ArgumentNullException` if `word` is `null`.

### `IsValidLength(string input, int minLength, int maxLength)`
Checks whether a string's length falls within an inclusive range.
- **Parameters:** `input` — the string to measure; `minLength` — minimum allowed length; `maxLength` — maximum allowed length.
- **Returns:** `true` if `input.Length` is between `minLength` and `maxLength` inclusive; otherwise `false`.
- **Throws:** `ArgumentNullException` if `input` is `null`. `ArgumentOutOfRangeException` if `minLength` is negative or `maxLength` is less than `minLength`.

### `IsAlphanumeric(string input)`
Determines whether a string consists exclusively of ASCII letters and digits.
- **Parameters:** `input` — the string to test.
- **Returns:** `true` if every character is an ASCII letter (A-Z, a-z) or digit (0-9); otherwise `false`. Returns `false` for empty strings.
- **Throws:** `ArgumentNullException` if `input` is `null`.

### `IsNumeric(string input)`
Determines whether a string consists exclusively of ASCII digit characters.
- **Parameters:** `input` — the string to test.
- **Returns:** `true` if every character is an ASCII digit (0-9); otherwise `false`. Returns `false` for empty strings.
- **Throws:** `ArgumentNullException` if `input` is `null`.

## Usage

### Example 1: Validating a migration ID before processing

```csharp
string candidateMigrationId = "20250115123045_AddCustomerTable";

if (ValidationHelper.IsValidMigrationId(candidateMigrationId))
{
    Console.WriteLine($"Processing migration: {candidateMigrationId}");
    // Proceed with diff logic
}
else
{
    Console.WriteLine($"Invalid migration ID format: {candidateMigrationId}");
}
```

### Example 2: Sanitizing user input for a new migration name

```csharp
string rawName = "  Fix_Order_Total$$  ";
string sanitized = ValidationHelper.SanitizeInput(rawName);

if (ValidationHelper.IsValidMigrationName(sanitized))
{
    string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
    string fullId = $"{timestamp}_{sanitized}";
    Console.WriteLine($"Generated migration ID: {fullId}");
}
else
{
    Console.WriteLine($"Sanitized name '{sanitized}' is not a valid migration name.");
}
```

## Notes

- All methods that accept a `string` parameter throw `ArgumentNullException` when passed `null`, except `SanitizeInput`, which gracefully returns `string.Empty`.
- `IsValidLength` additionally throws `ArgumentOutOfRangeException` if the length boundaries are invalid, regardless of the input string's value.
- Validation methods are stateless and thread-safe; they can be called concurrently from multiple threads without any synchronization.
- `IsValidFilePath` and `IsValidDirectoryPath` perform syntactic checks only; they do not verify whether the path actually exists on disk.
- `IsReservedKeyword` uses a fixed, case-insensitive keyword list appropriate for the project's target database. It does not dynamically query the database server.
- `SanitizeInput` is intentionally conservative and may produce empty results for inputs consisting entirely of stripped characters. Callers should check the result before using it in identifier generation.
- `IsValidCommitSha` accepts both full 40-character SHAs and abbreviated SHAs of at least 7 characters, reflecting common Git usage.
