# SchemaDiffPipelineService
The `SchemaDiffPipelineService` class is designed to facilitate the comparison and merging of database schema changes between different branches. It provides methods for performing two-way and three-way diffs, as well as attempting to auto-merge changes. The service also exposes various properties for accessing the results of these operations in different formats.

## API
* `public SchemaDiffPipelineService`: The constructor for the `SchemaDiffPipelineService` class.
* `public SchemaDiffPipelineResult RunTwoWayDiff`: Performs a two-way diff between the source and target branches. Returns a `SchemaDiffPipelineResult` object containing the results of the diff. Throws if the diff operation fails.
* `public SchemaDiffPipelineResult RunThreeWayDiff`: Performs a three-way diff between the source, target, and base branches. Returns a `SchemaDiffPipelineResult` object containing the results of the diff. Throws if the diff operation fails.
* `public SchemaMergeResult TryAutoMerge`: Attempts to auto-merge the changes between the source and target branches. Returns a `SchemaMergeResult` object containing the results of the merge attempt. Throws if the merge operation fails.
* `public SchemaDiffResult? Diff`: Gets the result of the last diff operation performed by the service.
* `public ThreeWayDiffResult? ThreeWayDiff`: Gets the result of the last three-way diff operation performed by the service.
* `public MigrationDiff? MigrationDiff`: Gets the migration diff result.
* `public string SideBySideHtml`: Gets the side-by-side HTML representation of the diff results.
* `public string UnifiedHtml`: Gets the unified HTML representation of the diff results.
* `public string MergeEditorHtml`: Gets the merge editor HTML representation of the diff results.
* `public string? BaseBranch`: Gets or sets the base branch for three-way diff operations.
* `public required string SourceBranch`: Gets or sets the source branch for diff operations.
* `public required string TargetBranch`: Gets or sets the target branch for diff operations.

## Usage
```csharp
// Example 1: Performing a two-way diff
var service = new SchemaDiffPipelineService
{
    SourceBranch = "feature/new-table",
    TargetBranch = "main"
};
var result = service.RunTwoWayDiff();
Console.WriteLine(service.SideBySideHtml);

// Example 2: Performing a three-way diff and attempting to auto-merge
var service2 = new SchemaDiffPipelineService
{
    BaseBranch = "release/1.0",
    SourceBranch = "feature/new-column",
    TargetBranch = "main"
};
var result2 = service2.RunThreeWayDiff();
var mergeResult = service2.TryAutoMerge();
Console.WriteLine(service2.MergeEditorHtml);
```

## Notes
The `SchemaDiffPipelineService` class is not thread-safe, and its methods should not be called concurrently from multiple threads. The `BaseBranch`, `SourceBranch`, and `TargetBranch` properties must be set before calling the `RunTwoWayDiff` or `RunThreeWayDiff` methods. If the `BaseBranch` property is not set, the `RunThreeWayDiff` method will throw. The `TryAutoMerge` method may throw if the auto-merge operation fails. The `Diff`, `ThreeWayDiff`, and `MigrationDiff` properties will be null if the corresponding diff operation has not been performed. The `SideBySideHtml`, `UnifiedHtml`, and `MergeEditorHtml` properties will be empty strings if the corresponding diff operation has not been performed.
