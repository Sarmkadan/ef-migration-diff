using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfMigrationDiff.Services;

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

    public static string ToJson(this MigrationAutoResolverService value, bool indented = false)
    {
        return JsonSerializer.Serialize(value, indented ? IndentedOptions : Options);
    }

    public static MigrationAutoResolverService? FromJson(string json)
    {
        return JsonSerializer.Deserialize<MigrationAutoResolverService>(json, Options);
    }

    public static bool TryFromJson(string json, out MigrationAutoResolverService? value)
    {
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
