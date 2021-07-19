// ... existing content ...

## VisualDiffOutputTestsExtensions

The `VisualDiffOutputTestsExtensions` class provides a set of extension methods for testing and asserting the output of visual diff operations. These methods allow you to create test data for schema diff results, three-way diff results, merge conflict regions, and merge resolution plans, as well as assert the expected behavior of these data structures.

Here's an example of how to use these extension methods:

```csharp
var baseline = SchemaDiffResult.CreateDiffResult(
    new[] { "Table1", "Table2" },
    new[] { "AddedColumn", "RemovedColumn" }
);

var source = ThreeWayDiffResult.CreateThreeWayDiff(
    baseline,
    new[] { "AddedColumn2" },
    new[] { "RemovedColumn2" }
);

var conflictRegion = MergeConflictRegion.CreateConflictRegion(
    "Table1",
    new[] { "Column1", "Column2" }
);

var resolutionPlan = MergeResolutionPlan.CreateResolutionPlan(
    conflictRegion,
    MergeResolutionStrategy.AcceptSource
);

baseline.ShouldHaveChanges();
source.ShouldHaveChanges();
conflictRegion.ShouldNotBeNull();
resolutionPlan.ShouldNotBeNull();

var totalChanges = baseline.TotalChanges();
var hasChanges = baseline.HasChanges();
var resolvedCount = baseline.CountResolvedWithStrategy(MergeResolutionStrategy.AcceptSource);

Assert.True(totalChanges > 0);
Assert.True(hasChanges);
Assert.True(resolvedCount == 0);
```