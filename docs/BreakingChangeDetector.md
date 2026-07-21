# BreakingChangeDetector

The `BreakingChangeDetector` type is a utility class designed to analyze differences between Entity Framework Core migration snapshots and identify changes that may break existing functionality. It classifies detected changes into categories based on their potential impact, enabling developers to assess and address compatibility risks before applying migrations.

## API

### `BreakingChangeDetector`
The primary class constructor. Initializes a new instance of the `BreakingChangeDetector` with default configuration for change classification.

### `List<BreakingChangeClassification> ClassifyChanges(IEnumerable<DiffResult> changes)`
Analyzes a collection of migration differences and returns a list of classified breaking changes.

**Parameters:**
- `changes` - An enumerable collection of `DiffResult` objects representing the differences between migration snapshots.

**Returns:**
A list of `BreakingChangeClassification` records, each describing a detected breaking change and its severity.

**Throws:**
- `ArgumentNullException` - If `changes` is `null`.

### `BreakingChangeClassification ClassifyChange(DiffResult change)`
Evaluates a single migration difference and classifies it according to predefined rules.

**Parameters:**
- `change` - A `DiffResult` object representing a single difference between migration snapshots.

**Returns:**
A `BreakingChangeClassification` record describing the change and its classification.

**Throws:**
- `ArgumentNullException` - If `change` is `null`.

### `BreakingChangeSummary ClassifyDiffResult(IEnumerable<DiffResult> changes)`
Processes a collection of migration differences and produces a summary of breaking changes, including counts by classification type.

**Parameters:**
- `changes` - An enumerable collection of `DiffResult` objects representing the differences between migration snapshots.

**Returns:**
A `BreakingChangeSummary` record containing aggregated results of the classification process.

**Throws:**
- `ArgumentNullException` - If `changes` is `null`.

### `sealed record BreakingChangeClassification`
Represents the classification of a single breaking change.

**Properties:**
- `Change` - The `DiffResult` object that was classified.
- `Classification` - A string describing the type of breaking change (e.g., "SchemaChange", "DataLoss").
- `Severity` - An enumeration value indicating the severity level of the change (e.g., `Warning`, `Error`).

### `sealed record BreakingChangeSummary`
Summarizes the results of a breaking change detection operation.

**Properties:**
- `TotalChanges` - The total number of differences analyzed.
- `BreakingChanges` - The number of changes classified as breaking.
- `Classifications` - A dictionary mapping classification types to their respective counts.
- `SeverityCounts` - A dictionary mapping severity levels to their respective counts.
