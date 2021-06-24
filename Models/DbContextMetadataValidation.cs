#nullable enable

namespace EfMigrationDiff.Models;

/// <summary>
/// Provides validation and validation-related extension methods for <see cref="DbContextMetadata"/>.
/// </summary>
public static class DbContextMetadataValidation
{
    /// <summary>
    /// Validates the given <see cref="DbContextMetadata"/> instance and returns a list of human-readable validation problems.
    /// Returns an empty list if the instance is valid.
    /// </summary>
    /// <param name="value">The metadata instance to validate.</param>
    /// <returns>A read-only list of validation error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this DbContextMetadata value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate required string properties
        if (string.IsNullOrWhiteSpace(value.Id))
        {
            errors.Add("Id is required and cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(value.ContextName))
        {
            errors.Add("ContextName is required and cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(value.AssemblyName))
        {
            errors.Add("AssemblyName is required and cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(value.Namespace))
        {
            errors.Add("Namespace is required and cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(value.DatabaseProvider))
        {
            errors.Add("DatabaseProvider is required and cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(value.ConnectionString))
        {
            errors.Add("ConnectionString is required and cannot be null or whitespace.");
        }

        // Validate LastScannedAt is not default (uninitialized)
        if (value.LastScannedAt == default)
        {
            errors.Add("LastScannedAt must be set to a valid DateTime value.");
        }

        // Validate collection properties
        if (value.MigrationIds is null)
        {
            errors.Add("MigrationIds collection cannot be null.");
        }

        if (value.EntityTypes is null)
        {
            errors.Add("EntityTypes collection cannot be null.");
        }

        if (value.Properties is null)
        {
            errors.Add("Properties dictionary cannot be null.");
        }

        return errors.AsReadOnly();
    }


    /// <summary>
    /// Determines whether the given <see cref="DbContextMetadata"/> instance is valid.
    /// </summary>
    /// <param name="value">The metadata instance to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this DbContextMetadata value)
    {
        return value.Validate().Count == 0;
    }


    /// <summary>
    /// Ensures that the given <see cref="DbContextMetadata"/> instance is valid.
    /// Throws an <see cref="ArgumentException"/> with detailed validation messages if the instance is invalid.
    /// </summary>
    /// <param name="value">The metadata instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid, containing a list of validation errors.</exception>
    public static void EnsureValid(this DbContextMetadata value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();

        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"DbContextMetadata validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }
}