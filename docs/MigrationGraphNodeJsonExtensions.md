# MigrationGraphNodeJsonExtensions

`Models/MigrationGraphNodeJsonExtensions.cs` provides System.Text.Json
serialization and deserialization extensions for
[`MigrationGraphNode`](MigrationGraphNode.md). It lets a migration graph node
be round-tripped to and from JSON without adding any serialization concerns to
the node class itself.

## Shared serializer options

All members share a single static `JsonSerializerOptions` instance:

| Setting | Value |
| --- | --- |
| `PropertyNamingPolicy` | `JsonNamingPolicy.CamelCase` |
| `WriteIndented` | `false` (overridden per-call when indentation is requested) |
| `DefaultIgnoreCondition` | `JsonIgnoreCondition.WhenWritingNull` |

Because the options are shared and immutable, property names are emitted in
camelCase and null properties are omitted from the output. Enum values are
serialized as their numeric values (no `JsonStringEnumConverter` is registered).

## Extension methods

### `ToJson`

```csharp
public static string ToJson(this MigrationGraphNode? value, bool indented = false)
```

Serializes a `MigrationGraphNode` instance to a JSON string.

- Returns `"{}"` if `value` is null.
- When `indented` is `true`, returns a copy of the shared options with
  `WriteIndented = true` so the output is formatted for readability; otherwise
  the compact shared options are used.

### `FromJson`

```csharp
public static MigrationGraphNode? FromJson(string? json)
```

Deserializes a JSON string to a `MigrationGraphNode` instance.

- Throws `ArgumentException` if `json` is null or whitespace.
- Returns `null` if the JSON is invalid or cannot be deserialized (a
  `JsonException` is caught and swallowed).

### `TryFromJson`

```csharp
public static bool TryFromJson(string? json, out MigrationGraphNode? value)
```

Attempts to deserialize a JSON string to a `MigrationGraphNode` instance.

- Sets `value` to `null` and returns `false` if `json` is null or whitespace.
- Sets `value` to the deserialized node and returns `true` on success.
- Sets `value` to `null` and returns `false` if the JSON is invalid (a
  `JsonException` is caught and swallowed).

## Usage

### Example 1: Serializing a node

```csharp
var node = new MigrationGraphNode
{
    MigrationId = "202310011200_AddBlogCreatedDate",
    Name = "AddBlogCreatedDate",
    DbContextName = "BlogContext",
    Sequence = 1,
    Status = MigrationStatus.Pending
};

string json = node.ToJson();            // compact camelCase
string pretty = node.ToJson(indented: true); // indented for readability
```

### Example 2: Deserializing a node

```csharp
string json = node.ToJson();

MigrationGraphNode? restored = MigrationGraphNodeJsonExtensions.FromJson(json);
if (restored is not null)
{
    Console.WriteLine(restored.MigrationId);
}
```

### Example 3: Try-pattern deserialization

```csharp
if (MigrationGraphNodeJsonExtensions.TryFromJson(json, out var value))
{
    // value is a valid MigrationGraphNode
}
else
{
    // json was null, whitespace, or invalid
}
```