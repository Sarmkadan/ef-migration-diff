# VisualDiffOutputTests

`VisualDiffOutputTests` contains unit‑test methods that verify the behavior of the visual diff utility used in the EF migration diff tool. Each test exercises a specific scenario—such as identical source/target models, source‑only changes, target‑only changes, destructive changes, empty inputs, source acceptance planning, and automatic conflict resolution—to ensure the diff algorithm produces the expected output collections and plans.

## API

### ComputeDiff_WithIdenticalChanges_ReturnsIdenticalResult
- **Purpose**: Verifies that when the source and target migration models contain identical changes, the diff result reports no differences and treats the outcome as identical.
- **Parameters**: None.
- **Return Value**: `void` (test method).
- **When it throws**: Throws an `Xunit.Sdk.AssertException` if the diff result does not indicate an identical outcome; otherwise completes silently.

### ComputeDiff_WithSourceOnlyChange_PopulatesSourceOnlyList
- **Purpose**: Confirms that a change present only in the source model is correctly placed in the source‑only list of the diff output, with the target‑only list remaining empty.
- **Parameters**: None.
- **Return Value**: `void`.
- **When it throws**: Throws an `Xunit.Sdk.AssertException` if the source‑only list does not contain exactly the expected change or if the target‑only list is non‑empty.

### ComputeDiff_WithTargetOnlyChange_PopulatesTargetOnlyList
- **Purpose**: Ensures that a change existing solely in the target model is recorded in the target‑only list, while the source‑only list stays empty.
- **Parameters**: None.
- **Return Value**: `void`.
- **When it throws**: Throws an `Xunit.Sdk.AssertException` when the target‑only list does not match the expected change or when the source‑only list is not empty.

### ComputeDiff_WithDestructiveChange_ReportsDestructive
- **Purpose**: Checks that a destructive change (e.g., column drop or type narrowing) is flagged as destructive in the diff result.
- **Parameters**: None.
- **Return Value**: `void`.
- **When it throws**: Throws an `Xunit.Sdk.AssertException` if the destructive flag is not set or if the change is mis‑categorized.

### ComputeDiff_WithEmptyInputs_ReturnsIdentical
- **Purpose**: Validates that supplying empty source and target models yields an identical diff result, indicating no changes detected.
- **Parameters**: None.
- **Return Value**: `void`.
- **When it throws**: Throws an `Xunit.Sdk.AssertException` when the result is not reported as identical.

### AcceptSource_BuildsPlanWithAllSourceResolutions
- **Purpose**: Asserts that when the acceptance strategy prefers the source side, the generated migration plan includes resolutions for all source‑side changes.
- **Parameters**: None.
- **Return Value**: `void`.
- **When it throws**: Throws an `Xunit.Sdk.AssertException` if any source change is missing from the plan or if unintended target changes appear.

### AutoMerge_WithTriviallyResolvableConflicts_ResolvesAll
- **Purpose**: Confirms that the auto‑merge logic resolves all conflicts that can be settled without user intervention (e.g., additive changes on both sides) and leaves no unresolved conflicts.
- **Parameters**: None.
- **Return Value**: `void`.
- **When it throws**: Throws an `Xunit.Sdk.AssertException` when any conflict remains unresolved after auto‑merge or when an unexpected change is introduced.

## Usage

```csharp
using Xunit;
using EfMigrationDiff.Tests; // namespace containing VisualDiffOutputTests

public class MyTestSuite
{
    [Fact]
    public void VerifyIdenticalChanges()
    {
        var test = new VisualDiffOutputTests();
        test.ComputeDiff_WithIdenticalChanges_ReturnsIdenticalResult();
        // No assertion needed; the method throws on failure.
    }

    [Fact]
    public void VerifySourceOnlyChangeHandling()
    {
        var test = new VisualDiffOutputTests();
        test.ComputeDiff_WithSourceOnlyChange_PopulatesSourceOnlyList();
    }
}
```

```csharp
using EfMigrationDiff.Tests;

public class AcceptanceTests
{
    [Fact]
    public void SourceAcceptancePlanIsComplete()
    {
        var test = new VisualDiffOutputTests();
        test.AcceptSource_BuildsPlanWithAllSourceResolutions();
    }

    [Fact]
    public void AutoMergeResolvesTrivialConflicts()
    {
        var test = new VisualDiffOutputTests();
        test.AutoMerge_WithTriviallyResolvableConflicts_ResolvesAll();
    }
}
```

## Notes

- The test class holds no mutable state; each method operates on locally created instances of the diff utility. Consequently, the methods are thread‑safe with respect to concurrent invocation, although they are intended to be executed by a test runner rather than manual multithreaded scenarios.
- Edge cases such as null inputs are not applicable because the methods encapsulate the test setup internally; any argument‑validation errors would surface as exceptions from the tested code, causing the test to fail with an `AssertionException`.
- The methods rely on the assertion framework (xUnit) to signal failures; they do not return values or expose additional data beyond the pass/fail outcome.
- When extending this test class, maintain the naming convention (`MethodName_Scenario_ExpectedOutcome`) to preserve readability and consistency with the existing suite.
