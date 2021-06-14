# SchemaDiffEngine

The `SchemaDiffEngine` is a utility class for analyzing and resolving differences between database schemas, particularly in scenarios involving migrations and schema merges. It provides capabilities to compute schema differences, evaluate three-way diffs, apply merge resolutions, and validate resolution plans.

## API

### `SchemaDiffEngine`
Initializes a new instance of the `SchemaDiffEngine` class.

### `SchemaDiffResult ComputeDiff(Schema source, Schema target)`
Computes the differences between a source schema and a target schema.

- **Parameters**
  - `source`: The source schema to compare against.
  - `target`: The target schema to compare.
- **Return Value**
  Returns a `SchemaDiffResult` containing the detected differences between the schemas.
- **Exceptions**
  Throws `ArgumentNullException` if either `source` or `target` is `null`.

### `ThreeWayDiffResult ComputeThreeWayDiff(Schema base, Schema source, Schema target)`
Computes a three-way diff between a base schema and two divergent schemas (source and target).

- **Parameters**
  - `base`: The common base schema.
  - `source`: The first divergent schema.
  - `target`: The second divergent schema.
- **Return Value**
  Returns a `ThreeWayDiffResult` containing the three-way differences.
- **Exceptions**
  Throws `ArgumentNullException` if any of `base`, `source`, or `target` is `null`.

### `SchemaMergeResult ApplyMergeResolution(MergeResolutionPlan plan)`
Applies a merge resolution plan to produce a merged schema.

- **Parameters**
  - `plan`: The merge resolution plan to apply.
- **Return Value**
  Returns a `SchemaMergeResult` containing the result of applying the resolution.
- **Exceptions**
  Throws `ArgumentNullException` if `plan` is `null`.
  Throws `InvalidOperationException` if the plan is invalid or cannot be applied.

### `MergeResolutionPlan AcceptSource(SchemaDiffResult diff)`
Generates a merge resolution plan that accepts all changes from the source schema.

- **Parameters**
  - `diff`: The schema diff result to resolve.
- **Return Value**
  Returns a `MergeResolutionPlan` representing the resolution that accepts all source changes.
- **Exceptions**
  Throws `ArgumentNullException` if `diff` is `null`.

### `MergeResolutionPlan AcceptTarget(SchemaDiffResult diff)`
Generates a merge resolution plan that accepts all changes from the target schema.

- **Parameters**
  - `diff`: The schema diff result to resolve.
- **Return Value**
  Returns a `MergeResolutionPlan` representing the resolution that accepts all target changes.
- **Exceptions**
  Throws `ArgumentNullException` if `diff` is `null`.

### `MergeResolutionPlan AutoMerge(SchemaDiffResult diff)`
Generates an automatic merge resolution plan based on heuristics or default rules.

- **Parameters**
  - `diff`: The schema diff result to resolve.
- **Return Value**
  Returns a `MergeResolutionPlan` representing the automatically resolved plan.
- **Exceptions**
  Throws `ArgumentNullException` if `diff` is `null`.

### `IReadOnlyList<string> ValidateResolution(MergeResolutionPlan plan)`
Validates a merge resolution plan to ensure it is applicable and conflict-free.

- **Parameters**
  - `plan`: The merge resolution plan to validate.
- **Return Value**
  Returns an `IReadOnlyList<string>` of validation messages. If the list is empty, the plan is valid.
- **Exceptions**
  Throws `ArgumentNullException` if `plan` is `null`.

## Usage

### Example 1: Basic Schema Comparison and Resolution
