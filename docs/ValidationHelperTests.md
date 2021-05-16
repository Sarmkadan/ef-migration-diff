# ValidationHelperTests

Unit tests for the `ValidationHelper` class, verifying behavior of migration timestamp validation, SQL identifier validation, and input sanitization logic.

## API

### `IsValidMigrationTimestamp_WithVariousInputs_ReturnsExpectedResult`

Validates that `IsValidMigrationTimestamp` returns correct boolean results for a variety of input strings. This test covers valid and invalid migration timestamp formats, including edge cases such as empty strings, malformed dates, and correctly formatted ISO timestamps. No exceptions are expected under normal test conditions.

### `IsValidTableName_WithBracketedSchemaAndTableName_ReturnsTrue`

Ensures that table names enclosed in square brackets (e.g., `[dbo].[Users]`) are accepted as valid. This test confirms that the validation logic correctly handles fully qualified table names with bracketed schema and table components.

### `IsValidColumnName_WithDigitAsFirstCharacter_ReturnsFalse`

Confirms that column names beginning with a digit are rejected as invalid. This test validates the rule that SQL identifiers must not start with a numeric character, ensuring proper enforcement of naming conventions.

### `SanitizeInput_WithSqlInjectionCharacters_RemovesSemicolonAndSingleQuote`

Verifies that input strings containing SQL injection characters (specifically semicolons and single quotes) are sanitized by removing those characters. This test ensures that potentially dangerous input is neutralized before further processing.

## Usage
