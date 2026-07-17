using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Provides extension methods for <see cref="MigrationDiffServiceTests"/> to facilitate test execution and validation.
    /// </summary>
    public static class MigrationDiffServiceTestsExtensions
    {
        /// <summary>
        /// Runs all test methods in the provided <paramref name="testInstance"/> that start with the specified <paramref name="prefix"/>.
        /// </summary>
        /// <param name="testInstance">The instance of <see cref="MigrationDiffServiceTests"/> containing the tests to run.</param>
        /// <param name="prefix">The prefix used to filter test methods by name.</param>
        /// <exception cref="ArgumentNullException"><paramref name="testInstance"/> or <paramref name="prefix"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="prefix"/> is <see cref="string.Empty"/> or consists only of whitespace.</exception>
        public static void RunAllTestsStartingWith(this MigrationDiffServiceTests testInstance, string prefix)
        {
            ArgumentNullException.ThrowIfNull(testInstance);
            ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

            var testMethods = GetTestMethods(testInstance.GetType(), prefix);
            foreach (var method in testMethods)
            {
                method.Invoke(testInstance, null);
            }
        }

        /// <summary>
        /// Asserts that all test methods in the provided <paramref name="testInstance"/> pass without throwing exceptions.
        /// </summary>
        /// <param name="testInstance">The instance of <see cref="MigrationDiffServiceTests"/> containing the tests to validate.</param>
        /// <returns><c>true</c> if all tests pass; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testInstance"/> is <see langword="null"/>.</exception>
        public static bool AssertAllTestsPass(this MigrationDiffServiceTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            var testMethods = GetTestMethods(testInstance.GetType(), string.Empty);
            foreach (var method in testMethods)
            {
                try
                {
                    method.Invoke(testInstance, null);
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Retrieves the names of all test methods in the provided <paramref name="testInstance"/>.
        /// </summary>
        /// <param name="testInstance">The instance of <see cref="MigrationDiffServiceTests"/> to inspect.</param>
        /// <param name="prefix">The prefix used to filter method names. If empty, returns all test method names.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the names of all test methods.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testInstance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="prefix"/> is <see langword="null"/>.</exception>
        public static IEnumerable<string> GetAllTestMethodNames(this MigrationDiffServiceTests testInstance, string prefix = "")
        {
            ArgumentNullException.ThrowIfNull(testInstance);
            ArgumentNullException.ThrowIfNull(prefix);

            return GetTestMethods(testInstance.GetType(), prefix)
                .Select(m => m.Name);
        }

        /// <summary>
        /// Gets all public instance methods of the specified <paramref name="type"/> that start with the given <paramref name="prefix"/>.
        /// </summary>
        /// <param name="type">The type to inspect for test methods.</param>
        /// <param name="prefix">The prefix used to filter methods by name.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="MethodInfo"/> objects representing the test methods.</returns>
        private static IEnumerable<MethodInfo> GetTestMethods(Type type, string prefix)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith(prefix, StringComparison.Ordinal) && m.ReturnType == typeof(void));
        }
    }
}