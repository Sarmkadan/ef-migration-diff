#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Provides validation helpers for <see cref="ConflictDetectionServiceTests"/> instances.
    /// </summary>
    public static class ConflictDetectionServiceTestsValidation
    {
        /// <summary>
        /// Validates the specified <see cref="ConflictDetectionServiceTests"/> instance.
        /// </summary>
        /// <param name="value">The test fixture instance to validate.</param>
        /// <returns>A list of human-readable validation problems; empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this ConflictDetectionServiceTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // Validate the test fixture instance itself
            // Check that the service field is properly initialized
            try
            {
                var serviceField = typeof(ConflictDetectionServiceTests).GetField("_service",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (serviceField is null)
                {
                    problems.Add("Test fixture does not contain expected _service field");
                }
                else
                {
                    var service = serviceField.GetValue(value);
                    if (service is null)
                    {
                        problems.Add("Test fixture _service field is null");
                    }
                }
            }
            catch (Exception ex)
            {
                problems.Add($"Failed to validate test fixture state: {ex.Message}");
            }

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the specified <see cref="ConflictDetectionServiceTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The test fixture instance to check.</param>
        /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(this ConflictDetectionServiceTests value)
        {
            return value.Validate().Count == 0;
        }

        /// <summary>
        /// Ensures that the specified <see cref="ConflictDetectionServiceTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The test fixture instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not valid, containing a list of validation problems.</exception>
        public static void EnsureValid(this ConflictDetectionServiceTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();
            if (problems.Count > 0)
            {
                throw new ArgumentException(
                    $"ConflictDetectionServiceTests instance is not valid. Problems: {string.Join("; ", problems)}");
            }
        }
    }
}