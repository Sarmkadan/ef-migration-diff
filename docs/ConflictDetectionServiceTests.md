# ConflictDetectionServiceTests

Unit tests for the `ConflictDetectionService` class, which identifies and categorizes conflicts between migration operations in Entity Framework Core migrations. The tests verify detection of table, column, and index conflicts, as well as severity level reporting for different conflict types.

## API

### `public ConflictDetectionServiceTests`

The test class containing unit tests for conflict detection functionality. This class uses xUnit testing framework conventions and does not expose any public members beyond those required for test discovery.

### `public void DetectConflicts_WithNoChanges_ReturnsEmptyList()`

Verifies that when no migration operations are provided, the conflict detection service returns an empty list of conflicts. This test ensures the service handles the trivial case of no operations without errors.

- **Parameters**: None
- **Return value**: void (asserts on test conditions)
- **Throws**: No documented exceptions

### `public void DetectConflicts_WithConflictingTableOperations_ReturnsTableConflict()`

Validates that the service correctly identifies conflicts between table-level operations (e.g., create table vs. drop table) and returns a table conflict. The test asserts that the conflict type is properly categorized and the conflicting operations are included in the result.

- **Parameters**: None
- **Return value**: void (asserts on test conditions)
- **Throws**: No documented exceptions

### `public void DetectConflicts_WithConflictingColumnOperations_ReturnsColumnConflict()`

Ensures the service detects conflicts between column-level operations (e.g., add column vs. drop column) and returns a column conflict. The test verifies the conflict type, severity, and the specific operations involved in the conflict.

- **Parameters**: None
- **Return value**: void (asserts on test conditions)
- **Throws**: No documented exceptions

### `public void DetectConflicts_WithNonConflictingChanges_ReturnsEmptyList()`

Checks that non-conflicting migration operations (e.g., unrelated table creations) do not produce any conflicts. This test confirms the service only reports actual conflicts and ignores compatible operations.

- **Parameters**: None
- **Return value**: void (asserts on test conditions)
- **Throws**: No documented exceptions

### `public void DetectConflicts_WithConflictingTableOperations_ReturnsErrorSeverity()`

Tests that certain table conflicts (e.g., create vs. drop) are reported with error severity. The test validates both the presence of the conflict and its severity level.

- **Parameters**: None
- **Return value**: void (asserts on test conditions)
- **Throws**: No documented exceptions

### `public void DetectConflicts_WithConflictingIndexOperations_ReturnsWarningSeverity()`

Verifies that index-related conflicts (e.g., create index vs. drop index) are reported with warning severity rather than error. The test ensures proper severity categorization for less critical conflicts.

- **Parameters**: None
- **Return value**: void (asserts on test conditions)
- **Throws**: No documented exceptions

## Usage

```csharp
// Example 1: Testing conflict detection with no changes
[Fact]
public void TestNoChanges()
{
    var service = new ConflictDetectionService();
    var operations = Array.Empty<MigrationOperation>();
    var conflicts = service.DetectConflicts(operations);

    Assert.Empty(conflicts);
}

// Example 2: Testing conflict detection with conflicting table operations
[Fact]
public void TestConflictingTableOperations()
{
    var service = new ConflictDetectionService();
    var operations = new MigrationOperation[]
    {
        new CreateTableOperation { Name = "Users" },
        new DropTableOperation { Name = "Users" }
    };
    var conflicts = service.DetectConflicts(operations);

    Assert.Single(conflicts);
    Assert.Equal(ConflictType.Table, conflicts[0].Type);
    Assert.Equal(Severity.Error, conflicts[0].Severity);
}
```

## Notes

- The tests assume the `ConflictDetectionService` is deterministic and thread-safe for concurrent test execution, as no shared mutable state is modified during test execution.
- Edge cases such as null operation lists or empty operation arrays are implicitly covered by the `DetectConflicts_WithNoChanges_ReturnsEmptyList` test.
- The severity levels (Error vs. Warning) are validated only for the specific conflict types tested; other conflict types may have different severity mappings not covered by these tests.
- The tests do not verify performance characteristics or memory usage of the conflict detection algorithm.
