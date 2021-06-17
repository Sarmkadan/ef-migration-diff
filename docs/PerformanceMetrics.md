# PerformanceMetrics

`PerformanceMetrics` is a utility class designed to measure and aggregate performance metrics for operations within an application. It tracks execution time, memory usage, and operation counts, providing methods to generate reports and retrieve collected metrics. This class is particularly useful for profiling and debugging performance-critical code paths.

## API

### `public PerformanceTimer StartOperation()`
Starts a new operation measurement and returns a `PerformanceTimer` instance. The timer is automatically stopped when the returned `PerformanceTimer` is disposed. This method increments the operation count and begins tracking the duration of the operation.

**Returns:**
- A `PerformanceTimer` instance that must be disposed to stop the measurement.

**Throws:**
- None.

---

### `public OperationMetrics? GetMetrics()`
Retrieves the aggregated metrics for the current operation, if any measurements have been recorded. If no measurements exist, this method returns `null`.

**Returns:**
- An `OperationMetrics` object containing aggregated metrics (e.g., `Count`, `TotalDuration`, `MinDuration`, `MaxDuration`, `MemoryDelta`), or `null` if no measurements are available.

**Throws:**
- None.

---

### `public Dictionary<string, OperationMetrics> GetAllMetrics()`
Returns a dictionary of all recorded operations and their aggregated metrics. The keys are operation names, and the values are `OperationMetrics` objects.

**Returns:**
- A `Dictionary<string, OperationMetrics>` where each entry represents an operation and its metrics.

**Throws:**
- None.

---

### `public string GenerateReport()`
Generates a human-readable report summarizing the performance metrics for all recorded operations. The report includes operation counts, total/average/min/max durations, and memory deltas.

**Returns:**
- A formatted string containing the performance report.

**Throws:**
- None.

---

### `public void Clear()`
Resets all collected metrics, clearing operation counts, durations, memory deltas, and measurements. This method is useful for reusing the `PerformanceMetrics` instance for new measurements.

**Parameters:**
- None.

**Returns:**
- Void.

**Throws:**
- None.

---

### `public string OperationName`
Gets the name of the operation being measured. This property is set when the `PerformanceMetrics` instance is created.

**Returns:**
- A string representing the operation name.

---

### `public int Count`
Gets the total number of times the operation has been measured.

**Returns:**
- An integer representing the operation count.

---

### `public TimeSpan TotalDuration`
Gets the cumulative duration of all recorded measurements for the operation.

**Returns:**
- A `TimeSpan` representing the total duration.

---

### `public TimeSpan MinDuration`
Gets the shortest duration recorded for the operation.

**Returns:**
- A `TimeSpan` representing the minimum duration.

---

### `public TimeSpan MaxDuration`
Gets the longest duration recorded for the operation.

**Returns:**
- A `TimeSpan` representing the maximum duration.

---

### `public long MemoryDelta`
Gets the net memory change (in bytes) observed during the operation measurements. This value may not be accurate in all environments due to garbage collection and other runtime factors.

**Returns:**
- A `long` representing the memory delta.

---

### `public List<TimeSpan> Measurements`
Gets a list of all individual duration measurements recorded for the operation.

**Returns:**
- A `List<TimeSpan>` containing each recorded duration.

---

### `public PerformanceTimer`
A nested disposable struct that represents an active operation measurement. The timer stops and records the duration when disposed.

**Methods:**
- `public void Dispose()`: Stops the timer and records the duration in the parent `PerformanceMetrics` instance.

---

## Usage

### Example 1: Measuring a Single Operation
