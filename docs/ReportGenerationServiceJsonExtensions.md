# ReportGenerationServiceJsonExtensions

A static utility class providing `System.Text.Json` serialization and deserialization extensions for `ReportGenerationService` instances. It offers methods to serialize a `ReportGenerationService` to a JSON string and to deserialize a JSON string back to a `ReportGenerationService` instance.

The class uses a shared, cached `JsonSerializerOptions` instance configured with `JsonSerializerDefaults.Web`, `JsonNamingPolicy.CamelCase`, `WriteIndented = false`, `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`, and a `JsonStringEnumConverter` using camelCase. When indented output is requested, a copy of the cached options is created with `WriteIndented = true` so the shared instance is never mutated.

## API

### `ToJson`
Serializes a `ReportGenerationService` instance to JSON with camelCase property naming.
- **Parameters**:
  - `this ReportGenerationService value` – the `ReportGenerationService` instance to serialize.
  - `bool indented` – whether to format the JSON with indentation for readability (default: `false`).
- **Returns**: `string` – a JSON string representation of the `ReportGenerationService` instance.
- **Exceptions**: Throws `ArgumentNullException` if `value` is `null`.

### `FromJson`
Deserializes a JSON string to a `ReportGenerationService` instance.
- **Parameters**: `string json` – the JSON string to deserialize.
- **Returns**: `ReportGenerationService?` – the deserialized `ReportGenerationService` instance, or `null` if the JSON is empty or whitespace.
- **Exceptions**: Throws `JsonException` if the JSON is invalid or cannot be deserialized.

### `TryFromJson`
Attempts to deserialize a JSON string to a `ReportGenerationService` instance.
- **Parameters**:
  - `string json` – the JSON string to deserialize.
  - `out ReportGenerationService? value` – the deserialized `ReportGenerationService` instance, or `null` if deserialization fails.
- **Returns**: `bool` – `true` if deserialization succeeds; otherwise, `false`.

## Usage

```csharp
using EfMigrationDiff.Services;

// Serialize a ReportGenerationService to JSON.
var service = new ReportGenerationService();
string json = service.ToJson();

// Serialize with indentation for readability.
string indented = service.ToJson(indented: true);

// Deserialize a JSON string back to a ReportGenerationService instance.
ReportGenerationService? restored = ReportGenerationServiceJsonExtensions.FromJson(json);
// restored != null

// Deserialize empty or whitespace JSON returns null.
ReportGenerationService? empty = ReportGenerationServiceJsonExtensions.FromJson("  ");
// empty == null

// TryFromJson reports success/failure via the return value.
if (ReportGenerationServiceJsonExtensions.TryFromJson(json, out ReportGenerationService? result))
{
    // result != null
}
```

## Notes

- `ToJson` is an extension method and can be called directly on a `ReportGenerationService` instance, while `FromJson` and `TryFromJson` are static methods invoked on the `ReportGenerationServiceJsonExtensions` type.
- `FromJson` returns `null` for empty or whitespace input, and `TryFromJson` returns `false` for empty/whitespace input and for input that throws a `JsonException` during deserialization, so callers do not need to catch serialization errors themselves.
- The shared `JsonSerializerOptions` instance is immutable in practice; indented output is produced from a per-call copy, so concurrent calls are safe.