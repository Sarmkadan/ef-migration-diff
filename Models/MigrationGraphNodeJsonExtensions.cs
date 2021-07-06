using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfMigrationDiff.Models;

/// <summary>
/// Provides extension methods for serializing and deserializing <see cref="MigrationGraphNode"/> instances to and from JSON.
/// </summary>
public static class MigrationGraphNodeJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	/// <summary>
	/// Serializes a <see cref="MigrationGraphNode"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The migration graph node to serialize. Can be null.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the migration graph node, or an empty object if the value is null.</returns>
	public static string ToJson(this MigrationGraphNode? value, bool indented = false)
	{
		if (value is null)
		{
			return "{}";
		}

		var options = indented
			? new JsonSerializerOptions(_jsonOptions)
			{
				WriteIndented = true
			}
			: _jsonOptions;

		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Deserializes a JSON string to a <see cref="MigrationGraphNode"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize. Can be null or whitespace.</param>
	/// <returns>The deserialized migration graph node, or null if the JSON is invalid or empty.</returns>
	public static MigrationGraphNode? FromJson(string? json)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(json);

		try
		{
			return JsonSerializer.Deserialize<MigrationGraphNode>(json, _jsonOptions);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	/// <summary>
	/// Attempts to deserialize a JSON string to a <see cref="MigrationGraphNode"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize. Can be null or whitespace.</param>
	/// <param name="value">Receives the deserialized migration graph node if successful.</param>
	/// <returns>True if deserialization succeeded; otherwise, false.</returns>
	public static bool TryFromJson(string? json, out MigrationGraphNode? value)
	{
		value = null;
		if (string.IsNullOrWhiteSpace(json))
		{
			return false;
		}

		try
		{
			value = JsonSerializer.Deserialize<MigrationGraphNode>(json, _jsonOptions);
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}
}