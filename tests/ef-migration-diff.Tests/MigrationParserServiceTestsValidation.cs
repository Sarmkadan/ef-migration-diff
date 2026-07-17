#nullable enable

using System.Collections.Generic;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Provides validation helpers for <see cref="MigrationParserServiceTests"/> instances.
/// </summary>
public static class MigrationParserServiceTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="MigrationParserServiceTests"/> instance.
    /// </summary>
    /// <param name="value">The test instance to validate.</param>
    /// <returns>A list of validation problems; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this MigrationParserServiceTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Array.Empty<string>();
    }

    /// <summary>
    /// Determines whether the specified <see cref="MigrationParserServiceTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The test instance to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this MigrationParserServiceTests? value) => value?.Validate().Count is 0 or null;

    /// <summary>
    /// Ensures that the specified <see cref="MigrationParserServiceTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The test instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is not valid, containing the validation problems.</exception>
    public static void EnsureValid(this MigrationParserServiceTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"MigrationParserServiceTests instance is not valid. Problems: {string.Join(", ", problems)}");
        }
    }
}
