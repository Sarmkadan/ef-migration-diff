# CsvFormatterJsonExtensions

A static utility class providing `System.Text.Json` serialization and deserialization extensions for `CsvFormatter` instances. It offers methods to serialize a `CsvFormatter` to a JSON string and to deserialize a JSON string back to a `CsvFormatter` instance, enabling round-trip serialization of CSV formatter configurations.

The class uses a shared, cached `JsonSerializerOptions` instance configured with `JsonNamingPolicy.CamelCase`, `WriteIndented = false`, and `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`. When indented output is requested, a copy of the cached options is created with `WriteIndented = true` so the shared instance is never mutated.

## API

### `ToJson`
Serializes a `CsvFormatter` instance to JSON with camelCase property naming.
- **Parameters**:
  - `this CsvFormatter value` – the `CsvFormatter` instance to serialize.
  - `bool indented` – whether to format the JSON with indentation for readability (default: `false`).
- **Returns**: `string` – a JSON string representation of the `CsvFormatter` instance.
- **Exceptions**: Throws `ArgumentNullException` if `value` is `null`.

### `FromJson`
Deserializes a JSON string to a `CsvFormatter` instance.
- **Parameters**: `string json` – the JSON string to deserialize.
- **Returns**: `CsvFormatter?` – the deserialized `CsvFormatter` instance, or `null` if the JSON represents a null value.
- **Exceptions**: Throws `ArgumentException` if `json` is `null` or empty; throws `JsonException` if the JSON is invalid or cannot be deserialized.

### `TryFromJson`
Attempts to deserialize a JSON string to a `CsvFormatter` instance.
- **Parameters**:
  - `string json` – the JSON string to deserialize.
  - `out CsvFormatter? value` – the deserialized `CsvFormatter` instance, or `null` if the JSON represents a null value or deserialization fails.
- **Returns**: `bool` – `true` if deserialization succeeds; otherwise, `false`.
- **Exceptions**: Throws `ArgumentException` if `json` is `null` or empty.

## Usage

```csharp
using EfMigrationDiff.Formatters;

// Serialize a CsvFormatter to JSON.
var formatter = new CsvFormatter(delimiter: ";", includeHeaders: false);
string json = formatter.ToJson();

// Serialize with indentation for readability.
string indented = formatter.ToJson(indented: true);

// Deserialize a JSON string back to a CsvFormatter instance.
CsvFormatter? restored = CsvFormatterJsonExtensions.FromJson(json);
// restored != null

// TryFromJson reports success/failure via the return value.
if (CsvFormatterJsonExtensions.TryFromJson(json, out CsvFormatter? result))
{
    // result != null
}
```

## Notes

- `ToJson` is an extension method and can be called directly on a `CsvFormatter` instance, while `FromJson` and `TryFromJson` are static methods invoked on the `CsvFormatterJsonExtensions` type.
- `FromJson` returns `null` for JSON that represents a null value, but unlike `TryFromJson` it does not catch `JsonException` — invalid JSON propagates the exception to the caller.
- `TryFromJson` returns `false` for input that throws a `JsonException` during deserialization, so callers do not need to catch serialization errors themselves.
- Both `FromJson` and `TryFromJson` throw `ArgumentException` when `json` is `null` or empty.
- The shared `JsonSerializerOptions` instance is immutable in practice; indented output is produced from a per-call copy, so concurrent calls are safe.