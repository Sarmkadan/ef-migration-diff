#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Utilities;

/// <summary>
/// Utility for converting values between different types safely.
/// Handles null values, type mismatches, and common conversions gracefully.
/// </summary>
public static class TypeConverter
{
    /// <summary>
    /// Converts a value to the specified type, returning default if conversion fails.
    /// </summary>
    public static T? ConvertTo<T>(object? value)
    {
        if (value is null)
            return default;

        if (value is T typedValue)
            return typedValue;

        try
        {
            // Handle string conversions
            if (typeof(T) == typeof(string))
            {
                var stringValue = value.ToString();
                return stringValue is null ? default : (T)(object)stringValue;
            }

            // Handle numeric conversions
            if (typeof(T) == typeof(int))
                return (T)(object)Convert.ToInt32(value);

            if (typeof(T) == typeof(long))
                return (T)(object)Convert.ToInt64(value);

            if (typeof(T) == typeof(double))
                return (T)(object)Convert.ToDouble(value);

            if (typeof(T) == typeof(decimal))
                return (T)(object)Convert.ToDecimal(value);

            // Handle boolean conversion
            if (typeof(T) == typeof(bool))
            {
                if (value is string strVal)
                    return (T)(object)(strVal.Equals("true", StringComparison.OrdinalIgnoreCase) || strVal == "1");
                return (T)(object)Convert.ToBoolean(value);
            }

            // Handle date/time conversions
            if (typeof(T) == typeof(DateTime))
                return (T)(object)Convert.ToDateTime(value);

            // Handle Guid
            if (typeof(T) == typeof(Guid))
            {
                var guidValue = value.ToString();
                return string.IsNullOrWhiteSpace(guidValue) ? default : (T)(object)Guid.Parse(guidValue);
            }

            // Try direct conversion
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Tries to convert a value, returning true if successful.
    /// </summary>
    public static bool TryConvertTo<T>(object? value, out T? result)
    {
        result = default;
        try
        {
            result = ConvertTo<T>(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a string to an enum value safely.
    /// </summary>
    public static T? StringToEnum<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
            return null;

        try
        {
            return (T)Enum.Parse(typeof(T), value, ignoreCase: true);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts an enum to its string representation.
    /// </summary>
    public static string EnumToString<T>(T value) where T : Enum
    {
        return value.ToString();
    }

    /// <summary>
    /// Converts a value to dictionary of string/object pairs.
    /// </summary>
    public static Dictionary<string, object?> ObjectToDictionary(object? obj)
    {
        var dict = new Dictionary<string, object?>();

        if (obj is null)
            return dict;

        var properties = obj.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var prop in properties)
        {
            try
            {
                dict[prop.Name] = prop.GetValue(obj);
            }
            catch
            {
                // Skip properties that can't be read
            }
        }

        return dict;
    }

    /// <summary>
    /// Converts a dictionary to an object of the specified type.
    /// </summary>
    public static T? DictionaryToObject<T>(Dictionary<string, object?> dict) where T : class, new()
    {
        var obj = new T();

        foreach (var kvp in dict)
        {
            var property = typeof(T).GetProperty(kvp.Key, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Instance);
            if (property?.CanWrite ?? false)
            {
                try
                {
                    var value = ConvertTo(kvp.Value, property.PropertyType);
                    property.SetValue(obj, value);
                }
                catch
                {
                    // Skip properties that can't be set
                }
            }
        }

        return obj;
    }

    /// <summary>
    /// Generic conversion method for dynamic type handling.
    /// </summary>
    public static object? ConvertTo(object? value, Type targetType)
    {
        if (value is null || targetType is null)
            return null;

        if (targetType.IsAssignableFrom(value.GetType()))
            return value;

        try
        {
            if (targetType == typeof(string))
                return value.ToString();

            if (targetType.IsEnum && value is string strVal)
                return Enum.Parse(targetType, strVal, ignoreCase: true);

            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if a value can be converted to the target type.
    /// </summary>
    public static bool CanConvertTo<T>(object? value)
    {
        try
        {
            _ = ConvertTo<T>(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
