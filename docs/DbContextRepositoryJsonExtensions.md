# DbContextRepositoryJsonExtensions

`Repositories/DbContextRepositoryJsonExtensions.cs` provides System.Text.Json
serialization and deserialization extensions for
[`DbContextRepository`](../Repositories/DbContextRepository.cs). It lets a
repository instance be round-tripped to and from JSON without adding any
serialization concerns to the repository class itself.

## Shared serializer options

All members share a single static `JsonSerializerOptions` instance built from
`JsonSerializerDefaults.Web`:

| Setting | Value |
| --- | --- |
| `PropertyNamingPolicy` | `JsonNamingPolicy.CamelCase` |
| `WriteIndented` | `false` (overridden per-call when indentation is requested) |
| `TypeInfoResolver` | `DefaultJsonTypeInfoResolver` |
| `ReferenceHandler` | `ReferenceHandler.IgnoreCycles` |

Because the options are shared and immutable, enum values are serialized as
camel-cased strings, and reference cycles are ignored during serialization.

## Extension methods

### `ToJson`

```csharp
public static string ToJson(this DbContextRepository value, bool indented = false)
```

Serializes a `DbContextRepository` instance to a JSON string.

- Throws `ArgumentNullException` if `value` is null.
- When `indented` is `true`, returns a copy of the shared options with
  `WriteIndented = true` so the output is formatted for readability; otherwise
  the compact shared options are used.

### `FromJson`

```csharp
public static DbContextRepository? FromJson(string json)
```

Deserializes a JSON string to a `DbContextRepository` instance.

- Throws `ArgumentException` if `json` is null or empty.
- Returns `null` if `json` is empty or whitespace.
- Throws `JsonException` if the JSON is invalid or cannot be deserialized.

### `TryFromJson`

```csharp
public static bool TryFromJson(string json, out DbContextRepository? value)
```

Attempts to deserialize a JSON string to a `DbContextRepository` instance
without throwing.

- Returns `false` (and sets `value` to `null`) if `json` is empty or
  whitespace, or if deserialization throws a `JsonException`.
- Returns `true` and sets `value` on success.

## Usage

```csharp
using EfMigrationDiff.Repositories;

var repo = new DbContextRepository();
repo.Add(new DbContextMetadata { /* ... */ });

// Serialize (compact by default, or indented)
string json = repo.ToJson();
string pretty = repo.ToJson(indented: true);

// Deserialize (throws on invalid input)
DbContextRepository? restored = DbContextRepositoryJsonExtensions.FromJson(json);

// Deserialize (safe, no exceptions)
if (DbContextRepositoryJsonExtensions.TryFromJson(json, out var parsed))
{
    // parsed is a DbContextRepository
}
```

## Notes

- `ToJson` is an extension method and can be called directly on a
  `DbContextRepository` instance, while `FromJson` and `TryFromJson` are static
  methods invoked on the `DbContextRepositoryJsonExtensions` type.
- The repository's in-memory state is serialized as-is; no locking is applied
  during serialization, so concurrent mutation of the repository while
  serializing is not synchronized.
- Reference cycles are ignored during serialization to prevent infinite loops
  when serializing object graphs that may contain circular references.