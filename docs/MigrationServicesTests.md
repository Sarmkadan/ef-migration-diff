# MigrationServicesTests

The `MigrationServicesTests` class contains unit tests for validating the behavior of migration detection and conflict resolution services in the `ef-migration-diff` project. These tests verify the correctness of change detection, safety checks, and conflict identification when comparing database schema migrations, ensuring that migrations are applied accurately and conflicts are properly reported.

## API

### `DetectChanges_WithCreateTableContent_DetectsOneCreateTableChange`
Verifies that the migration service correctly identifies a single `CREATE TABLE` operation when comparing schema states.
- **Purpose**: Ensures that schema changes involving table creation are detected.
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: None.

### `IsMigrationSafe_WithDropTableContent_ReturnsFalse`
Tests whether the migration service correctly identifies a `DROP TABLE` operation as unsafe.
- **Purpose**: Validates that destructive operations (e.g., dropping tables) are flagged as unsafe.
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: None.

### `DetectConflicts_WhenSameTableCreatedWithDifferentSchema_ReturnsNamingConflict`
Checks if the service detects a naming conflict when the same table is created with differing schema definitions.
- **Purpose**: Ensures conflicts arising from schema mismatches are reported.
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: None.

### `DetectConflicts_WhenSameColumnModifiedWithDifferentDefaultValue_ReturnsColumnConflict`
Validates that modifying the same column with different default values triggers a conflict.
- **Purpose**: Confirms that column-level conflicts (e.g., default value mismatches) are detected.
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: None.

### `DetectConflicts_WhenSameColumnModifiedWithSameDefaultValue_ReturnsNoConflicts`
Ensures no conflict is reported when the same column is modified with identical default values.
- **Purpose**: Verifies that non-conflicting changes do not trigger false positives.
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: None.

### `ExecuteAsync_WithRegisteredMockedCommand_InvokesCommandExactlyOnce`
Tests that a registered command is invoked exactly once during asynchronous execution.
- **Purpose**: Validates the correctness of command invocation in async workflows.
- **Parameters**: None.
- **Return Value**: `Task`.
- **Throws**: None.

## Usage

### Example 1: Detecting Schema Changes
