#nullable enable

namespace EfMigrationDiff.Extensions;

/// <summary>
/// Extension methods for string type providing common text manipulation operations.
/// Includes trimming, case conversion, null-safe checks, and formatting utilities.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Checks if string is null or empty.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string is null or empty; otherwise, false.</returns>
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// Checks if string is null, empty, or contains only whitespace.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string is null, empty, or whitespace; otherwise, false.</returns>
    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Returns the string or an empty string if null or empty.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>The original string if not null or empty; otherwise, an empty string.</returns>
    public static string OrEmpty(this string? value)
    {
        return value ?? string.Empty;
    }

    /// <summary>
    /// Returns the string or a default value if null or empty.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="defaultValue">The value to return if the string is null or empty.</param>
    /// <returns>The original string if not null or empty; otherwise, the default value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="defaultValue"/> is null.</exception>
    public static string Or(this string? value, string defaultValue)
    {
        ArgumentNullException.ThrowIfNull(defaultValue);
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    /// <summary>
    /// Ensures string ends with the specified suffix.
    /// </summary>
    /// <param name="value">The string to check and modify.</param>
    /// <param name="suffix">The suffix to ensure.</param>
    /// <returns>The original string if it already ends with the suffix; otherwise, the string with the suffix appended.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> or <paramref name="suffix"/> is null.</exception>
    public static string EnsureEndsWith(this string value, string suffix)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(suffix);

        return value.EndsWith(suffix, StringComparison.Ordinal) ? value : value + suffix;
    }

    /// <summary>
    /// Ensures string starts with the specified prefix.
    /// </summary>
    /// <param name="value">The string to check and modify.</param>
    /// <param name="prefix">The prefix to ensure.</param>
    /// <returns>The original string if it already starts with the prefix; otherwise, the prefix concatenated with the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> or <paramref name="prefix"/> is null.</exception>
    public static string EnsureStartsWith(this string value, string prefix)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(prefix);

        return value.StartsWith(prefix, StringComparison.Ordinal) ? value : prefix + value;
    }

    /// <summary>
    /// Removes a prefix from the string if it exists.
    /// </summary>
    /// <param name="value">The string to process.</param>
    /// <param name="prefix">The prefix to remove.</param>
    /// <returns>The string with the prefix removed if it was present; otherwise, the original string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> or <paramref name="prefix"/> is null.</exception>
    public static string RemovePrefix(this string value, string prefix)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(prefix);

        return value.StartsWith(prefix, StringComparison.Ordinal)
            ? value[prefix.Length..]
            : value;
    }

    /// <summary>
    /// Removes a suffix from the string if it exists.
    /// </summary>
    /// <param name="value">The string to process.</param>
    /// <param name="suffix">The suffix to remove.</param>
    /// <returns>The string with the suffix removed if it was present; otherwise, the original string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> or <paramref name="suffix"/> is null.</exception>
    public static string RemoveSuffix(this string value, string suffix)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(suffix);

        return value.EndsWith(suffix, StringComparison.Ordinal)
            ? value[..^suffix.Length]
            : value;
    }

    /// <summary>
    /// Converts string to PascalCase format.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The PascalCase formatted string, or the original string if empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToPascalCase(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrEmpty(value))
            return value;

        var parts = value.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return value;

        return string.Concat(parts.Select(p =>
            p.Length == 0
                ? string.Empty
                : char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }

    /// <summary>
    /// Converts string to camelCase format.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The camelCase formatted string, or the original string if empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToCamelCase(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var pascalCase = value.ToPascalCase();
        return string.IsNullOrEmpty(pascalCase)
            ? pascalCase
            : char.ToLowerInvariant(pascalCase[0]) + pascalCase[1..];
    }

    /// <summary>
    /// Converts string to snake_case format.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The snake_case formatted string, or the original string if empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToSnakeCase(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrEmpty(value))
            return value;

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            if (i > 0 && char.IsUpper(value[i]))
            {
                bool shouldAddUnderscore =
                    char.IsLower(value[i - 1]) ||
                    (i + 1 < value.Length && char.IsLower(value[i + 1]));

                if (shouldAddUnderscore)
                {
                    result.Append('_');
                }
            }

            result.Append(char.ToLowerInvariant(value[i]));
        }
        return result.ToString();
    }

    /// <summary>
    /// Converts string to kebab-case format.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The kebab-case formatted string, or the original string if empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToKebabCase(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.ToSnakeCase().Replace('_', '-');
    }

    /// <summary>
    /// Truncates string to specified length and appends ellipsis if truncated.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum length of the result.</param>
    /// <param name="suffix">The suffix to append when truncated. Defaults to "...".</param>
    /// <returns>The truncated string with ellipsis if it was longer than maxLength; otherwise, the original string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> or <paramref name="suffix"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLength"/> is negative.</exception>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(suffix);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        if (value.Length <= maxLength)
            return value;

        int charsToKeep = Math.Max(0, maxLength - suffix.Length);
        return value[..charsToKeep] + suffix;
    }

    /// <summary>
    /// Repeats the string the specified number of times.
    /// </summary>
    /// <param name="value">The string to repeat.</param>
    /// <param name="count">The number of times to repeat the string.</param>
    /// <returns>A new string consisting of the original string repeated count times.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    public static string Repeat(this string value, int count)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        return string.Concat(Enumerable.Repeat(value, count));
    }

    /// <summary>
    /// Counts occurrences of a substring.
    /// </summary>
    /// <param name="value">The string to search within.</param>
    /// <param name="substring">The substring to count.</param>
    /// <returns>The number of times the substring appears in the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> or <paramref name="substring"/> is null.</exception>
    public static int CountOccurrences(this string value, string substring)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(substring);

        if (substring.Length == 0)
            return 0;

        int count = 0;
        int index = 0;
        while ((index = value.IndexOf(substring, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }
}