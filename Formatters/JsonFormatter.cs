#nullable enable
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfMigrationDiff.Formatters;

/// <summary>
/// Custom JSON converter that ensures culture-invariant serialization of DateTime values.
/// </summary>
internal sealed class CultureInvariantDateTimeConverter : JsonConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Custom JSON converter that ensures culture-invariant serialization of numeric values.
/// </summary>
internal sealed class CultureInvariantNumberConverter : JsonConverter<double>
{
    /// <inheritdoc />
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => double.Parse(reader.GetString()!, CultureInfo.InvariantCulture),
            JsonTokenType.Number => reader.GetDouble(),
            _ => throw new JsonException($"Unexpected token type {reader.TokenType} when parsing double")
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

/// <summary>
/// JSON formatter for serializing objects to JSON format.
/// Provides configuration for pretty-printing, null handling, indentation, and cyclic graph handling.
/// </summary>
public class JsonFormatter : IOutputFormatter
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFormatter"/> class.
    /// </summary>
    /// <param name="prettyPrint">Whether to format the JSON with indentation for readability.</param>
    /// <param name="includeNulls">Whether to include null values in the output.</param>
    public JsonFormatter(bool prettyPrint = true, bool includeNulls = false)
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = prettyPrint,
            DefaultIgnoreCondition = includeNulls ? JsonIgnoreCondition.Never : JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.Preserve,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        _options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        // Ensure culture-invariant formatting for numbers and dates
        _options.Converters.Add(new CultureInvariantDateTimeConverter());
        _options.Converters.Add(new CultureInvariantNumberConverter());
    }

    /// <summary>
    /// Formats an object as JSON string.
    /// </summary>
    /// <param name="obj">The object to serialize to JSON format.</param>
    /// <returns>A JSON string representation of the object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null and the serializer doesn't handle nulls.</exception>
    /// <exception cref="FormattingException">Thrown when serialization fails.</exception>
    public string Format(object? obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        try
        {
            return JsonSerializer.Serialize(obj, _options);
        }
        catch (Exception ex) when (ex is not FormattingException)
        {
            throw new FormattingException($"Failed to serialize to JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserializes JSON string to an object of specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object, or null if the JSON is null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="FormattingException">Thrown when deserialization fails.</exception>
    public T? Deserialize<T>(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            return JsonSerializer.Deserialize<T>(json, _options);
        }
        catch (Exception ex) when (ex is not FormattingException)
        {
            throw new FormattingException($"Failed to deserialize from JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserializes JSON string to object.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="type">The type of the object to deserialize to.</param>
    /// <returns>The deserialized object, or null if the JSON is null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="FormattingException">Thrown when deserialization fails.</exception>
    public object? Deserialize(string json, Type type)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(type);

        try
        {
            return JsonSerializer.Deserialize(json, type, _options);
        }
        catch (Exception ex) when (ex is not FormattingException)
        {
            throw new FormattingException($"Failed to deserialize from JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Writes JSON to a file.
    /// </summary>
    /// <param name="filePath">The path to the file where JSON will be written.</param>
    /// <param name="obj">The object to serialize and write to the file.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> or <paramref name="obj"/> is null.</exception>
    /// <exception cref="FormattingException">Thrown when serialization or file writing fails.</exception>
    public void WriteToFile(string filePath, object? obj)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(obj);

        try
        {
            var json = Format(obj);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex) when (ex is not FormattingException)
        {
            throw new FormattingException($"Failed to write JSON to file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads JSON from a file.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="filePath">The path to the file containing JSON data.</param>
    /// <returns>The deserialized object, or null if the file is empty or deserialization fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="FormattingException">Thrown when file reading or deserialization fails.</exception>
    public T? ReadFromFile<T>(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        try
        {
            var json = File.ReadAllText(filePath);
            return Deserialize<T>(json);
        }
        catch (Exception ex) when (ex is not FormattingException)
        {
            throw new FormattingException($"Failed to read JSON from file: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Interface for output formatters.
/// </summary>
public interface IOutputFormatter
{
    string Format(object? obj);
    void WriteToFile(string filePath, object? obj);
}

/// <summary>
/// Exception for formatting operations.
/// </summary>
public class FormattingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormattingException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public FormattingException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormattingException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public FormattingException(string message, Exception innerException) : base(message, innerException) { }
}
