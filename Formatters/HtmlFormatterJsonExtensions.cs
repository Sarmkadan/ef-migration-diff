#nullable enable

namespace EfMigrationDiff.Formatters;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="HtmlFormatter"/>.
/// </summary>
public static class HtmlFormatterJsonExtensions
{
    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new(System.Text.Json.JsonSerializerOptions.Default)
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the <see cref="HtmlFormatter"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The formatter instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the formatter.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this HtmlFormatter value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new System.Text.Json.JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true
            }
            : _jsonOptions;

        return System.Text.Json.JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to an <see cref="HtmlFormatter"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="HtmlFormatter"/> instance, or null if the JSON is null or empty.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="System.Text.Json.JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static HtmlFormatter? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return System.Text.Json.JsonSerializer.Deserialize<HtmlFormatter>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to an <see cref="HtmlFormatter"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out HtmlFormatter? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = System.Text.Json.JsonSerializer.Deserialize<HtmlFormatter>(json, _jsonOptions);
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}