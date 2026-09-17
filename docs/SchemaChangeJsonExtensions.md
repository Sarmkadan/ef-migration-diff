# SchemaChangeJsonExtensions

`Models/SchemaChangeJsonExtensions.cs` provides System.Text.Json
serialization and deserialization extensions for
[`SchemaChange`](SchemaChange.md). It lets a schema change be round-tripped
to and from JSON without adding any serialization concerns to the model class
itself.

## Shared serializer options

All members share a single static `JsonSerializerOptions` instance:

| Setting | Value |
| --- | --- |
| `PropertyNamingPolicy` | `JsonNamingPolicy.CamelCase` |
| `WriteIndented` | `false` (overridden per-call when indentation is requested) |
| `DefaultIgnoreCondition` | `JsonIgnoreCondition.WhenWritingNull` |
| `Converters` | `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)` |

Because the options are shared and immutable, property names are emitted in
camelCase, null properties are omitted from the output, and enum values (such
as `SqlChangeType`) are serialized as their camelCase string names rather than
numeric values.

## Extension methods

### `ToJson`

```csharp
public static string ToJson(this SchemaChange value, bool indented = false)
```

Serializes a `SchemaChange` instance to a JSON string.

- Throws `ArgumentNullException` if `value` is null.
- When `indented` is `true`, returns a copy of the shared options with
  `WriteIndented = true` so the output is formatted for readability; otherwise
  the compact shared options are used.

### `FromJson`

```csharp
public static SchemaChange? FromJson(string json)
```

Deserializes a JSON string to a `SchemaChange` instance.

- Throws `ArgumentNullException` if `json` is null.
- Returns `null` if `json` is null or whitespace.
- Returns `null` if the JSON is invalid or cannot be deserialized (a
  `JsonException` is caught and swallowed).

### `TryFromJson`

```csharp
public static bool TryFromJson(string json, out SchemaChange? value)
```

Attempts to deserialize a JSON string to a `SchemaChange` instance.

- Throws `ArgumentNullException` if `json` is null.
- Sets `value` to `null` and returns `false` if `json` is null or whitespace.
- Sets `value` to the deserialized change and returns `true` on success.
- Sets `value` to `null` and returns `false` if the JSON is invalid (a
  `JsonException` is caught and swallowed).

## Usage

### Example 1: Serializing a change

```csharp
var change = new SchemaChange("202310011200_AddBlogCreatedDate", SqlChangeType.AddColumn, "ALTER TABLE dbo.Blogs ADD CreatedDate datetime2 NOT NULL")
{
    TableName = "Blogs",
    ColumnName = "CreatedDate"
};

string json = change.ToJson();              // compact camelCase
string pretty = change.ToJson(indented: true); // indented for readability
```

### Example 2: Deserializing a change

```csharp
string json = change.ToJson();

SchemaChange? restored = SchemaChangeJsonExtensions.FromJson(json);
if (restored is not null)
{
    Console.WriteLine(restored.GetDescription());
}
```

### Example 3: Try-pattern deserialization

```csharp
if (SchemaChangeJsonExtensions.TryFromJson(json, out var value))
{
    // value is a valid SchemaChange
}
else
{
    // json was null, whitespace, or invalid
}
```