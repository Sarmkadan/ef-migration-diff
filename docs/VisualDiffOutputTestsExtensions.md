# VisualDiffOutputTestsExtensions

A static class providing extension methods for asserting and constructing test data related to schema and migration differences in Entity Framework-based projects. Designed to support unit testing scenarios where visual diff outputs and merge conflict resolution strategies are validated.

## API

### CreateDiffResult
```csharp
public static SchemaDiffResult CreateDiffResult(...)
```
Creates a `SchemaDiffResult` instance for testing purposes. Parameters typically include source and target schema definitions, along with optional configuration for change detection rules. Returns a fully initialized `SchemaDiffResult` object. Throws `ArgumentNullException` if required schema parameters are null.

### CreateThreeWayDiff
```csharp
public static ThreeWayDiffResult CreateThreeWayDiff(...)
```
Constructs a `ThreeWayDiffResult` representing differences between a base schema, a source schema, and a target schema. Used to simulate three-way merge scenarios in migration testing. Parameters include the three schemas and optional conflict resolution settings. Throws `ArgumentException` if schemas are incompatible or missing.

### CreateConflictRegion
```csharp
public static MergeConflictRegion CreateConflictRegion(...)
```
Generates a `MergeConflictRegion` object for testing conflict resolution logic. Accepts parameters defining conflicting changes and their context. Returns a configured conflict region. Throws `InvalidOperationException` if conflict metadata is invalid.

### CreateResolutionPlan
```csharp
public static MergeResolutionPlan CreateResolutionPlan(...)
```
Builds a `MergeResolutionPlan` from a collection of conflict regions and resolution strategies. Used to test merge outcome validation. Parameters include conflict regions and resolution directives. Throws `ArgumentNullException` if required parameters are null.

### ShouldBeIdentical
```csharp
public static void ShouldBeIdentical(...)
```
Asserts that two schema or diff result instances are structurally identical. Parameters include the expected and actual objects. Throws `AssertionException` if differences are detected.

### ShouldHaveChanges
```csharp
public static void ShouldHaveChanges(...)
```
Verifies that a diff result contains at least one change. Accepts a diff result instance. Throws `AssertionException` if no changes are present.

### ShouldBeFullyResolved
```csharp
public static void ShouldBeFullyResolved(...)
```
Confirms that all conflicts in a resolution plan have been addressed. Parameters include the resolution plan. Throws `AssertionException` if unresolved conflicts exist.

### ShouldResolveWithStrategy
```csharp
public static void ShouldResolveWithStrategy(...)
```
Validates that a conflict region resolves correctly using a specified strategy. Parameters include the conflict region and expected resolution outcome. Throws `AssertionException` if the resolution does not match expectations.

### TotalChanges
```csharp
public static int TotalChanges(...)
```
Returns the total number of changes detected in a diff result. Parameters include the diff result. Returns an integer count. Throws `ArgumentNullException` if the diff result is null.

### HasChanges
```csharp
public static bool HasChanges(...)
```
Determines whether a diff result contains any changes. Parameters include the diff result. Returns `true` if changes exist, `false` otherwise. Throws `ArgumentNullException` if the diff result is null.

### CountResolvedWithStrategy
```csharp
public static int CountResolvedWithStrategy(...)
```
Counts the number of conflicts resolved using a specific strategy in a resolution plan. Parameters include the resolution plan and strategy type. Returns the count as an integer. Throws `ArgumentNullException` if the resolution plan is null.

## Usage

### Example 1: Validating Schema Differences
```csharp
[Test]
public void TestSchemaChangesDetection()
{
    var sourceSchema = new SchemaDefinition(); // Populate with test data
    var targetSchema = new SchemaDefinition(); // Populate with test data
    
    var diffResult = sourceSchema.CreateDiffResult(targetSchema);
    
    diffResult.ShouldHaveChanges();
    Assert.That(diffResult.TotalChanges, Is.EqualTo(3));
}
```

### Example 2: Testing Conflict Resolution
```csharp
[Test]
public void TestMergeConflictResolution()
{
    var baseSchema = new SchemaDefinition();
    var sourceSchema = new SchemaDefinition();
    var targetSchema = new SchemaDefinition();
    
    var threeWayDiff = baseSchema.CreateThreeWayDiff(sourceSchema, targetSchema);
    var conflictRegion = threeWayDiff.Conflicts.First().CreateConflictRegion();
    var resolutionPlan = conflictRegion.CreateResolutionPlan(ResolutionStrategy.AutoMerge);
    
    resolutionPlan.ShouldBeFullyResolved();
    Assert.That(resolutionPlan.CountResolvedWithStrategy(ResolutionStrategy.AutoMerge), Is.EqualTo(1));
}
```

## Notes

- All methods are thread-safe for read operations as they do not modify shared state.
- `CreateDiffResult` and related factory methods may throw exceptions if provided with null or invalid schema definitions.
- `ShouldBeIdentical` performs deep comparison and may have performance implications with large schema objects.
- `HasChanges` and `TotalChanges` are optimized for quick validation and should be preferred over manual enumeration in performance-sensitive tests.
- `ShouldResolveWithStrategy` requires exact strategy matching; partial or fuzzy strategy matches are not supported.
