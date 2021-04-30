#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfMigrationDiff.Formatters;

/// <summary>
/// JSON formatter for serializing objects to JSON format.
/// Provides configuration for pretty-printing, null handling, and indentation.
/// </summary>
public class JsonFormatter : IOutputFormatter
{
    private readonly JsonSerializerOptions _options;

    public JsonFormatter(bool prettyPrint = true, bool includeNulls = false)
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = prettyPrint,
            DefaultIgnoreCondition = includeNulls ? JsonIgnoreCondition.Never : JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    /// <summary>
    /// Formats an object as JSON string.
    /// </summary>
    public string Format(object? obj)
    {
        try
        {
            return JsonSerializer.Serialize(obj, _options);
        }
        catch (Exception ex)
        {
            throw new FormattingException($"Failed to serialize to JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserializes JSON string to an object of specified type.
    /// </summary>
    public T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, _options);
        }
        catch (Exception ex)
        {
            throw new FormattingException($"Failed to deserialize from JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserializes JSON string to object.
    /// </summary>
    public object? Deserialize(string json, Type type)
    {
        try
        {
            return JsonSerializer.Deserialize(json, type, _options);
        }
        catch (Exception ex)
        {
            throw new FormattingException($"Failed to deserialize from JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Writes JSON to a file.
    /// </summary>
    public void WriteToFile(string filePath, object? obj)
    {
        try
        {
            var json = Format(obj);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            throw new FormattingException($"Failed to write JSON to file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads JSON from a file.
    /// </summary>
    public T? ReadFromFile<T>(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            return Deserialize<T>(json);
        }
        catch (Exception ex)
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
    public FormattingException(string message) : base(message) { }
    public FormattingException(string message, Exception innerException) : base(message, innerException) { }
}
