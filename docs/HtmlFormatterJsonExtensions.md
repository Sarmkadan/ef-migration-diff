# HtmlFormatterJsonExtensions

A static utility class providing `System.Text.Json` serialization and deserialization extensions for `HtmlFormatter` instances. It offers methods to serialize an `HtmlFormatter` to a JSON string and to deserialize a JSON string back to an `HtmlFormatter` instance.

The class uses a shared, cached `JsonSerializerOptions` instance configured with `JsonNamingPolicy.CamelCase`, `WriteIndented = false`, and `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`. When indented output is requested, a copy of the cached options is created with `WriteIndented = true` so the shared instance is never mutated.

## API

### `ToJson`
Serializes an `HtmlFormatter` instance to JSON with camelCase property naming.
- **Parameters**:
  - `this HtmlFormatter value` – the `HtmlFormatter` instance to serialize.
  - `bool indented` – whether to format the JSON with indentation for readability (default: `false`).
- **Returns**: `string` – a JSON string representation of the `HtmlFormatter` instance.
- **Exceptions**: Throws `ArgumentNullException` if `value` is `null`.

### `FromJson`
Deserializes a JSON string to an `HtmlFormatter` instance.
- **Parameters**: `string json` – the JSON string to deserialize.
- **Returns**: `HtmlFormatter?` – the deserialized `HtmlFormatter` instance, or `null` if the JSON is null, empty, or whitespace.
- **Exceptions**: Throws `ArgumentNullException` if `json` is `null`; throws `JsonException` if the JSON is invalid or cannot be deserialized.

### `TryFromJson`
Attempts to deserialize a JSON string to an `HtmlFormatter` instance.
- **Parameters**:
  - `string json` – the JSON string to deserialize.
  - `out HtmlFormatter? value` – the deserialized `HtmlFormatter` instance, or `null` if deserialization fails.
- **Returns**: `bool` – `true` if deserialization succeeds; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `json` is `null`.

## Usage

```csharp
using EfMigrationDiff.Formatters;

// Serialize an HtmlFormatter to JSON.
var formatter = new HtmlFormatter();
string json = formatter.ToJson();

// Serialize with indentation for readability.
string indented = formatter.ToJson(indented: true);

// Deserialize a JSON string back to an HtmlFormatter instance.
HtmlFormatter? restored = HtmlFormatterJsonExtensions.FromJson(json);
// restored != null

// Deserialize whitespace JSON returns null.
HtmlFormatter? empty = HtmlFormatterJsonExtensions.FromJson("   ");
// empty == null

// TryFromJson reports success/failure via the return value.
if (HtmlFormatterJsonExtensions.TryFromJson(json, out HtmlFormatter? result))
{
    // result != null
}
```

## Notes

- `ToJson` is an extension method and can be called directly on an `HtmlFormatter` instance, while `FromJson` and `TryFromJson` are static methods invoked on the `HtmlFormatterJsonExtensions` type.
- `FromJson` returns `null` for null, empty, or whitespace-only input, but unlike `TryFromJson` it does not catch `JsonException` — invalid JSON propagates the exception to the caller.
- `TryFromJson` returns `false` for whitespace-only input and for input that throws a `JsonException` during deserialization, so callers do not need to catch serialization errors themselves.
- The shared `JsonSerializerOptions` instance is immutable in practice; indented output is produced from a per-call copy, so concurrent calls are safe.