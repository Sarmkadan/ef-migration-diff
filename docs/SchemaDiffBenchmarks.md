# SchemaDiffBenchmarks

`SchemaDiffBenchmarks` is a performance benchmarking suite for the `ef-migration-diff` library. It measures the execution time and memory characteristics of schema comparison, three-way merge, and merge resolution operations across varying input sizes and configurations. The class is designed for use with BenchmarkDotNet and exposes a set of benchmark methods that exercise the core diff engine under controlled, repeatable conditions.

## API

### Instance Fields

#### `Config`
A BenchmarkDotNet configuration object that controls the execution environment for all benchmarks in this class. It specifies parameters such as run strategy, iteration counts, warmup phases, and output formatting.

### Methods

#### `Setup`
```csharp
public void Setup()
```
Prepares the benchmark environment before each iteration. It initializes the schema inputs, loads any required metadata, and ensures that the diff engine is in a clean state. Called automatically by the benchmarking infrastructure; not intended for direct invocation.

#### `ComputeDiff_Small`
```csharp
public SchemaDiffResult ComputeDiff_Small()
```
Computes a two-way schema diff between two small schemas. Returns a `SchemaDiffResult` containing the detected additions, removals, and modifications. The input schemas are pre-loaded during `Setup`.

#### `ComputeDiff_Medium`
```csharp
public SchemaDiffResult ComputeDiff_Medium()
```
Computes a two-way schema diff between two medium-sized schemas. Returns a `SchemaDiffResult` with the full set of differences. Useful for measuring performance scaling as schema size increases.

#### `ComputeDiff_Large`
```csharp
public SchemaDiffResult ComputeDiff_Large()
```
Computes a two-way schema diff between two large schemas. Returns a `SchemaDiffResult`. This benchmark stresses the diff engine with a high number of schema elements.

#### `ComputeDiff_WithSqlContent`
```csharp
public SchemaDiffResult ComputeDiff_WithSqlContent()
```
Computes a two-way schema diff where the schemas include SQL body content (stored procedures, functions, triggers). Returns a `SchemaDiffResult` that includes textual differences within SQL objects. Measures the overhead of content-level comparison.

#### `ComputeDiff_WithWhitespaceIgnored`
```csharp
public SchemaDiffResult ComputeDiff_WithWhitespaceIgnored()
```
Computes a two-way schema diff with whitespace normalization enabled. Returns a `SchemaDiffResult` where formatting-only changes are suppressed. Evaluates the cost of whitespace-insensitive comparison.

#### `ComputeDiff_WithMetadata`
```csharp
public SchemaDiffResult ComputeDiff_WithMetadata()
```
Computes a two-way schema diff that includes extended metadata comparison (e.g., descriptions, annotations). Returns a `SchemaDiffResult` enriched with metadata-level differences. Benchmarks the additional processing overhead.

#### `ComputeThreeWayDiff_Small`
```csharp
public ThreeWayDiffResult ComputeThreeWayDiff_Small()
```
Performs a three-way merge analysis on small schemas with a common ancestor. Returns a `ThreeWayDiffResult` that identifies conflicts and compatible changes between the source and target branches relative to the base.

#### `ComputeThreeWayDiff_Medium`
```csharp
public ThreeWayDiffResult ComputeThreeWayDiff_Medium()
```
Performs a three-way merge analysis on medium-sized schemas. Returns a `ThreeWayDiffResult`. Measures merge analysis performance at an intermediate scale.

#### `ComputeThreeWayDiff_Large`
```csharp
public ThreeWayDiffResult ComputeThreeWayDiff_Large()
```
Performs a three-way merge analysis on large schemas. Returns a `ThreeWayDiffResult`. Stress-tests the merge engine with a high volume of divergent changes.

#### `ComputeThreeWayDiff_WithConflicts`
```csharp
public ThreeWayDiffResult ComputeThreeWayDiff_WithConflicts()
```
Performs a three-way merge analysis on schemas deliberately constructed to produce merge conflicts. Returns a `ThreeWayDiffResult` containing conflicting change entries. Benchmarks conflict detection overhead.

#### `AcceptSourceStrategy`
```csharp
public MergeResolutionPlan AcceptSourceStrategy()
```
Generates a merge resolution plan that unconditionally accepts the source branch's changes for all conflicts. Returns a `MergeResolutionPlan` where every conflict is resolved in favor of the source side.

#### `AcceptTargetStrategy`
```csharp
public MergeResolutionPlan AcceptTargetStrategy()
```
Generates a merge resolution plan that unconditionally accepts the target branch's changes for all conflicts. Returns a `MergeResolutionPlan` where every conflict is resolved in favor of the target side.

#### `AutoMergeStrategy`
```csharp
public MergeResolutionPlan AutoMergeStrategy()
```
Generates a merge resolution plan using an automated heuristic that attempts to merge non-overlapping changes and flags true conflicts. Returns a `MergeResolutionPlan` with the auto-merge decisions applied.

#### `ApplyMergeResolution`
```csharp
public SchemaMergeResult ApplyMergeResolution()
```
Applies a pre-computed merge resolution plan to produce a merged schema. Returns a `SchemaMergeResult` containing the final merged schema and any unresolved items. Benchmarks the cost of materializing the merge output.

#### `ValidateResolution`
```csharp
public IReadOnlyList<string> ValidateResolution()
```
Validates the integrity of a resolved merge result by checking for consistency violations, orphaned references, or semantic errors. Returns an `IReadOnlyList<string>` of validation messages; an empty list indicates a valid result.

#### `DefaultOptions`
```csharp
public SchemaDiffResult DefaultOptions()
```
Computes a two-way schema diff using the library's default comparison options. Returns a `SchemaDiffResult`. Serves as a baseline benchmark for option-free diff performance.

#### `WithSqlContent`
```csharp
public SchemaDiffResult WithSqlContent()
```
Computes a two-way schema diff with SQL content comparison explicitly enabled. Returns a `SchemaDiffResult`. Isolates the performance impact of content-level diffing from other option combinations.

## Usage

### Example 1: Running the full benchmark suite with BenchmarkDotNet

```csharp
using BenchmarkDotNet.Running;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<SchemaDiffBenchmarks>();
        Console.WriteLine($"Total benchmarks executed: {summary.BenchmarksCases.Length}");
    }
}
```

### Example 2: Invoking individual benchmarks for ad-hoc profiling

```csharp
var benchmarks = new SchemaDiffBenchmarks();
benchmarks.Setup();

var smallDiff = benchmarks.ComputeDiff_Small();
Console.WriteLine($"Small diff: {smallDiff.Added.Count} added, {smallDiff.Removed.Count} removed");

var threeWay = benchmarks.ComputeThreeWayDiff_WithConflicts();
Console.WriteLine($"Three-way conflicts: {threeWay.Conflicts.Count}");

var plan = benchmarks.AcceptSourceStrategy();
var merged = benchmarks.ApplyMergeResolution();
var issues = benchmarks.ValidateResolution();
Console.WriteLine($"Validation issues: {issues.Count}");
```

## Notes

- **Setup dependency**: All benchmark methods assume `Setup` has been called beforehand. When running under BenchmarkDotNet, this is handled automatically per iteration. In manual scenarios, call `Setup` explicitly before any diff or merge method.
- **Input immutability**: The schema inputs loaded during `Setup` are not modified by any benchmark method. Each call operates on the same initial state, ensuring repeatable measurements.
- **Return value allocation**: Methods returning `SchemaDiffResult`, `ThreeWayDiffResult`, `MergeResolutionPlan`, or `SchemaMergeResult` allocate new objects on each invocation. Benchmark memory diagnostics will reflect these allocations.
- **Thread safety**: This class is not designed for concurrent use. All benchmark methods access instance state set during `Setup` and are intended to run sequentially within a single benchmark iteration. Parallel invocation from multiple threads may produce undefined behavior.
- **Validation output**: `ValidateResolution` returns an empty list for a clean merge. Non-empty results indicate structural or semantic problems in the merged schema and should be investigated before accepting the merge output.
- **Strategy independence**: `AcceptSourceStrategy`, `AcceptTargetStrategy`, and `AutoMergeStrategy` produce plans based on the current conflict state. They do not mutate shared state and can be called independently to compare resolution approaches.
