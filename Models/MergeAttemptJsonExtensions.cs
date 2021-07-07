using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfMigrationDiff.Models;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="MergeAttempt"/>.
/// </summary>
public static class MergeAttemptJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	/// <summary>
	/// Serializes a <see cref="MergeAttempt"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The merge attempt to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the merge attempt.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
	public static string ToJson(this MergeAttempt value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		var options = indented
			? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
			: _jsonOptions;

		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Deserializes a JSON string to a <see cref="MergeAttempt"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized merge attempt, or <see langword="null"/> if deserialization fails.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/></exception>
	public static MergeAttempt? FromJson(string json)
	{
		ArgumentNullException.ThrowIfNull(json);

		return JsonSerializer.Deserialize<MergeAttempt>(json, _jsonOptions);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string to a <see cref="MergeAttempt"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized merge attempt if successful; otherwise, <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/></exception>
	public static bool TryFromJson(string json, out MergeAttempt? value)
	{
		ArgumentNullException.ThrowIfNull(json);

		try
		{
			value = JsonSerializer.Deserialize<MergeAttempt>(json, _jsonOptions);
			return true;
		}
		catch (JsonException)
		{
			value = null;
			return false;
		}
	}
}