# ISchemaDiffEngine

`ISchemaDiffEngine` is the v2 engine contract (declared in `Interfaces/ISchemaDiffEngine.cs`, namespace `EfMigrationDiff.Interfaces`) for computing schema diffs between EF Core migration branches and for applying merge resolutions. It is the seam between the v2 pipeline and its implementation.

The same file also declares `IVisualDiffRenderer` and `IMergeEditor`. This document covers only `ISchemaDiffEngine`.

## Contract

- **Inputs** are lists of `SchemaChange` objects that were already extracted from migration source text. The engine does not read files or touch git.
- **Outputs** are model objects (`SchemaDiffResult`, `ThreeWayDiffResult`, `SchemaMergeResult`). The interface does not produce HTML or text; rendering belongs to `IVisualDiffRenderer`.
- **Options** are optional. Every method that takes `SchemaDiffOptions? options` treats `null` as `SchemaDiffOptions.Default`.
- **Null arguments** are rejected with `ArgumentNullException` in the current implementation.
- **Identity**: each computed result receives a fresh `Id` (`Guid.NewGuid()`). Results are not deterministic by identity, but their content is deterministic for the same inputs.
- **Pairing**: `ApplyMergeResolution` expects the `ThreeWayDiffResult` produced by `ComputeThreeWayDiff` and a `MergeResolutionPlan` keyed by the `MergeConflictRegion.Id` values from that result. The plan is usually built with the `IMergeEditor` methods.

## Members

### `SchemaDiffResult ComputeDiff(IReadOnlyList<SchemaChange> sourceChanges, IReadOnlyList<SchemaChange> targetChanges, SchemaDiffOptions? options = null)`

Computes a two-way diff between the schema changes of a source branch and a target branch.

- **Parameters**
  - `sourceChanges`: Schema changes detected in the source branch.
  - `targetChanges`: Schema changes detected in the target branch.
  - `options`: Optional diff configuration. `null` uses `SchemaDiffOptions.Default`.
- **Return value**: A `SchemaDiffResult` with:
  - `Hunks`: line-based hunks from an LCS edit script over the projected change lines.
  - `SourceOnlyChanges`: source changes whose key (`TableName|ChangeType|ColumnName`) does not appear in the target.
  - `TargetOnlyChanges`: target changes whose `TableName` and `ChangeType` do not both appear in the source.
  - `ModifiedChanges`: source changes whose key exists in the target but whose `Sql` text differs (ordinal comparison).
  - `SourceLabel` and `TargetLabel`, copied from `options`.
- **Exceptions**: `ArgumentNullException` if `sourceChanges` or `targetChanges` is `null`.

### `ThreeWayDiffResult ComputeThreeWayDiff(IReadOnlyList<SchemaChange> baseChanges, IReadOnlyList<SchemaChange> sourceChanges, IReadOnlyList<SchemaChange> targetChanges, SchemaDiffOptions? options = null)`

Computes a three-way diff that uses a common ancestor (base) together with the source and target branches.

- **Parameters**
  - `baseChanges`: Schema changes in the common ancestor branch.
  - `sourceChanges`: Schema changes in the source branch.
  - `targetChanges`: Schema changes in the target branch.
  - `options`: Optional diff configuration. `null` uses `SchemaDiffOptions.Default`.
- **Return value**: A `ThreeWayDiffResult` with:
  - `BaseToSource`: a two-way diff from base to source. Its labels are `options.BaseLabel` and `options.SourceLabel`.
  - `BaseToTarget`: a two-way diff from base to target. Its labels are `options.BaseLabel` and `options.TargetLabel`.
  - `ConflictRegions`: one `MergeConflictRegion` per conflict reported by `ConflictDetectionService`, built from the source and target changes. `BaseLines` holds the base changes whose table name appears in the region's source lines.
  - `BaseLabel`, `SourceLabel`, and `TargetLabel`, copied from `options`.
- **Exceptions**: `ArgumentNullException` if `baseChanges`, `sourceChanges`, or `targetChanges` is `null`.

### `SchemaMergeResult ApplyMergeResolution(ThreeWayDiffResult diff, MergeResolutionPlan plan)`

Applies a resolution plan to every conflict region of a three-way diff and returns the merged changes.

- **Parameters**
  - `diff`: The three-way diff whose `ConflictRegions` are resolved.
  - `plan`: Resolution decisions keyed by `MergeConflictRegion.Id`. Custom text is read from `plan.CustomContent`.
- **Return value**: A `SchemaMergeResult` with:
  - `ResolvedChanges`: changes produced for each resolved region, in region order.
  - `UnresolvedCount`: the number of regions with no plan entry, or with `MergeResolutionStrategy.Unresolved`.
  - `IsSuccessful`: `true` when `UnresolvedCount` is `0`.
  - `Warnings`: one message per unresolved region and one per unhandled strategy.
- **Exceptions**: `ArgumentNullException` if `diff` or `plan` is `null`.

Strategy handling in the current implementation:

| Strategy | Output |
|---|---|
| `AcceptSource` | Added lines of `SourceLines`, returned as `SchemaChange` items. |
| `AcceptTarget` | Added lines of `TargetLines`. |
| `AcceptBoth` | Source lines followed by target lines. |
| `Custom` | One `SchemaChange` with the custom text, only when `plan.CustomContent` has an entry for the region. |
| `Unresolved`, or no entry | Counted in `UnresolvedCount` and reported in `Warnings`. |

### `MergeResolutionPlan AcceptSource(ThreeWayDiffResult diff)`

Builds a plan that sets `MergeResolutionStrategy.AcceptSource` for every conflict region in `diff`.

- **Parameters**
  - `diff`: The three-way diff containing the conflict regions.
- **Return value**: A new `MergeResolutionPlan` with one entry per region.
- **Exceptions**: `ArgumentNullException` if `diff` is `null`.

This is a member of `IMergeEditor`, which `SchemaDiffEngine` also implements. It is listed here because it is the usual way to create a plan for `ApplyMergeResolution`.

### `MergeResolutionPlan AcceptTarget(ThreeWayDiffResult diff)`

Builds a plan that sets `MergeResolutionStrategy.AcceptTarget` for every conflict region in `diff`.

- **Parameters**
  - `diff`: The three-way diff containing the conflict regions.
- **Return value**: A new `MergeResolutionPlan` with one entry per region.
- **Exceptions**: `ArgumentNullException` if `diff` is `null`.

This is also an `IMergeEditor` member.

### `MergeResolutionPlan AutoMerge(ThreeWayDiffResult diff)`

Builds a plan in which each region that is trivially resolvable is set to `AcceptSource`, and every other region is set to `Unresolved`.

- **Parameters**
  - `diff`: The three-way diff containing the conflict regions.
- **Return value**: A new `MergeResolutionPlan` with one entry per region.
- **Exceptions**: `ArgumentNullException` if `diff` is `null`.

A region is trivially resolvable when its source and target lines have the same content in the same order (`MergeConflictRegion.IsTriviallyResolvable`).

This is also an `IMergeEditor` member.

### `IReadOnlyList<string> ValidateResolution(MergeResolutionPlan plan, ThreeWayDiffResult context)`

Checks a resolution plan against a three-way diff and reports every problem it finds.

- **Parameters**
  - `plan`: The plan to validate.
  - `context`: The three-way diff the plan is meant to resolve.
- **Return value**: A list of error messages. The list is empty when the plan is valid. The method reports:
  - a missing entry for a region;
  - a region still marked `Unresolved`;
  - a `Custom` strategy with no entry in `plan.CustomContent`.

This is an `IMergeEditor` member. `ApplyMergeResolution` does not call it. Callers that need the check must call it themselves.

## Implementation

The only implementation in the repository is `SchemaDiffEngine` (`Services/SchemaDiffEngine.cs`). It is `sealed` and implements both `ISchemaDiffEngine` and `IMergeEditor`. Its constructor takes a `ConflictDetectionService` and an `ILogger<SchemaDiffEngine>`. `Extensions/SchemaDiffServiceExtensions.cs` registers it as a singleton and exposes it as `ISchemaDiffEngine`. `Services/SchemaDiffPipelineService.cs` depends on `ISchemaDiffEngine`.

Points to know when relying on the current behavior:

- **Line projection.** `ComputeDiff` turns each change into text lines, sorted by `TableName` and then `ChangeType`. Inclusion of SQL and metadata lines and whitespace trimming follow `SchemaDiffOptions` (`IncludeSqlContent`, `IncludeMetadata`, `IgnoreWhitespace`).
- **Hunk limits.** `Hunks` is built as a single hunk. `SchemaDiffOptions.ContextLines` and `MaxHunkLines` are not applied here.
- **Edit script cost.** The LCS table is `O(m×n)` in time and space, where `m` and `n` are the line counts. The code comment says this is acceptable for migration files of a few hundred lines.
- **Asymmetric keys.** `SourceOnlyChanges` matches on table, change type, and column. `TargetOnlyChanges` matches only on table and change type, so a target change in the same table with the same change type is not reported as target-only even if its column differs.
- **Conflict sources.** Conflict regions come only from `ConflictDetectionService.DetectConflicts(source, target)`. The base changes are used only to fill `BaseLines`.
- **Custom strategy without content.** If `Custom` is set but `plan.CustomContent` has no entry, the code takes the default branch. It adds a warning and does not increment `UnresolvedCount`. `IsSuccessful` can therefore be `true` while `Warnings` is not empty. `SchemaMergeResult.IsClean` also checks `Warnings`, so it returns `false` in that case.
- **Custom output shape.** The `SchemaChange` created for custom content has an empty migration id, `SqlChangeType.Unknown`, and the region description stored in `TableName`.
- **Migration id of resolved lines.** Changes produced from `AcceptSource`, `AcceptTarget`, and `AcceptBoth` use the strategy name (for example `"AcceptSource"`) as their migration id, not the original migration id.
- **Plan validation.** `ApplyMergeResolution` does not call `ValidateResolution`. A plan can be applied without being validated.

## Example

```csharp
var engine = serviceProvider.GetRequiredService<ISchemaDiffEngine>();
var editor = (IMergeEditor)engine;   // SchemaDiffEngine implements both

var options = SchemaDiffOptions.ForMerge("main", "feature/a", "feature/b");
ThreeWayDiffResult diff = engine.ComputeThreeWayDiff(baseChanges, sourceChanges, targetChanges, options);

MergeResolutionPlan plan = editor.AutoMerge(diff);
IReadOnlyList<string> errors = editor.ValidateResolution(plan, diff);

if (errors.Count == 0)
{
    SchemaMergeResult merged = engine.ApplyMergeResolution(diff, plan);
}
```

The cast to `IMergeEditor` is needed only because the `IMergeEditor` methods are not on `ISchemaDiffEngine`.
