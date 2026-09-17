# MigrationRepositoryJsonExtensions

`Repositories/MigrationRepositoryJsonExtensions.cs` provides System.Text.Json
serialization and deserialization extensions for
[`MigrationRepository`](../Repositories/MigrationRepository.cs). It lets a
repository instance be round-tripped to and from JSON without adding any
serialization concerns to the repository class itself.

## Shared serializer options

All members share a single static `JsonSerializerOptions` instance built from
`JsonSerializerDefaults.Web`:

| Setting | Value |
| --- | --- |
| `PropertyNamingPolicy` | `JsonNamingPolicy.CamelCase` |
| `WriteIndented` | `false` (overridden per-call when indentation is requested) |
| `PropertyNameCaseInsensitive` | `true` |
| `DefaultIgnoreCondition` | `JsonIgnoreCondition.WhenWritingNull` |
| `TypeInfoResolver` | `DefaultJsonTypeInfoResolver` |
| `Converters` | `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)` |

Because the options are shared and immutable, enum values are serialized as
camel-cased strings (e.g. `MigrationStatus` values), and null properties are
omitted from the output.

## Extension methods

### `ToJson`

```csharp
public static string ToJson(this MigrationRepository value, bool indented = false)
```

Serializes a `MigrationRepository` instance to a JSON string.

- Throws `ArgumentNullException` if `value` is null.
- When `indented` is `true`, returns a copy of the shared options with
  `WriteIndented = true` so the output is formatted for readability; otherwise
  the compact shared options are used.

### `FromJson`

```csharp
public static MigrationRepository? FromJson(string json)
```

Deserializes a JSON string to a `MigrationRepository` instance.

- Throws `ArgumentException` if `json` is null or empty.
- Returns `null` if `json` is empty or whitespace.
- Throws `JsonException` if the JSON is invalid or cannot be deserialized.

### `TryFromJson`

```csharp
public static bool TryFromJson(string json, out MigrationRepository? value)
```

Attempts to deserialize a JSON string to a `MigrationRepository` instance
without throwing.

- Returns `false` (and sets `value` to `null`) if `json` is empty or
  whitespace, or if deserialization throws a `JsonException`.
- Returns `true` and sets `value` on success.

## Usage

```csharp
using EfMigrationDiff.Repositories;

var repo = new MigrationRepository();
repo.Add(new Migration { Id = "m1", Name = "InitialCreate" });

// Serialize (compact by default, or indented)
string json = repo.ToJson();
string pretty = repo.ToJson(indented: true);

// Deserialize (throws on invalid input)
MigrationRepository? restored = MigrationRepositoryJsonExtensions.FromJson(json);

// Deserialize (safe, no exceptions)
if (MigrationRepositoryJsonExtensions.TryFromJson(json, out var parsed))
{
    // parsed is a MigrationRepository
}
```

## Notes

- `FromJson` and `TryFromJson` are static methods rather than extension
  methods, so they are invoked on the class name (`MigrationRepositoryJsonExtensions`)
  rather than on a string instance.
- The repository's in-memory state is serialized as-is; no locking is applied
  during serialization, so concurrent mutation of the repository while
  serializing is not synchronized.