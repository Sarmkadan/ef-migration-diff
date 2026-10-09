#nullable enable

using System.Text;

namespace EfMigrationDiff.Models;

/// <summary>
/// Provides display-name helpers for the enums declared in <c>MigrationEnums.cs</c>.
/// </summary>
public static class MigrationEnumsExtensions
{
    /// <summary>
    /// Converts an enum member name from PascalCase into a human-readable display name,
    /// e.g. <c>CreateTable</c> becomes <c>Create Table</c> and <c>DDLStatement</c> becomes <c>DDL Statement</c>.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The enum value to convert.</param>
    /// <returns>The display name of the value. Undefined numeric values are rendered as their numeric string.</returns>
    public static string ToDisplayName<TEnum>(this TEnum value) where TEnum : struct, Enum
    {
        var name = value.ToString();

        if (!Enum.IsDefined(value))
            return name;

        var builder = new StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];

            if (i > 0 && char.IsUpper(current))
            {
                var previous = name[i - 1];
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);

                // Break before an uppercase letter that follows a lowercase letter or digit,
                // or that starts a new word after an acronym (e.g. "DDLStatement" -> "DDL Statement").
                if (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && nextIsLower))
                    builder.Append(' ');
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
