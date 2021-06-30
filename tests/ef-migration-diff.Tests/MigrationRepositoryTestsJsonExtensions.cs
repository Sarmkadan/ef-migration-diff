#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Provides JSON serialization extensions for <see cref="MigrationRepositoryTests"/> instances.
/// </summary>
public static class MigrationRepositoryTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        {
            Modifiers = {
                static ti =>
                {
                    if (ti.Type == typeof(MigrationRepositoryTests))
                    {
                        foreach (var prop in ti.Properties)
                        {
                            prop.ShouldSerialize = (_, _) => true;
                        }
                    }
                }
            }
        }
    };

    /// <summary>
    /// Serializes the specified <see cref="MigrationRepositoryTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The value to serialize. Can be <see langword="null"/>.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the value, or <see langword="null"/> if <paramref name="value"/> is <see langword="null"/>.</returns>
    public static string ToJson(this MigrationRepositoryTests? value, bool indented = false)
        => JsonSerializer.Serialize(value, indented ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true } : _jsonOptions);

    /// <summary>
    /// Deserializes a JSON string to a <see cref="MigrationRepositoryTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="MigrationRepositoryTests"/> instance, or <see langword="null"/> if deserialization fails.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public static MigrationRepositoryTests? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<MigrationRepositoryTests>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="MigrationRepositoryTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">When this method returns, contains the deserialized value if successful, or <see langword="null"/> if deserialization failed.</param>
    /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public static bool TryFromJson(string json, out MigrationRepositoryTests? value)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            value = JsonSerializer.Deserialize<MigrationRepositoryTests>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}