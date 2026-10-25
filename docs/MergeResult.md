# MergeResult

`MergeResult` aggregates the outcome of one automated batch conflict-resolution run. It is produced by `MigrationAutoResolverService.ResolveAsync` and separates conflicts that were auto-resolved from those that need manual review. Each conflict processed gets one `MergeAttempt` in `Attempts`, and every conflict the resolver could not handle is kept in `UnresolvedConflicts`.

The type lives in `Models/MergeResult.cs` (namespace `EfMigrationDiff.Models`). Related types in the same file are `MergeStrategy` (the enum of strategies) and `MergeAttempt` (one per-conflict attempt, documented in [MergeAttempt.md](MergeAttempt.md)).

## API

### Properties

#### `Id`
`public string Id { get; set; }`

Unique identifier for this run, a GUID string generated on construction. Used in `ToString()` and in `ToDetailedSummary()`, which shows the first 8 characters.

#### `ResolvedAt`
`public DateTime ResolvedAt { get; set; }`

UTC timestamp set on construction (`DateTime.UtcNow`). Despite the name, it is not updated when the run finishes. It marks when the result object was created, which is effectively the start of the resolution run.

#### `Attempts`
`public List<MergeAttempt> Attempts { get; set; }`

One `MergeAttempt` per conflict processed, in the order the conflicts were supplied. The resolver makes exactly one attempt per conflict, so this list is not a retry history. Attempts for conflicts that were resolved and for those that failed are both recorded here.

#### `UnresolvedConflicts`
`public List<ConflictInfo> UnresolvedConflicts { get; set; }`

The conflicts whose attempt did not succeed. These are the items a person must review. The original `ConflictInfo` instances are stored, so their state is available to the caller.

#### `IsFullyResolved`
`public bool IsFullyResolved => UnresolvedConflicts.Count == 0 && Attempts.All(a => a.Succeeded);`

Computed property. `true` when no conflicts are unresolved and every attempt succeeded. An empty result (no conflicts at all) returns `true`.

#### `TotalConflicts`
`public int TotalConflicts => Attempts.Count;`

Number of conflicts processed. Computed from `Attempts`.

#### `ResolvedCount`
`public int ResolvedCount => Attempts.Count(a => a.Succeeded);`

Number of attempts that succeeded.

#### `UnresolvedCount`
`public int UnresolvedCount => UnresolvedConflicts.Count;`

Number of conflicts that need manual intervention.

#### `HasBlockingConflicts`
`public bool HasBlockingConflicts => UnresolvedConflicts.Any(c => c.IsBlocking());`

`true` when at least one unresolved conflict has `Severity` of `Error` or `Critical`. Those severities are never auto-resolved, so any such conflict in the batch makes this `true`. Unresolved `Warning` conflicts do not block.

### Methods

#### `GetSummary`
`public string GetSummary()`

Returns a one-line outcome message:

| Condition | Result |
|---|---|
| `TotalConflicts == 0` | `No conflicts to resolve.` |
| `IsFullyResolved` | `All {ResolvedCount} conflict(s) auto-resolved successfully.` |
| otherwise | `{ResolvedCount}/{TotalConflicts} conflict(s) auto-resolved; {UnresolvedCount} require manual intervention.` |

**Returns:** `string`. `Program.cs` prints this value after an auto-merge run.

#### `ToString`
`public override string ToString()`

Returns `MergeResult {Id} — {ResolvedCount}/{TotalConflicts} resolved, {UnresolvedCount} unresolved`, with ` [BLOCKING]` appended when `HasBlockingConflicts` is `true`. Suitable for logs.

**Returns:** `string`.

### Related extensions

These are in `Models/MergeResultExtensions.cs` and `Extensions/AutoResolverExtensions.cs`. They are not members of `MergeResult`.

| Method | Purpose |
|---|---|
| `IsSuccessful()` | Same as `IsFullyResolved`. |
| `IsSafeToMerge()` | `IsFullyResolved` and not `HasBlockingConflicts`. Use this as the deploy gate. |
| `ToDetailedSummary()` | Multi-line report: counts, blocking status, each attempt, and each conflict needing review. |
| `GroupUnresolvedByType()` | Groups `UnresolvedConflicts` by `ConflictType` for triage. |

All of these throw `ArgumentNullException` when `result` is `null`.

## Usage

### Typical merge workflow

1. Compare the two branches to get the conflicts (`MigrationDiffService.CompareBranches(...).Conflicts`).
2. Pass those conflicts to `MigrationAutoResolverService.ResolveAsync`. The service skips `Error` and `Critical` conflicts, applies the registered strategy for each remaining conflict type, and calls `MarkResolved` on the conflicts it fixes.
3. Read the returned `MergeResult`:
   - `GetSummary()` for a short message.
   - `Attempts` for per-conflict details, including the `MergedContent` of successful attempts.
   - `UnresolvedConflicts` for the work left to a person.
4. Gate the deployment on `IsSafeToMerge()`, or on `HasBlockingConflicts` if you want to allow non-blocking warnings through.

### Example 1: Checking the outcome of a run

```csharp
var result = await resolverService.ResolveAsync(diff.Conflicts, cancellationToken);

Console.WriteLine(result.GetSummary());

if (!result.IsSafeToMerge())
{
    Console.WriteLine(result.ToDetailedSummary());
    Environment.Exit(1);
}
```

### Example 2: Reviewing what still needs attention

```csharp
var result = await resolverService.ResolveAsync(diff.Conflicts);

foreach (var (type, conflicts) in result.GroupUnresolvedByType())
{
    Console.WriteLine($"{type}: {conflicts.Count} conflict(s) need review");
}
```

### Example 3: Collecting the merged SQL

```csharp
var result = await resolverService.ResolveAsync(diff.Conflicts);

var mergedSql = result.Attempts
    .Where(a => a.Succeeded && a.MergedContent is not null)
    .Select(a => a.MergedContent!);
```

`MergedContent` is `null` for a successful `Skip` attempt, so the `is not null` filter is required.

## Notes

- **Blocking versus unresolved:** `UnresolvedConflicts` is not the same as blocking. A `Warning` conflict that could not be auto-resolved is unresolved but not blocking. Use `HasBlockingConflicts` (or `IsSafeToMerge()`) to decide whether to stop the deployment.
- **Already-resolved conflicts:** If a conflict is passed in with `IsResolved` already `true`, its attempt fails with "Conflict is already marked as resolved." The resolver still adds that conflict to `UnresolvedConflicts`. Because of this, `UnresolvedCount` can count a conflict that was never actually broken. Filter out resolved conflicts before calling `ResolveAsync` if that matters for your report.
- **Exceptions become failures:** An exception thrown by a strategy is caught, recorded as a failed attempt with the exception message as `FailureReason`, and the conflict moves to `UnresolvedConflicts`. The run continues with the next conflict. Cancellation is checked before each conflict, and a cancelled token stops the run with `OperationCanceledException`.
- **Empty runs are successful:** A run with no conflicts returns a result where `IsFullyResolved` is `true`, `IsSafeToMerge()` is `true`, and `GetSummary()` returns `No conflicts to resolve.`
- **`ResolvedAt` is set at construction:** Do not use it to measure how long the run took.
- **Thread safety:** The type is not thread-safe. Create one result per run and do not share it across threads without external synchronization.
