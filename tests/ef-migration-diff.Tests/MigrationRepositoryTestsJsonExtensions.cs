#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace EfMigrationDiff.Tests;

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

    public static string ToJson(this MigrationRepositoryTests value, bool indented = false)
    {
        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;
        return JsonSerializer.Serialize(value, options);
    }

    public static MigrationRepositoryTests? FromJson(string json)
    {
        return JsonSerializer.Deserialize<MigrationRepositoryTests>(json, _jsonOptions);
    }

    public static bool TryFromJson(string json, out MigrationRepositoryTests? value)
    {
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