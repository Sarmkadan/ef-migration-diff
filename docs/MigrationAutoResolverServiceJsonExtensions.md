# MigrationAutoResolverServiceJsonExtensions

A static utility class providing `System.Text.Json` serialization and deserialization extensions for `MigrationAutoResolverService` instances. It offers methods to serialize a `MigrationAutoResolverService` to a JSON string and to deserialize a JSON string back to a `MigrationAutoResolverService` instance, enabling round-trip serialization of auto-resolver configuration.

The class uses a shared, cached `JsonSerializerOptions` instance configured with `JsonNamingPolicy.CamelCase`, `WriteIndented = false`, and `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`. When indented output is requested, a separate cached instance with `WriteIndented = true` is used, so the shared compact instance is never mutated.

## API

### `ToJson`
Serializes a `MigrationAutoResolverService` instance to JSON with camelCase property naming.
- **Parameters**:
  - `this MigrationAutoResolverService value` – the `MigrationAutoResolverService` instance to serialize.
  - `bool indented` – whether to format the JSON with indentation for readability (default: `false`).
- **Returns**: `string` – a JSON string representation of the `MigrationAutoResolverService` instance.
- **Exceptions**: Throws `ArgumentNullException` if `value` is `null`.

### `FromJson`
Deserializes a JSON string to a `MigrationAutoResolverService` instance.
- **Parameters**: `string json` – the JSON string to deserialize.
- **Returns**: `MigrationAutoResolverService?` – the deserialized `MigrationAutoResolverService` instance, or `null` if the JSON does not represent a valid object.
- **Exceptions**: Throws `ArgumentNullException` if `json` is `null`; throws `JsonException` if the JSON is invalid or cannot be deserialized.

### `TryFromJson`
Attempts to deserialize a JSON string to a `MigrationAutoResolverService` instance.
- **Parameters**:
  - `string json` – the JSON string to deserialize.
  - `out MigrationAutoResolverService? value` – the deserialized `MigrationAutoResolverService` instance, or `null` if deserialization fails.
- **Returns**: `bool` – `true` if deserialization succeeds; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `json` is `null`.

## Usage

```csharp
using EfMigrationDiff.Services;

// Serialize a MigrationAutoResolverService to JSON.
var service = new MigrationAutoResolverService(logger);
string json = service.ToJson();

// Serialize with indentation for readability.
string indented = service.ToJson(indented: true);

// Deserialize a JSON string back to a MigrationAutoResolverService instance.
MigrationAutoResolverService? restored = MigrationAutoResolverServiceJsonExtensions.FromJson(json);
// restored != null

// TryFromJson reports success/failure via the return value.
if (MigrationAutoResolverServiceJsonExtensions.TryFromJson(json, out MigrationAutoResolverService? result))
{
    // result != null
}
```

## Notes

- `ToJson` is an extension method and can be called directly on a `MigrationAutoResolverService` instance, while `FromJson` and `TryFromJson` are static methods invoked on the `MigrationAutoResolverServiceJsonExtensions` type.
- `FromJson` returns `null` for JSON that does not represent a valid object, but unlike `TryFromJson` it does not catch `JsonException` — invalid JSON propagates the exception to the caller.
- `TryFromJson` returns `false` for input that throws a `JsonException` during deserialization, so callers do not need to catch serialization errors themselves.
- All three methods throw `ArgumentNullException` when their required string/instance argument is `null`.
- The shared `JsonSerializerOptions` instances are immutable in practice; indented output is produced from a separate cached instance, so concurrent calls are safe.