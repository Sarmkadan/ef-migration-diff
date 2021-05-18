# MigrationDiffServiceTests

## Overview
`MigrationDiffServiceTests` is a test class that verifies the behavior of the `MigrationDiffService.CompareBranches` method. Each test method exercises a specific scenario—such as handling null branches, identical migrations, source‑only or target‑only migrations, schema changes, and empty branches—to ensure the service correctly categorizes and reports differences between two migration branches.

## API
### CompareBranches_WithNullSourceBranch_ThrowsArgumentNullException
- **Purpose**: Confirms that supplying a `null` source branch to `CompareBranches` results in an `ArgumentNullException`.
- **Parameters**: None.
- **Return Value**: `void` (test method).
- **Throws**: The test expects an `ArgumentNullException`; if the exception is not thrown, the test fails.

### CompareBranches_WithNullTargetBranch_ThrowsArgumentNullException
- **Purpose**: Confirms that supplying a `null` target branch to `CompareBranches` results in an `ArgumentNullException`.
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: The test expects an `ArgumentNullException`; missing exception causes test failure.

### CompareBranches_WithIdenticalMigrations_CategorizesCorrectly
- **Purpose**: Verifies that when both branches contain the same set of migrations, the diff reports no additions, deletions, or modifications.
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: No exceptions are expected under normal execution; test failure indicates incorrect categorization.

### CompareBranches_WithSourceOnlyMigration_CategorizesCorrectly
- **Purpose**: Ensures that a migration present only in the source branch is categorized as a deletion (or removal) in the diff output.
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: No exceptions are expected; failure indicates mis‑categorization.

### CompareBranches_WithTargetOnlyMigration_CategorizesCorrectly
- **Purpose**: Ensures that a migration present only in the target branch is categorized as an addition in the diff output.
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: No exceptions are expected; failure indicates mis‑categorization.

### CompareBranches_DetectsSchemaChanges
- **Purpose**: Checks that changes to the underlying schema (e.g., altered column types) between branches are detected and reported as modifications.
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: No exceptions are expected; failure indicates schema changes were not detected.

### CompareBranches_WithEmptyBranches_ReturnsEmptyDiff
- **Purpose**: Validates that when both source and target branches contain no migrations, the diff result is empty (no additions, deletions, or modifications).
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: No exceptions are expected; failure indicates an incorrect empty‑branch handling.

## Usage
The following examples illustrate how to interact with `MigrationDiffService` in production code and how the corresponding unit tests validate its behavior.

```csharp
// Example 1: Normal comparison with identical migrations
var service = new MigrationDiffService();
var source = new MigrationBranch { Migrations = new[] { "20230101_Init", "20230102_AddUser" } };
var target = new MigrationBranch { Migrations = new[] { "20230101_Init", "20230102_AddUser" } };

var diff = service.CompareBranches(source, target);
// diff.Additions and diff.Removals should be empty; diff.Modifications should be empty.
```

```csharp
// Example 2: Handling a null source branch (expected to throw)
var service = new MigrationDiffService();
MigrationBranch source = null;
var target = new MigrationBranch { Migrations = Array.Empty<string>() };

try
{
    service.CompareBranches(source, target);
}
catch (ArgumentNullException ex)
{
    // ex.ParameterName == "source"
    // Handle or assert as appropriate in test code.
}
```

## Notes
- **Edge Cases**: The tests cover null branches, empty branches, identical migrations, source‑only/target‑only migrations, and schema modifications. Any other scenario (e.g., mixed additions and deletions) should be exercised by additional tests not covered by this class.
- **Thread‑Safety**: `MigrationDiffService` does not maintain mutable state; its `CompareBranches` method operates solely on its input parameters. Consequently, multiple threads can invoke the method concurrently without synchronization. The test class itself is intended for single‑threaded test execution and does not provide thread‑safety guarantees for test state.
