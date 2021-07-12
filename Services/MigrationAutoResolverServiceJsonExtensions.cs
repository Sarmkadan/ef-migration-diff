using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfMigrationDiff.Services;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="MigrationAutoResolverService"/>.
/// </summary>
public static class MigrationAutoResolverServiceJsonExtensions
{
	private static readonly JsonSerializerOptions Options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private static readonly JsonSerializerOptions IndentedOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	/// <summary>
	/// Serializes the <see cref="MigrationAutoResolverService"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The service instance to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the service.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
	public static string ToJson(this MigrationAutoResolverService value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);
		return JsonSerializer.Serialize(value, indented ? IndentedOptions : Options);
	}

	/// <summary>
	/// Deserializes a JSON string to a <see cref="MigrationAutoResolverService"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized service instance, or <see langword="null"/> if deserialization fails.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
	public static MigrationAutoResolverService? FromJson(string json)
	{
		ArgumentNullException.ThrowIfNull(json);
		return JsonSerializer.Deserialize<MigrationAutoResolverService>(json, Options);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string to a <see cref="MigrationAutoResolverService"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized service instance if successful; otherwise, <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
	public static bool TryFromJson(string json, out MigrationAutoResolverService? value)
	{
		ArgumentNullException.ThrowIfNull(json);

		try
		{
			value = JsonSerializer.Deserialize<MigrationAutoResolverService>(json, Options);
			return true;
		}
		catch (JsonException)
		{
			value = default;
			return false;
		}
	}
}
