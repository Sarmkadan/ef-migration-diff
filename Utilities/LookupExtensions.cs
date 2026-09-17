#nullable enable
using System.Text.RegularExpressions;
using EfMigrationDiff.Utilities;

namespace EfMigrationDiff.Extensions;

/// <summary>
/// Extension methods for looking up values from constants.
/// </summary>
public static class LookupExtensions
{
    /// <summary>
    /// Checks if the input string matches the migration ID pattern defined in <see cref="Constants.Patterns.MigrationIdPattern"/>.
    /// </summary>
    /// <param name="input">The string to check.</param>
    /// <returns>True if the input matches the migration ID pattern; otherwise, false.</returns>
    public static bool IsMigrationId(this string? input)
    {
        if (input is null)
            return false;

        return Regex.IsMatch(input, Constants.Patterns.MigrationIdPattern);
    }
}