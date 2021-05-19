# AutoMergeSuggestionsTests

Test suite for validating the behavior of the auto-merge suggestion resolution logic in the EF migration diff tool. This class exercises the conflict detection and automatic resolution strategies applied when merging divergent migration models, ensuring that index, constraint, column, and table-level conflicts are handled according to the configured strategy, including partial resolution, cancellation support, and strategy registration lookups.

## API

### `public async Task ResolveAsync_WithNoConflicts_ReturnsEmptyResult`

Verifies that when two migration models have no conflicting elements, the resolution process returns an empty result set without errors. No parameters; returns a completed task. Does not throw.

### `public async Task ResolveAsync_WithIndexConflict_AutoResolvesViaSkip`

Ensures that an index conflict between source and target models is automatically resolved by applying the skip strategy, omitting the conflicting index from the merged output. No parameters; returns a completed task. Does not throw.

### `public async Task ResolveAsync_WithConstraintConflict_AutoResolvesViaCombine`

Validates that a constraint conflict triggers the combine strategy, merging both constraint definitions into a single resolved representation. No parameters; returns a completed task. Does not throw.

### `public async Task ResolveAsync_WithColumnConflict_LeavesUnresolved`

Confirms that column-level conflicts are intentionally left unresolved by the auto-merge logic, requiring manual intervention. The result set retains the unresolved conflict entry. No parameters; returns a completed task. Does not throw.

### `public async Task ResolveAsync_WithTableConflict_LeavesUnresolved`

Confirms that table-level conflicts are intentionally left unresolved, mirroring the column-conflict behavior. The result set retains the unresolved conflict entry. No parameters; returns a completed task. Does not throw.

### `public async Task ResolveAsync_WithMixedConflicts_PartiallyResolves`

Tests a scenario containing a mix of resolvable and non-resolvable conflicts. The method asserts that auto-resolvable conflicts (e.g., indexes, constraints) are resolved while column and table conflicts remain unresolved, producing a partially resolved result set. No parameters; returns a completed task. Does not throw.

### `public async Task ConfigureStrategy_OverridesDefaultBehavior`

Demonstrates that explicitly configuring a custom resolution strategy for a given conflict type overrides the default behavior, and that subsequent resolutions honor the overridden strategy. No parameters; returns a completed task. Does not throw.

### `public void GetStrategy_ForRegisteredType_ReturnsExpectedStrategy`

Queries the strategy registry for a conflict type that has been explicitly registered and asserts that the correct strategy instance is returned. No parameters; returns void. Does not throw.

### `public void GetStrategy_ForUnregisteredType_ReturnsNull`

Queries the strategy registry for a conflict type that has never been registered and asserts that the lookup returns null. No parameters; returns void. Does not throw.

### `public async Task ResolveAsync_WithCancelledToken_ThrowsOperationCancelledException`

Passes a pre-cancelled cancellation token to the resolution method and asserts that an `OperationCancelledException` is thrown, verifying cooperative cancellation support. No parameters; returns a completed task. Throws `OperationCancelledException` (expected).

## Usage

```csharp
// Example 1: Running the full test suite with a standard test runner
[TestFixture]
public class MigrationMergeTests
{
    private AutoMergeSuggestionsTests _sut;

    [SetUp]
    public void Setup()
    {
        _sut = new AutoMergeSuggestionsTests();
    }

    [Test]
    public async Task VerifyAllConflictScenarios()
    {
        await _sut.ResolveAsync_WithNoConflicts_ReturnsEmptyResult();
        await _sut.ResolveAsync_WithIndexConflict_AutoResolvesViaSkip();
        await _sut.ResolveAsync_WithConstraintConflict_AutoResolvesViaCombine();
        await _sut.ResolveAsync_WithColumnConflict_LeavesUnresolved();
        await _sut.ResolveAsync_WithTableConflict_LeavesUnresolved();
        await _sut.ResolveAsync_WithMixedConflicts_PartiallyResolves();
    }

    [Test]
    public void VerifyStrategyRegistration()
    {
        _sut.GetStrategy_ForRegisteredType_ReturnsExpectedStrategy();
        _sut.GetStrategy_ForUnregisteredType_ReturnsNull();
    }
}
```

```csharp
// Example 2: Testing cancellation and configuration overrides in isolation
[TestFixture]
public class StrategyConfigurationTests
{
    [Test]
    public async Task CancellationAndOverrideWorkflow()
    {
        var tests = new AutoMergeSuggestionsTests();

        // Verify that cancellation is respected
        await tests.ResolveAsync_WithCancelledToken_ThrowsOperationCancelledException();

        // Verify that custom strategy configuration takes effect
        await tests.ConfigureStrategy_OverridesDefaultBehavior();
    }
}
```

## Notes

- Tests that assert unresolved results for column and table conflicts (`LeavesUnresolved`) imply that the auto-merge engine deliberately excludes these conflict categories from automatic resolution; downstream consumers must handle them manually.
- The `GetStrategy_ForUnregisteredType_ReturnsNull` test indicates that the strategy registry is not fallback-tolerant—callers must null-check before invoking any strategy.
- Cancellation is cooperative; the `ResolveAsync_WithCancelledToken_ThrowsOperationCancelledException` test confirms that the token is observed and does not leave partial state behind, but individual implementations must ensure cleanup on cancellation.
- These tests are designed for sequential execution within a single test runner context; no thread-safety guarantees are implied for concurrent invocation across multiple threads, as the underlying resolution state may not be isolated.
