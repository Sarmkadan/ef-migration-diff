# MergeAttemptJsonExtensions

`Models/MergeAttemptJsonExtensions.cs` provides System.Text.Json
serialization and deserialization extensions for
[`MergeAttempt`](MergeAttempt.md). It lets a merge attempt be round-tripped
to and from JSON without adding any serialization concerns to the model class
itself.

## Shared serializer options

All members share a single static `JsonSerializerOptions` instance:

| Setting | Value |
| --- | --- |
| `PropertyNamingPolicy` | `JsonNamingPolicy.CamelCase` |
| `WriteIndented` | `false` (overridden per-call when indentation is requested) |
| `DefaultIgnoreCondition` | `JsonIgnoreCondition.WhenWritingNull` |

Because the options are shared and immutable, property names are emitted in
camelCase and null properties are omitted from the output. Enum values
(`ConflictType`, `MergeStrategy`) are serialized as their numeric values (no
`JsonStringEnumConverter` is registered).

## Members

### `ToJson`

```csharp
public static string ToJson(this MergeAttempt value, bool indented = false)
```

Serializes a `MergeAttempt` instance to a JSON string.

- Throws `ArgumentNullException` if `value` is null.
- When `indented` is `true`, returns a copy of the shared options with
  `WriteIndented = true` so the output is formatted for readability; otherwise
  the compact shared options are used.

### `FromJson`

```csharp
public static MergeAttempt? FromJson(string json)
```

Deserializes a JSON string to a `MergeAttempt` instance.

- Throws `ArgumentNullException` if `json` is null.
- Returns `null` if the JSON is invalid or cannot be deserialized (a
  `JsonException` is caught and swallowed).

### `TryFromJson`

```csharp
public static bool TryFromJson(string json, out MergeAttempt? value)
```

Attempts to deserialize a JSON string to a `MergeAttempt` instance.

- Throws `ArgumentNullException` if `json` is null.
- Sets `value` to the deserialized attempt and returns `true` on success.
- Sets `value` to `null` and returns `false` if the JSON is invalid (a
  `JsonException` is caught and swallowed).

## Usage

### Example 1: Serializing an attempt

```csharp
var attempt = new MergeAttempt
{
    ConflictId = "conflict-42",
    ConflictType = ConflictType.ColumnConflict,
    StrategyApplied = MergeStrategy.LastWins,
    Succeeded = true,
    MergedContent = "ALTER TABLE dbo.Blog ADD CreatedDate datetime2 NOT NULL;"
};

string json = attempt.ToJson();                 // compact camelCase
string pretty = attempt.ToJson(indented: true); // indented for readability
```

### Example 2: Deserializing an attempt

```csharp
string json = attempt.ToJson();

MergeAttempt? restored = MergeAttemptJsonExtensions.FromJson(json);
if (restored is not null)
{
    Console.WriteLine(restored.ConflictId);
}
```

### Example 3: Try-pattern deserialization

```csharp
if (MergeAttemptJsonExtensions.TryFromJson(json, out var value))
{
    // value is a valid MergeAttempt
}
else
{
    // json was invalid and could not be deserialized
}
```