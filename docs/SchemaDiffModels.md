# SchemaDiffModels

`Models/SchemaDiffModels.cs` defines the data types used by the v2 schema diff pipeline (`SchemaDiffEngine`, `SchemaDiffPipelineService`) to represent two-way and three-way comparisons of EF Core migration schemas, line-level diff output, merge conflict regions, resolution plans, and merge results. All types live in the `EfMigrationDiff.Models` namespace.

The file contains:

| Type | Kind | Purpose |
|---|---|---|
| `DiffLineKind` | enum | Classifies a line in a diff view |
| `MergeResolutionStrategy` | enum | How a conflict region is resolved |
| `DiffLine` | sealed record | One rendered line in a diff view |
| `DiffHunk` | sealed record | A contiguous block of changed and context lines |
| `MergeConflictRegion` | sealed class | A region where source and target both diverged from base |
| `MergeResolutionPlan` | sealed class | Resolution decisions for a set of conflict regions |
| `SchemaDiffResult` | sealed class | Output of a two-way diff |
| `ThreeWayDiffResult` | sealed class | Output of a three-way diff (base, source, target) |
| `SchemaMergeResult` | sealed record | Output of applying a merge resolution plan |

Several types use `[JsonIgnore]` on their computed (derived) members, so those members are not included when the models are serialized. Only stored properties and constructor parameters are serialized.

## API

### DiffLineKind

An enum describing how a line appears in a diff view.

**Members:**

- `Unchanged = 0`  
  The line is identical in source and target.

- `Removed = 1`  
  The line exists only in the source side (removed relative to target).

- `Added = 2`  
  The line exists only in the target side (added relative to source).

- `Modified = 3`  
  The line is present on both sides, but its content differs.

- `Placeholder = 4`  
  An empty filler line used to keep the opposite column aligned in a side-by-side view.

### MergeResolutionStrategy

An enum describing how a three-way merge conflict region is resolved.

**Members:**

- `Unresolved = 0`  
  No resolution has been chosen. This is the default value.

- `AcceptSource = 1`  
  Keep the source-branch version.

- `AcceptTarget = 2`  
  Keep the target-branch version.

- `AcceptBoth = 3`  
  Include both versions, source content first, then target content.

- `Custom = 4`  
  Use a manually supplied value. `SchemaDiffEngine` reads that value from `MergeResolutionPlan.CustomContent`.

### DiffLine

A sealed positional record representing one rendered line within a schema diff view.

**Constructor parameters:**

- `DiffLineKind Kind`  
  How the line is classified.

- `int LineNumber`  
  The source or target line number. `0` for placeholder lines.

- `string Content`  
  The textual content of the line.

- `int CorrespondingLineNumber = 0`  
  In side-by-side views, the matching line number on the opposite pane. `0` if there is no counterpart.

**Members:**

- `public bool IsChanged { get; }` (`[JsonIgnore]`)  
  Returns `true` when `Kind` is `Added`, `Removed`, or `Modified`.

- `public char UnifiedMarker { get; }` (`[JsonIgnore]`)  
  The unified-diff marker: `'+'` for `Added`, `'-'` for `Removed`, and `' '` for every other kind (including `Modified` and `Placeholder`).

### DiffHunk

A sealed positional record grouping a contiguous block of diff lines that form one logical change region.

**Constructor parameters:**

- `int SourceStart`  
  One-based starting line number on the source side.

- `int TargetStart`  
  One-based starting line number on the target side.

- `IReadOnlyList<DiffLine> Lines`  
  The ordered lines in the hunk, including context lines.

**Members:**

- `public int AddedCount { get; }` (`[JsonIgnore]`)  
  The number of lines with kind `Added`.

- `public int RemovedCount { get; }` (`[JsonIgnore]`)  
  The number of lines with kind `Removed`.

- `public bool HasChanges { get; }` (`[JsonIgnore]`)  
  Returns `true` when at least one line satisfies `IsChanged`.

### MergeConflictRegion

A sealed class representing a region in a three-way diff where source and target both diverged from the common base. The region must be resolved before a clean merge is possible.

**Required properties (set in an object initializer):**

- `public required Guid Id { get; init; }`  
  Unique identifier, referenced by `MergeResolutionPlan` dictionaries.

- `public required int HunkIndex { get; init; }`  
  Zero-based index of the first conflicting hunk in the overall diff sequence.

- `public required string Description { get; init; }`  
  Human-readable description of the conflicting schema elements.

**Optional properties:**

- `public IReadOnlyList<DiffLine> SourceLines { get; init; } = []`  
  Source-branch version of the conflicting content.

- `public IReadOnlyList<DiffLine> TargetLines { get; init; } = []`  
  Target-branch version of the conflicting content.

- `public IReadOnlyList<DiffLine> BaseLines { get; init; } = []`  
  Common-ancestor version, provided for reference context.

- `public MergeResolutionStrategy Resolution { get; set; } = MergeResolutionStrategy.Unresolved`  
  The strategy chosen for this region. This property is settable after construction.

- `public string? CustomContent { get; set; }`  
  Custom merged content recorded on the region. It is not read by `SchemaDiffEngine`, which uses `MergeResolutionPlan.CustomContent` instead.

**Members:**

- `public bool IsResolved { get; }` (`[JsonIgnore]`)  
  Returns `true` when `Resolution` is not `Unresolved`.

- `public bool IsTriviallyResolvable { get; }` (`[JsonIgnore]`)  
  Returns `true` when the `Content` values of `SourceLines` and `TargetLines` match in order. Both sides made the same change, so the region can be resolved without a decision.

### MergeResolutionPlan

A sealed class that holds the resolution decisions for a set of conflict regions. It is passed to `ISchemaDiffEngine.ApplyMergeResolution` to produce a merged schema.

**Properties:**

- `public Dictionary<Guid, MergeResolutionStrategy> Resolutions { get; init; } = new()`  
  Maps each `MergeConflictRegion.Id` to its chosen strategy.

- `public Dictionary<Guid, string> CustomContent { get; init; } = new()`  
  Custom content for regions resolved with `Custom`, keyed by `MergeConflictRegion.Id`. This dictionary is separate from `MergeConflictRegion.CustomContent`.

**Methods:**

- `public bool IsComplete(IEnumerable<MergeConflictRegion> regions)`  
  Returns `true` when every region has an entry in `Resolutions` whose value is not `Unresolved`. Throws `ArgumentNullException` when `regions` is `null`.

- `public int CountByStrategy(MergeResolutionStrategy strategy)`  
  Counts the entries in `Resolutions` whose value equals `strategy`.

### SchemaDiffResult

A sealed class holding the output of a two-way diff between a source and a target branch.

**Required properties:**

- `public required Guid Id { get; init; }`  
  Unique identifier of this computation.

- `public required string SourceLabel { get; init; }`  
  Display label for the source (left) side.

- `public required string TargetLabel { get; init; }`  
  Display label for the target (right) side.

**Optional properties:**

- `public IReadOnlyList<DiffHunk> Hunks { get; init; } = []`  
  Ordered hunks that cover all changed regions.

- `public IReadOnlyList<SchemaChange> SourceOnlyChanges { get; init; } = []`  
  Schema changes present only in the source branch.

- `public IReadOnlyList<SchemaChange> TargetOnlyChanges { get; init; } = []`  
  Schema changes present only in the target branch.

- `public IReadOnlyList<SchemaChange> ModifiedChanges { get; init; } = []`  
  Schema changes present in both branches with different SQL content.

- `public DateTime ComputedAt { get; init; } = DateTime.UtcNow`  
  UTC timestamp of when the diff was computed.

**Members:**

- `public int TotalAdded { get; }` (`[JsonIgnore]`)  
  Sum of `AddedCount` across all hunks.

- `public int TotalRemoved { get; }` (`[JsonIgnore]`)  
  Sum of `RemovedCount` across all hunks.

- `public bool IsIdentical { get; }` (`[JsonIgnore]`)  
  Returns `true` when no hunk has changes. Note that this is based on `Hunks` only, not on the schema change lists.

- `public bool HasDestructiveChanges { get; }` (`[JsonIgnore]`)  
  Returns `true` when any change in `SourceOnlyChanges` or `TargetOnlyChanges` is destructive, for example dropping a table or column. `ModifiedChanges` is not checked.

### ThreeWayDiffResult

A sealed class holding the output of a three-way diff across a base, a source, and a target branch.

**Required properties:**

- `public required Guid Id { get; init; }`  
  Unique identifier of this computation.

- `public required string BaseLabel { get; init; }`  
  Label for the common-ancestor branch.

- `public required string SourceLabel { get; init; }`  
  Label for the source branch.

- `public required string TargetLabel { get; init; }`  
  Label for the target branch.

- `public required SchemaDiffResult BaseToSource { get; init; }`  
  Two-way diff from base to source.

- `public required SchemaDiffResult BaseToTarget { get; init; }`  
  Two-way diff from base to target.

**Optional properties:**

- `public IReadOnlyList<MergeConflictRegion> ConflictRegions { get; init; } = []`  
  Regions where source and target diverged from base in different ways.

- `public DateTime ComputedAt { get; init; } = DateTime.UtcNow`  
  UTC timestamp of when the computation was performed.

**Members:**

- `public bool HasUnresolvedConflicts { get; }` (`[JsonIgnore]`)  
  Returns `true` when any region has `IsResolved` equal to `false`. This checks the region's own `Resolution` property, not a `MergeResolutionPlan`.

- `public int ConflictCount { get; }` (`[JsonIgnore]`)  
  The number of conflict regions.

- `public bool IsAutoMergeable { get; }` (`[JsonIgnore]`)  
  Returns `true` when every region has `IsTriviallyResolvable` equal to `true`. This is also `true` when there are no regions.

### SchemaMergeResult

A sealed positional record holding the result of applying a `MergeResolutionPlan` to a `ThreeWayDiffResult`.

**Constructor parameters:**

- `bool IsSuccessful`  
  `true` when all conflict regions were fully resolved.

- `IReadOnlyList<SchemaChange> ResolvedChanges`  
  The merged set of schema changes.

- `int UnresolvedCount`  
  The number of regions with no valid resolution in the plan.

- `IReadOnlyList<string> Warnings`  
  Non-fatal issues found while applying the merge.

**Members:**

- `public bool IsClean { get; }` (`[JsonIgnore]`)  
  Returns `true` when `IsSuccessful` is `true` and `Warnings` is empty.

- `public int ChangeCount { get; }` (`[JsonIgnore]`)  
  The number of entries in `ResolvedChanges`.

## Usage

### Example 1: Reading line-level diff output

```csharp
DiffHunk hunk = new(
    SourceStart: 10,
    TargetStart: 10,
    Lines:
    [
        new DiffLine(DiffLineKind.Unchanged, 10, "CREATE TABLE [dbo].[Orders] (", 10),
        new DiffLine(DiffLineKind.Removed, 11, "    [Total] decimal(10,2) NOT NULL,", 0),
        new DiffLine(DiffLineKind.Added, 0, "    [Total] decimal(18,2) NOT NULL,", 11),
    ]);

Console.WriteLine($"+{hunk.AddedCount} -{hunk.RemovedCount}, changes: {hunk.HasChanges}");

foreach (var line in hunk.Lines)
{
    Console.WriteLine($"{line.UnifiedMarker} {line.Content}");
}
```

### Example 2: Building a resolution plan for conflict regions

```csharp
var region = new MergeConflictRegion
{
    Id = Guid.NewGuid(),
    HunkIndex = 2,
    Description = "Column [Total] altered on both branches"
};

var plan = new MergeResolutionPlan();
plan.Resolutions[region.Id] = MergeResolutionStrategy.Custom;
plan.CustomContent[region.Id] = "    [Total] decimal(18,2) NOT NULL,";

bool complete = plan.IsComplete([region]);           // true
int customCount = plan.CountByStrategy(MergeResolutionStrategy.Custom); // 1
```

### Example 3: Checking whether a three-way diff can be merged automatically

```csharp
ThreeWayDiffResult diff = GetThreeWayDiff();

if (diff.IsAutoMergeable)
{
    Console.WriteLine("All conflicts are identical on both sides.");
}
else if (diff.HasUnresolvedConflicts)
{
    Console.WriteLine($"{diff.ConflictCount} conflict(s) need a decision.");
}
```

## Notes

- **Immutability:** `DiffLine`, `DiffHunk`, and `SchemaMergeResult` are records, and their properties are init-only. `MergeConflictRegion` allows changes to `Resolution` and `CustomContent` after construction. `MergeResolutionPlan` exposes mutable dictionaries through `init` properties.
- **Thread safety:** The types have no internal synchronization. Concurrent reads are safe as long as nothing writes. Writes to `MergeResolutionPlan` dictionaries or to `MergeConflictRegion` properties must be synchronized externally.
- **Identifiers:** `MergeConflictRegion.Id` and the keys of `MergeResolutionPlan.Resolutions` and `MergeResolutionPlan.CustomContent` must match. `IsComplete` and `CountByStrategy` do not check that keys correspond to actual regions. `CountByStrategy` counts dictionary entries, so stale keys are counted too.
- **Two places for custom content:** `MergeConflictRegion.CustomContent` and `MergeResolutionPlan.CustomContent` are independent. `SchemaDiffEngine.ApplyMergeResolution` reads only the plan's dictionaries (`Resolutions` and `CustomContent`), so a `Custom` strategy without an entry in `plan.CustomContent` is left unapplied. `ValidateResolution` reports that case as an error.
- **Resolution state:** The engine also ignores `MergeConflictRegion.Resolution`. Decisions take effect only through `MergeResolutionPlan.Resolutions`. `ThreeWayDiffResult.HasUnresolvedConflicts` reads each region's own `Resolution`, so it does not reflect a plan that has not been copied onto the regions.
- **Empty collections:** An empty `ConflictRegions` list makes `IsAutoMergeable` return `true`. An empty `Hunks` list makes `IsIdentical` return `true`.
- **Serialization:** Computed members are marked `[JsonIgnore]`, so JSON output contains stored state only. Consumers that need `IsResolved`, `TotalAdded`, or similar values must derive them after deserializing.
