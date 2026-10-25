#nullable enable
namespace EfMigrationDiff.Utilities;

/// <summary>
/// Utility for converting values between different types safely.
/// Handles null values, type mismatches, and common conversions gracefully.
/// </summary>
public static class TypeConverter
{
    private const string TrueLiteral = "true";
    private const string OneLiteral = "1";

    /// <summary>
    /// Converts a value to the specified type, returning default if conversion fails.
    /// </summary>
    /// <typeparam name="T">The target type to convert to.</typeparam>
    /// <param name="value">The value to convert. May be <c>null</c>.</param>
    /// <returns>The converted value, or <c>default</c> if <paramref name="value"/> is <c>null</c> or conversion fails.</returns>
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
                return (T)(object)Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);

            if (typeof(T) == typeof(long))
                return (T)(object)Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);

            if (typeof(T) == typeof(double))
                return (T)(object)Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);

            if (typeof(T) == typeof(decimal))
                return (T)(object)Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture);

            // Handle boolean conversion
            if (typeof(T) == typeof(bool))
            {
                if (value is string strVal)
                    return (T)(object)(strVal.Equals(TrueLiteral, StringComparison.OrdinalIgnoreCase)
                        || strVal == OneLiteral);
                return (T)(object)Convert.ToBoolean(value, System.Globalization.CultureInfo.InvariantCulture);
            }

            // Handle date/time conversions
            if (typeof(T) == typeof(DateTime))
                return (T)(object)Convert.ToDateTime(value, System.Globalization.CultureInfo.InvariantCulture);

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
    /// <typeparam name="T">The target type to convert to.</typeparam>
    /// <param name="value">The value to convert. May be <c>null</c>.</param>
    /// <param name="result">When this method returns, contains the converted value, or <c>default</c> if conversion failed.</param>
    /// <returns><c>true</c> if the call completed without throwing; otherwise, <c>false</c>.</returns>
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
    /// <typeparam name="T">The enum type to parse into.</typeparam>
    /// <param name="value">The name of the enum member to parse. Matching is case-insensitive.</param>
    /// <returns>The parsed enum value, or <c>null</c> if <paramref name="value"/> is null or empty or does not match a member of <typeparamref name="T"/>.</returns>
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
    /// <typeparam name="T">The enum type of <paramref name="value"/>.</typeparam>
    /// <param name="value">The enum value to convert.</param>
    /// <returns>The name of the enum member, or the numeric value as a string if no member matches.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static string EnumToString<T>(T value) where T : Enum
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.ToString();
    }

    /// <summary>
    /// Converts a value to dictionary of string/object pairs.
    /// </summary>
    /// <param name="obj">The object whose public instance properties are copied. May be <c>null</c>.</param>
    /// <returns>A dictionary keyed by property name. Empty if <paramref name="obj"/> is <c>null</c>; properties that cannot be read are omitted.</returns>
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
    /// <typeparam name="T">The reference type to create. Must have a public parameterless constructor.</typeparam>
    /// <param name="dict">The key/value pairs to assign to writable public properties. Keys are matched case-insensitively.</param>
    /// <returns>A new <typeparamref name="T"/> with matching properties set. Keys that match no writable property are ignored.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dict"/> is <c>null</c>.</exception>
    public static T? DictionaryToObject<T>(Dictionary<string, object?> dict) where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(dict);
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
    /// <param name="value">The value to convert. May be <c>null</c>.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <returns>The converted value, or <c>null</c> if <paramref name="value"/> is <c>null</c> or conversion fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="targetType"/> is <c>null</c>.</exception>
    public static object? ConvertTo(object? value, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        if (value is null)
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
    /// <typeparam name="T">The target type to check.</typeparam>
    /// <param name="value">The value to check. May be <c>null</c>.</param>
    /// <returns><c>true</c> if the conversion completed without throwing; otherwise, <c>false</c>.</returns>
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
