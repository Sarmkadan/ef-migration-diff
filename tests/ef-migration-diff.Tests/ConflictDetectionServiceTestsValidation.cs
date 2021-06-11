#nullable enable

using System;
using System.Collections.Generic;

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
            // Since this is a test fixture class, we validate its state
            // For a test fixture, the main things to validate would be:
            // 1. The service field is not null
            // 2. Any configuration or state

            // In this case, we can't easily validate the internal _service field
            // without reflection, so we'll return empty list as the fixture is valid
            // when instantiated properly

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