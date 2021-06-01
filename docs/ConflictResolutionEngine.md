# ConflictResolutionEngine

A conflict resolution engine for Entity Framework migrations that analyzes, categorizes, and resolves conflicts between database schemas and migration scripts. It provides batch conflict resolution capabilities and supports custom resolution strategies with prioritization and risk assessment.

## API

### `public ConflictResolutionEngine()`
Initializes a new instance of the `ConflictResolutionEngine` with default settings. No conflicts are analyzed until `ResolveBatch` or individual conflict resolution methods are invoked.

### `public ConflictResolution ResolveConflict(Conflict conflict)`
Resolves a single conflict using the engine's registered strategies and prioritization rules.
- **Parameters**: `conflict` – The conflict to resolve.
- **Return value**: A `ConflictResolution` object describing the chosen resolution and its justification.
- **Throws**: `ArgumentNullException` if `conflict` is `null`.

### `public ConflictResolutionReport ResolveBatch(IEnumerable<Conflict> conflicts)`
Resolves a batch of conflicts in a single operation, applying prioritization and risk-based ordering.
- **Parameters**: `conflicts` – An enumerable of conflicts to resolve.
- **Return value**: A `ConflictResolutionReport` containing resolutions, statistics, and recommendations.
- **Throws**: `ArgumentNullException` if `conflicts` is `null`.

### `public void RegisterStrategy(ResolutionStrategy strategy)`
Registers a custom resolution strategy for use during conflict resolution.
- **Parameters**: `strategy` – The strategy to register.
- **Throws**: `ArgumentNullException` if `strategy` is `null`.

### `public ResolutionType Type { get; }`
Gets the type of resolution applied by this engine instance.
- **Return value**: A `ResolutionType` enum value indicating the resolution mode (e.g., automatic, manual, hybrid).

### `public string Description { get; }`
Gets a human-readable description of the engine's current configuration or state.

### `public int Priority { get; set; }`
Gets or sets the priority of this engine instance relative to others. Higher values indicate higher priority during conflict resolution.

### `public bool IsHighRisk { get; }`
Indicates whether this engine instance is configured to handle high-risk conflicts.

### `public string ConflictId { get; }`
Gets the unique identifier of the conflict currently being processed, if any.

### `public EfMigrationDiff.Models.ConflictType ConflictType { get; }`
Gets the type of the conflict currently being processed, if any.

### `public DateTime AnalyzedAt { get; }`
Gets the timestamp when the current conflict or batch was last analyzed.

### `public ConflictSeverity Severity { get; }`
Gets the severity level of the conflict currently being processed, if any.

### `public ResolutionStrategy RecommendedStrategy { get; }`
Gets the strategy recommended by the engine for resolving the current conflict, if any.

### `public List<string> Recommendations { get; }`
Gets a list of textual recommendations for resolving the current conflict or batch.

### `public DateTime AnalyzedAt { get; }`
Gets the timestamp when the current conflict or batch was last analyzed.

### `public List<ConflictResolution> Resolutions { get; }`
Gets the list of resolutions applied during the current batch or conflict resolution process.

### `public int TotalConflicts { get; }`
Gets the total number of conflicts processed in the current batch.

### `public int CriticalCount { get; }`
Gets the number of critical conflicts identified in the current batch.

### `public int HighCount { get; }`
Gets the number of high-severity conflicts identified in the current batch.

### `public int CanAutoResolve { get; }`
Gets the number of conflicts in the current batch that can be resolved automatically.

## Usage

### Example 1: Resolving a single conflict
