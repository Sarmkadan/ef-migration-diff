# PerformanceMetricsJsonExtensions

A static utility class providing `System.Text.Json` serialization and deserialization extensions for `PerformanceMetrics`. It offers methods to serialize a `PerformanceMetrics` instance to JSON and to deserialize a JSON string back into a `PerformanceMetrics` instance.

The class uses a shared, cached `JsonSerializerOptions` instance configured with `JsonNamingPolicy.CamelCase`, `WriteIndented = false`, `ReferenceHandler.IgnoreCycles`, and `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`. When indented output is requested, a copy of the cached options with `WriteIndented = true` is used.

## API

### `ToJson`
Serializes a `PerformanceMetrics` instance to JSON with camelCase property naming.
- **Parameters**:
  - `this PerformanceMetrics value` – the performance metrics to serialize.
  - `bool indented` – whether to format the JSON with indentation for readability (default: `false`).
- **Returns**: `string` – a JSON string representation of the performance metrics.
- **Exceptions**: Throws `ArgumentNullException` if `value` is `null`.

### `FromJson`
Deserializes a JSON string to a `PerformanceMetrics` instance.
- **Parameters**: `string json` – the JSON string to deserialize.
- **Returns**: `PerformanceMetrics?` – the deserialized `PerformanceMetrics` instance, or `null` if the JSON is invalid.
- **Exceptions**: Throws `ArgumentException` if `json` is `null` or empty.

### `TryFromJson`
Attempts to deserialize a JSON string to a `PerformanceMetrics` instance.
- **Parameters**:
  - `string json` – the JSON string to deserialize.
  - `out PerformanceMetrics? value` – the deserialized `PerformanceMetrics` instance, or `null` if deserialization fails.
- **Returns**: `bool` – `true` if deserialization succeeds; otherwise, `false`.
- **Exceptions**: Throws `ArgumentException` if `json` is `null` or empty.

## Usage

```csharp
using EfMigrationDiff.Utilities;

// Record some metrics to serialize.
var metrics = new PerformanceMetrics();
using (metrics.StartOperation("parse"))
{
    // ... work ...
}

// Serialize a PerformanceMetrics instance to JSON.
string json = metrics.ToJson();

// Serialize with indentation for readability.
string indented = metrics.ToJson(indented: true);

// Deserialize a JSON string back into a PerformanceMetrics instance.
PerformanceMetrics? restored = PerformanceMetricsJsonExtensions.FromJson(json);

// Deserialize invalid JSON returns null.
PerformanceMetrics? invalid = PerformanceMetricsJsonExtensions.FromJson("not-json");
// invalid == null

// TryFromJson reports success/failure via the return value.
if (PerformanceMetricsJsonExtensions.TryFromJson(json, out PerformanceMetrics? result))
{
    // result is a valid PerformanceMetrics instance.
}
```