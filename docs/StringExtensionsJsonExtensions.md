# StringExtensionsJsonExtensions

A static utility class providing `System.Text.Json` serialization extensions for string values, using a camelCase property naming policy. It offers methods to serialize a string to JSON and to deserialize a JSON string back to a string value.

The class is `sealed` to prevent inheritance and enable potential runtime optimizations. It uses a shared, cached `JsonSerializerOptions` instance configured with `JsonNamingPolicy.CamelCase`, `WriteIndented = false`, and `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`.

## API

### `ToJson`
Serializes a string value to JSON with camelCase property naming.
- **Parameters**:
  - `this string value` – the string value to serialize.
  - `bool indented` – whether to format the JSON with indentation for readability (default: `false`).
- **Returns**: `string` – a JSON string representation of the string value.
- **Exceptions**: Throws `ArgumentNullException` if `value` is `null`.

### `FromJson`
Deserializes a JSON string to a string value.
- **Parameters**: `string json` – the JSON string to deserialize.
- **Returns**: `string?` – the deserialized string value, or `null` if the JSON is invalid or whitespace.
- **Exceptions**: Throws `ArgumentNullException` if `json` is `null`.

### `TryFromJson`
Attempts to deserialize a JSON string to a string value.
- **Parameters**:
  - `string json` – the JSON string to deserialize.
  - `out string? value` – the deserialized string value, or `null` if deserialization fails.
- **Returns**: `bool` – `true` if deserialization succeeds; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `json` is `null`.

## Usage

```csharp
using EfMigrationDiff.Extensions;

// Serialize a string to JSON.
string json = "hello".ToJson();
// json == "\"hello\""

// Serialize with indentation for readability.
string indented = "hello".ToJson(indented: true);

// Deserialize a JSON string back to a string value.
string? value = StringExtensionsJsonExtensions.FromJson("\"hello\"");
// value == "hello"

// Deserialize invalid or whitespace JSON returns null.
string? invalid = StringExtensionsJsonExtensions.FromJson("not-json");
// invalid == null

// TryFromJson reports success/failure via the return value.
if (StringExtensionsJsonExtensions.TryFromJson("\"hello\"", out string? result))
{
    // result == "hello"
}
```