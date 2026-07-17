#nullable enable

using System.Globalization;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Provides validation methods for <see cref="MigrationServicesTests"/> instances.
/// </summary>
public static class MigrationServicesTestsValidation
{
    /// <summary>
    /// Validates a <see cref="MigrationServicesTests"/> instance.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this MigrationServicesTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate test methods are not null
        // These are method groups, so we check if they're null directly
        if (value.DetectChanges_WithCreateTableContent_DetectsOneCreateTableChange == null)
        {
            problems.Add("DetectChanges_WithCreateTableContent_DetectsOneCreateTableChange is null");
        }

        if (value.IsMigrationSafe_WithDropTableContent_ReturnsFalse == null)
        {
            problems.Add("IsMigrationSafe_WithDropTableContent_ReturnsFalse is null");
        }

        if (value.DetectConflicts_WhenSameTableCreatedWithDifferentSchema_ReturnsNamingConflict == null)
        {
            problems.Add("DetectConflicts_WhenSameTableCreatedWithDifferentSchema_ReturnsNamingConflict is null");
        }

        if (value.DetectConflicts_WhenSameColumnModifiedWithDifferentDefaultValue_ReturnsColumnConflict == null)
        {
            problems.Add("DetectConflicts_WhenSameColumnModifiedWithDifferentDefaultValue_ReturnsColumnConflict is null");
        }

        if (value.DetectConflicts_WhenSameColumnModifiedWithSameDefaultValue_ReturnsNoConflicts == null)
        {
            problems.Add("DetectConflicts_WhenSameColumnModifiedWithSameDefaultValue_ReturnsNoConflicts is null");
        }

        if (value.ExecuteAsync_WithRegisteredMockedCommand_InvokesCommandExactlyOnce == null)
        {
            problems.Add("ExecuteAsync_WithRegisteredMockedCommand_InvokesCommandExactlyOnce is null");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="MigrationServicesTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this MigrationServicesTests? value) => value?.Validate().Count == 0;

    /// <summary>
    /// Ensures that a <see cref="MigrationServicesTests"/> instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is invalid.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static void EnsureValid(this MigrationServicesTests? value)
    {
        var problems = value.Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"MigrationServicesTests instance is invalid. Problems: {string.Join(", ", problems)}");
        }
    }
}
