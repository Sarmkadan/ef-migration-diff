#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Extensions;

/// <summary>
/// Extension methods for string type providing common text manipulation operations.
/// Includes trimming, case conversion, null-safe checks, and formatting utilities.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Checks if string is null, empty, or contains only whitespace.
    /// </summary>
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// Checks if string is null, empty, or contains only whitespace.
    /// </summary>
    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Returns the string or a default value if null or empty.
    /// </summary>
    public static string OrEmpty(this string? value)
    {
        return value ?? string.Empty;
    }

    /// <summary>
    /// Returns the string or a default value if null or empty.
    /// </summary>
    public static string Or(this string? value, string defaultValue)
    {
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    /// <summary>
    /// Ensures string ends with the specified suffix.
    /// </summary>
    public static string EnsureEndsWith(this string value, string suffix)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        return value.EndsWith(suffix, StringComparison.Ordinal) ? value : value + suffix;
    }

    /// <summary>
    /// Ensures string starts with the specified prefix.
    /// </summary>
    public static string EnsureStartsWith(this string value, string prefix)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        return value.StartsWith(prefix, StringComparison.Ordinal) ? value : prefix + value;
    }

    /// <summary>
    /// Removes a prefix from the string if it exists.
    /// </summary>
    public static string RemovePrefix(this string value, string prefix)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        return value.StartsWith(prefix, StringComparison.Ordinal)
            ? value.Substring(prefix.Length)
            : value;
    }

    /// <summary>
    /// Removes a suffix from the string if it exists.
    /// </summary>
    public static string RemoveSuffix(this string value, string suffix)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        return value.EndsWith(suffix, StringComparison.Ordinal)
            ? value.Substring(0, value.Length - suffix.Length)
            : value;
    }

    /// <summary>
    /// Converts string to PascalCase format.
    /// </summary>
    public static string ToPascalCase(this string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var parts = value.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant()));
    }

    /// <summary>
    /// Converts string to camelCase format.
    /// </summary>
    public static string ToCamelCase(this string value)
    {
        var pascalCase = value.ToPascalCase();
        if (string.IsNullOrEmpty(pascalCase)) return pascalCase;
        return char.ToLowerInvariant(pascalCase[0]) + pascalCase.Substring(1);
    }

    /// <summary>
    /// Converts string to snake_case format.
    /// </summary>
    public static string ToSnakeCase(this string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            if (i > 0 && char.IsUpper(value[i]) && (char.IsLower(value[i - 1]) || (i + 1 < value.Length && char.IsLower(value[i + 1]))))
            {
                result.Append('_');
            }
            result.Append(char.ToLowerInvariant(value[i]));
        }
        return result.ToString();
    }

    /// <summary>
    /// Converts string to kebab-case format.
    /// </summary>
    public static string ToKebabCase(this string value)
    {
        return value.ToSnakeCase().Replace('_', '-');
    }

    /// <summary>
    /// Truncates string to specified length and appends ellipsis if truncated.
    /// </summary>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value.Length <= maxLength) return value;
        return value.Substring(0, Math.Max(0, maxLength - suffix.Length)) + suffix;
    }

    /// <summary>
    /// Repeats the string the specified number of times.
    /// </summary>
    public static string Repeat(this string value, int count)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        return string.Concat(Enumerable.Repeat(value, count));
    }

    /// <summary>
    /// Counts occurrences of a substring.
    /// </summary>
    public static int CountOccurrences(this string value, string substring)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(substring)) return 0;
        return (value.Length - value.Replace(substring, string.Empty).Length) / substring.Length;
    }
}
