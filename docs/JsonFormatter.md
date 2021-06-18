# JsonFormatter

The `JsonFormatter` class provides a centralized utility for serializing and deserializing objects to and from JSON format, specifically designed to support file-based operations within the `ef-migration-diff` project. It encapsulates the logic for converting .NET types to JSON strings and persisting them to disk, while also handling the reverse process of reading files and reconstructing typed objects, ensuring consistent error handling through the dedicated `FormattingException`.

## API

### Constructors

#### `public JsonFormatter()`
Initializes a new instance of the `JsonFormatter` class. This constructor sets up the necessary internal configuration required for subsequent formatting and parsing operations.

### Properties

#### `public string Format`
Gets a string representation indicating the current data format handled by this instance (e.g., "JSON"). This property is read-only and serves as an identifier for the serialization strategy employed.

### Methods

#### `public T? Deserialize<T>(string json)`
Deserializes a JSON string into an object of the specified generic type `T`.
*   **Parameters**:
    *   `json`: The JSON string to deserialize.
*   **Returns**: An instance of `T` if successful, or `null` if the input represents a null value.
*   **Throws**: `FormattingException` if the JSON is malformed, does not match the structure of `T`, or if the deserialization process fails.

#### `public object? Deserialize(string json)`
Deserializes a JSON string into a non-generic `object`. The runtime type of the returned object is determined by the JSON content.
*   **Parameters**:
    *   `json`: The JSON string to deserialize.
*   **Returns**: An `object` representing the deserialized data, or `null` if the input represents a null value.
*   **Throws**: `FormattingException` if the JSON is malformed or cannot be parsed.

#### `public void WriteToFile<T>(string path, T data)`
Serializes the provided data object to JSON and writes the result to the specified file path. If the file exists, it is overwritten.
*   **Parameters**:
    *   `path`: The file system path where the JSON content will be written.
    *   `data`: The object of type `T` to serialize.
*   **Returns**: None.
*   **Throws**: `FormattingException` if the object cannot be serialized. May also throw standard I/O exceptions (e.g., `UnauthorizedAccessException`, `DirectoryNotFoundException`) if the file system operation fails.

#### `public T? ReadFromFile<T>(string path)`
Reads the content from the specified file, interprets it as JSON, and deserializes it into an object of type `T`.
*   **Parameters**:
    *   `path`: The file system path to read from.
*   **Returns**: An instance of `T` if successful, or `null` if the file content represents a null value.
*   **Throws**: `FormattingException` if the file content is not valid JSON or does not match type `T`. May also throw standard I/O exceptions if the file cannot be accessed or read.

### Exceptions

#### `public FormattingException(string message)`
Initializes a new instance of the `FormattingException` class with a specified error message. This exception is thrown when serialization or deserialization operations fail due to format issues.

#### `public FormattingException(string message, Exception innerException)`
Initializes a new instance of the `FormattingException` class with a specified error message and a reference to the inner exception that caused this exception. This preserves the stack trace of the underlying error (e.g., a specific parser error from the underlying JSON library).

## Usage

### Example 1: Writing and Reading a Configuration Object
This example demonstrates saving a migration configuration object to a file and retrieving it later.

```csharp
public class MigrationConfig
{
    public string SourceDatabase { get; set; }
    public string TargetDatabase { get; set; }
    public bool IncludeData { get; set; }
}

var formatter = new JsonFormatter();
var config = new MigrationConfig
{
    SourceDatabase = "Server=Local;Db=Source;",
    TargetDatabase = "Server=Local;Db=Target;",
    IncludeData = false
};

// Write the configuration to a file
formatter.WriteToFile("config.json", config);

// Read the configuration back from the file
var loadedConfig = formatter.ReadFromFile<MigrationConfig>("config.json");

if (loadedConfig != null)
{
    Console.WriteLine($"Target: {loadedConfig.TargetDatabase}");
}
```

### Example 2: Deserializing Raw JSON Strings
This example shows how to parse JSON strings directly without file I/O, handling potential formatting errors.

```csharp
var formatter = new JsonFormatter();
string jsonString = "{\"Id\": 101, \"Name\": \"InitialMigration\"}";

try
{
    // Deserialize to a specific type
    var migration = formatter.Deserialize<MigrationMetadata>(jsonString);
    
    // Deserialize to a generic object if type is unknown at compile time
    var dynamicData = formatter.Deserialize(jsonString);
    
    Console.WriteLine($"Format detected: {formatter.Format}");
}
catch (FormattingException ex)
{
    Console.Error.WriteLine($"Failed to parse JSON: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.Error.WriteLine($"Inner detail: {ex.InnerException.Message}");
    }
}
```

## Notes

*   **Null Handling**: Both `Deserialize<T>` and `ReadFromFile<T>` return `null` if the JSON content explicitly represents a null value. Callers should verify the return value before accessing members to avoid `NullReferenceException`.
*   **Exception Wrapping**: All format-related errors are wrapped in `FormattingException`. However, file system-level errors (such as missing directories or permission issues in `WriteToFile` and `ReadFromFile`) are not caught internally and will propagate as standard .NET IO exceptions.
*   **Thread Safety**: The `JsonFormatter` class exposes state via the `Format` property and performs stateless operations in its methods. While the methods themselves do not appear to maintain mutable internal state during execution, the underlying JSON serialization library used internally may have specific thread-safety constraints. It is recommended to instantiate separate instances per thread or synchronize access if the same instance is shared across concurrent threads performing write operations.
*   **File Overwriting**: The `WriteToFile` method overwrites existing files at the target path without warning. Ensure that critical data is backed up or that the path logic prevents accidental overwrites.
