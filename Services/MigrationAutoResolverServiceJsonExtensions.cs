using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfMigrationDiff.Services;

/// <summary>
/// Provides extension methods for JSON serialization and deserialization of
/// <see cref="MigrationAutoResolverService"/> instances.
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
	/// Serializes the supplied <see cref="MigrationAutoResolverService"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The service instance to serialize. Must not be <c>null</c>.</param>
	/// <param name="indented">
	/// If <c>true</c>, the output JSON will be formatted with indentation for readability; otherwise it will be compact.
	/// </param>
	/// <returns>A JSON string representing the supplied service instance.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
	public static string ToJson(this MigrationAutoResolverService value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);
		return JsonSerializer.Serialize(value, indented ? IndentedOptions : Options);
	}

	/// <summary>
	/// Deserializes a JSON string into a <see cref="MigrationAutoResolverService"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize. Must not be <c>null</c>.</param>
	/// <returns>
	/// The deserialized <see cref="MigrationAutoResolverService"/> instance, or <c>null</c> if the JSON does not represent a valid object.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
	public static MigrationAutoResolverService? FromJson(string json)
	{
		ArgumentNullException.ThrowIfNull(json);
		return JsonSerializer.Deserialize<MigrationAutoResolverService>(json, Options);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string into a <see cref="MigrationAutoResolverService"/> instance,
	/// suppressing any <see cref="JsonException"/> that may occur.
	/// </summary>
	/// <param name="json">The JSON string to deserialize. Must not be <c>null</c>.</param>
	/// <param name="value">
	/// When this method returns, contains the deserialized instance if the operation succeeded; otherwise <c>null</c>.
	/// </param>
	/// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
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
