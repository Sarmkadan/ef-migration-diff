# ReportEngineJsonExtensions

A static utility class providing `System.Text.Json` serialization and deserialization extensions for `ReportEngine` instances. It offers methods to serialize a `ReportEngine` to a JSON string and to deserialize a JSON string back to a `ReportEngine` instance.

The class uses a shared, cached `JsonSerializerOptions` instance configured with `JsonSerializerDefaults.Web`, `JsonNamingPolicy.CamelCase`, `WriteIndented = false`, and `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`. When indented output is requested, a copy of the cached options is created with `WriteIndented = true` so the shared instance is never mutated.

## API

### `ToJson`
Serializes a `ReportEngine` instance to JSON with camelCase property naming.
- **Parameters**:
  - `this ReportEngine value` – the `ReportEngine` instance to serialize.
  - `bool indented` – whether to format the JSON with indentation for readability (default: `false`).
- **Returns**: `string` – a JSON string representation of the `ReportEngine` instance.
- **Exceptions**: Throws `ArgumentNullException` if `value` is `null`.

### `FromJson`
Deserializes a JSON string to a `ReportEngine` instance.
- **Parameters**: `string json` – the JSON string to deserialize.
- **Returns**: `ReportEngine?` – the deserialized `ReportEngine` instance, or `null` if the JSON is invalid or whitespace.
- **Exceptions**: Throws `ArgumentNullException` if `json` is `null`.

### `TryFromJson`
Attempts to deserialize a JSON string to a `ReportEngine` instance.
- **Parameters**:
  - `string json` – the JSON string to deserialize.
  - `out ReportEngine? value` – the deserialized `ReportEngine` instance, or `null` if deserialization fails.
- **Returns**: `bool` – `true` if deserialization succeeds; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `json` is `null`.

## Usage

```csharp
using EfMigrationDiff.Reports;

// Serialize a ReportEngine to JSON.
var engine = new ReportEngine();
string json = engine.ToJson();

// Serialize with indentation for readability.
string indented = engine.ToJson(indented: true);

// Deserialize a JSON string back to a ReportEngine instance.
ReportEngine? restored = ReportEngineJsonExtensions.FromJson(json);
// restored != null

// Deserialize invalid or whitespace JSON returns null.
ReportEngine? invalid = ReportEngineJsonExtensions.FromJson("not-json");
// invalid == null

// TryFromJson reports success/failure via the return value.
if (ReportEngineJsonExtensions.TryFromJson(json, out ReportEngine? result))
{
    // result != null
}
```

## Notes

- `ToJson` is an extension method and can be called directly on a `ReportEngine` instance, while `FromJson` and `TryFromJson` are static methods invoked on the `ReportEngineJsonExtensions` type.
- `FromJson` and `TryFromJson` return `null` / `false` for whitespace-only input and for input that throws a `JsonException` during deserialization, so callers do not need to catch serialization errors themselves.
- The shared `JsonSerializerOptions` instance is immutable in practice; indented output is produced from a per-call copy, so concurrent calls are safe.