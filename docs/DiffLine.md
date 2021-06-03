# DiffLine

`DiffLine` is a sealed record that represents a single line within a diff hunk produced by the EF migration diff tool. It captures the state of a line across source, target, and base versions of a migration model snapshot, along with merge resolution metadata. Together with `DiffHunk`, it forms the core data structure for representing and resolving schema conflicts between divergent migration branches.

## API

### DiffLine

A sealed record modelling one line of a diff hunk, identified by a unique `Id` and carrying resolution instructions.

**Members:**

- `public required Guid Id`  
  A unique identifier for this diff line. Required at construction; used to correlate resolution decisions stored in parent dictionaries.

- `public required int HunkIndex`  
  The zero-based index of this line within its parent `DiffHunk`. Required at construction.

- `public required string Description`  
  A human-readable description of the change represented by this line (e.g. "Added column", "Altered index"). Required at construction.

- `public IReadOnlyList<DiffLine> SourceLines`  
  The collection of lines from the source migration snapshot that correspond to this diff line. May be empty if the line has no source representation.

- `public IReadOnlyList<DiffLine> TargetLines`  
  The collection of lines from the target migration snapshot that correspond to this diff line. May be empty if the line has no target representation.

- `public IReadOnlyList<DiffLine> BaseLines`  
  The collection of lines from the common base snapshot. Provides the ancestral state before source and target diverged.

- `public MergeResolutionStrategy Resolution`  
  The merge resolution strategy applied to this specific line. Determines whether the source, target, or a custom value is used when merging.

- `public string? CustomContent`  
  When `Resolution` is set to a strategy requiring custom input, this holds the user-supplied content. `null` otherwise.

### DiffHunk

A sealed record representing a contiguous block of differing lines between source and target migration snapshots.

**Members:**

- `public required Guid Id`  
  A unique identifier for this hunk. Required at construction.

- `public required string SourceLabel`  
  A label describing the source branch or context (e.g. branch name, migration identifier). Required at construction.

- `public required string TargetLabel`  
  A label describing the target branch or context. Required at construction.

- `public IReadOnlyList<DiffHunk> Hunks`  
  The collection of child hunks contained within this hunk. Allows hierarchical diff structures.

- `public IReadOnlyList<SchemaChange> SourceOnlyChanges`  
  Schema changes present only in the source migration snapshot, with no counterpart in the target.

- `public IReadOnlyList<SchemaChange> TargetOnlyChanges`  
  Schema changes present only in the target migration snapshot, with no counterpart in the source.

- `public Dictionary<Guid, MergeResolutionStrategy> Resolutions`  
  A dictionary mapping each `DiffLine.Id` within this hunk to its chosen merge resolution strategy. Keys correspond to `DiffLine.Id` values.

- `public Dictionary<Guid, string> CustomContent`  
  A dictionary mapping `DiffLine.Id` values to custom content strings for lines whose resolution strategy requires manual input.

- `public bool IsComplete`  
  Indicates whether all lines within this hunk have been assigned a resolution strategy. Returns `true` when every `DiffLine` has an entry in `Resolutions`.

- `public int CountByStrategy`  
  Returns the total number of `DiffLine` entries that have a resolution strategy assigned. Equivalent to the count of entries in `Resolutions`.

## Usage

### Example 1: Iterating hunks and inspecting line-level details

```csharp
DiffHunk rootHunk = diffResult.RootHunk;

foreach (var hunk in rootHunk.Hunks)
{
    Console.WriteLine($"Hunk {hunk.Id}: {hunk.SourceLabel} vs {hunk.TargetLabel}");

    foreach (var line in hunk.SourceLines)
    {
        Console.WriteLine($"  Line {line.Id} [{line.HunkIndex}]: {line.Description}");
        Console.WriteLine($"    Resolution: {line.Resolution}");

        if (line.CustomContent is not null)
        {
            Console.WriteLine($"    Custom: {line.CustomContent}");
        }
    }

    Console.WriteLine($"  Complete: {hunk.IsComplete}");
    Console.WriteLine($"  Resolved lines: {hunk.CountByStrategy}");
}
```

### Example 2: Programmatically applying merge resolutions

```csharp
DiffHunk hunk = GetConflictingHunk();

foreach (var line in hunk.SourceLines)
{
    // Default to accepting source changes
    hunk.Resolutions[line.Id] = MergeResolutionStrategy.AcceptSource;
}

// Override a specific line with custom content
Guid specificLineId = hunk.SourceLines.First(l => l.Description.Contains("index")).Id;
hunk.Resolutions[specificLineId] = MergeResolutionStrategy.Custom;
hunk.CustomContent[specificLineId] = """
    CREATE INDEX [IX_NewIndex] ON [dbo].[Orders] ([CustomerId])
    """;

bool readyToMerge = hunk.IsComplete; // true if all lines have resolutions
```

## Notes

- **Immutability:** Both `DiffLine` and `DiffHunk` are `sealed record` types, making them value-equal and immutable with respect to their properties. The `Resolutions` and `CustomContent` dictionaries on `DiffHunk` are mutable reference types, allowing incremental population of resolution data after the hunk is constructed.
- **Thread safety:** The records themselves are safe to read concurrently. The dictionaries `Resolutions` and `CustomContent` are not thread-safe; concurrent writes or mixed read/write access must be externally synchronised.
- **Hierarchy:** `DiffHunk` contains a `Hunks` property of type `IReadOnlyList<DiffHunk>`, enabling nested diff structures. Circular references are not prevented by the type system and must be avoided at construction time.
- **Completeness detection:** `IsComplete` relies on every `DiffLine.Id` present in the hunk having a corresponding key in `Resolutions`. If lines are added to a hunk after resolution dictionaries are populated, `IsComplete` may return `false` until those new lines are resolved.
- **Line identity:** `DiffLine.Id` values must be unique within the scope of a `DiffHunk`'s resolution dictionaries. Duplicate GUIDs will cause dictionary entries to collide, silently overwriting previous resolutions.
- **Empty collections:** `SourceLines`, `TargetLines`, and `BaseLines` may all be empty for a given `DiffLine`, representing a line that exists only as a structural placeholder in the diff output.
