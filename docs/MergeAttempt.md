# MergeAttempt

Represents a single attempt to resolve a schema conflict during EF Core migration merging. Each `MergeAttempt` records the strategy applied, whether it succeeded, the resulting merged content, and any failure details. Multiple attempts can be chained together for the same conflict, forming a resolution history accessible through the `Attempts` collection.

## API

### Properties

#### `ConflictId`
`public string ConflictId`

Identifies the conflict this attempt belongs to. This value links the attempt to a specific `ConflictInfo` instance and remains consistent across all attempts for the same conflict.

#### `ConflictType`
`public ConflictType ConflictType`

Indicates the category of conflict being resolved (e.g., table addition, column modification, index change). Determined at the time the conflict is first detected and immutable for all attempts against that conflict.

#### `StrategyApplied`
`public MergeStrategy StrategyApplied`

The merge strategy used in this specific attempt. Different attempts for the same conflict may try different strategies if earlier ones fail.

#### `Succeeded`
`public bool Succeeded`

Whether the strategy application produced a valid merged result without errors. When `false`, `FailureReason` should contain an explanation.

#### `FailureReason`
`public string? FailureReason`

Null when `Succeeded` is `true`. When `Succeeded` is `false`, contains a human-readable description of why the strategy failed (e.g., "Irreconcilable column type mismatch", "Circular dependency detected").

#### `MergedContent`
`public string? MergedContent`

The resulting migration code or schema representation after applying the strategy. Null if the attempt failed. When present, this content is the candidate for final resolution.

#### `AttemptedAt`
`public DateTime AttemptedAt`

UTC timestamp of when this attempt was executed. Set automatically at creation time.

#### `Id`
`public string Id`

Unique identifier for this individual attempt instance. Distinct from `ConflictId`, which groups multiple attempts.

#### `ResolvedAt`
`public DateTime ResolvedAt`

UTC timestamp of when the conflict was ultimately resolved. Set on the successful attempt that finalized the resolution. For unsuccessful attempts, this may remain at its default value or reflect the time of the last attempt in the chain.

#### `Attempts`
`public List<MergeAttempt> Attempts`

The full history of attempts for this conflict, including the current instance. Ordered chronologically. Allows traversing the resolution path to understand what strategies were tried and why earlier ones failed.

#### `UnresolvedConflicts`
`public List<ConflictInfo> UnresolvedConflicts`

When the attempt fails, this list contains any sub-conflicts or remaining unresolved issues discovered during the strategy application. Empty when `Succeeded` is `true`. Enables recursive or iterative resolution of nested conflicts.

### Methods

#### `ToString`
`public override string ToString()`

Returns a string representation including the `ConflictId`, `StrategyApplied`, and `Succeeded` status. Suitable for logging and debugging output.

**Returns:** `string` — formatted summary of the attempt.

#### `GetSummary`
`public string GetSummary()`

Produces a detailed human-readable summary of the attempt, including strategy name, success/failure status, timestamp, and failure reason if applicable. More verbose than `ToString`, intended for reports and user-facing diagnostics.

**Returns:** `string` — multi-line or structured summary text.

## Usage

### Example 1: Recording a Successful Merge Attempt

```csharp
var conflict = new ConflictInfo
{
    ConflictId = "conflict-001",
    ConflictType = ConflictType.TableAddition,
    Description = "Both migrations add a table named 'Orders'"
};

var attempt = new MergeAttempt
{
    ConflictId = conflict.ConflictId,
    ConflictType = conflict.ConflictType,
    StrategyApplied = MergeStrategy.KeepBothWithRename,
    Succeeded = true,
    MergedContent = "migrationBuilder.CreateTable(name: 'Orders_Legacy', ...)",
    AttemptedAt = DateTime.UtcNow,
    ResolvedAt = DateTime.UtcNow
};

conflict.Attempts.Add(attempt);

Console.WriteLine(attempt.GetSummary());
// Output: Strategy 'KeepBothWithRename' succeeded at 2025-03-15T10:30:00Z.
```

### Example 2: Handling a Failed Attempt and Retrying

```csharp
var firstAttempt = new MergeAttempt
{
    ConflictId = "conflict-002",
    ConflictType = ConflictType.ColumnModification,
    StrategyApplied = MergeStrategy.TakeSource,
    Succeeded = false,
    FailureReason = "Column type mismatch: source has 'nvarchar(max)', target has 'int'",
    AttemptedAt = DateTime.UtcNow
};

// Store the failed attempt
var attempts = new List<MergeAttempt> { firstAttempt };

// Retry with a different strategy
var secondAttempt = new MergeAttempt
{
    ConflictId = "conflict-002",
    ConflictType = ConflictType.ColumnModification,
    StrategyApplied = MergeStrategy.ManualResolution,
    Succeeded = true,
    MergedContent = "migrationBuilder.AlterColumn<string>(name: 'Status', ...)",
    AttemptedAt = DateTime.UtcNow,
    ResolvedAt = DateTime.UtcNow,
    Attempts = attempts
};

attempts.Add(secondAttempt);

if (secondAttempt.Succeeded)
{
    Console.WriteLine($"Resolved after {attempts.Count} attempt(s).");
    Console.WriteLine(secondAttempt.MergedContent);
}
```

## Notes

- **Chaining attempts:** The `Attempts` list is a reference to the shared history collection. Modifying it on one instance affects all instances that share the same list reference. Ensure you initialize `Attempts` once per conflict and assign the same list instance to each new `MergeAttempt`.
- **Null `MergedContent`:** Always check `Succeeded` before accessing `MergedContent`. A failed attempt with a non-null `MergedContent` is an invalid state and should not occur in normal operation.
- **`ResolvedAt` on failed attempts:** The `ResolvedAt` timestamp is only meaningful on the successful attempt that ends the resolution chain. Consumers should treat `ResolvedAt` as undefined when `Succeeded` is `false`.
- **Thread safety:** This type is not thread-safe. If multiple threads may create attempts for the same conflict concurrently, external synchronization (e.g., locking on the shared `Attempts` list) is required to avoid race conditions when appending to the history.
- **`UnresolvedConflicts` lifecycle:** This list is populated only when a strategy partially resolves a conflict but exposes new sub-conflicts. Recursive resolution should process these before marking the parent conflict as resolved.
