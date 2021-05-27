using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfMigrationDiff.Models;

public static class MergeAttemptJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string ToJson(this MergeAttempt value, bool indented = false)
    {
        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    public static MergeAttempt? FromJson(string json)
    {
        return JsonSerializer.Deserialize<MergeAttempt>(json, _jsonOptions);
    }

    public static bool TryFromJson(string json, out MergeAttempt? value)
    {
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