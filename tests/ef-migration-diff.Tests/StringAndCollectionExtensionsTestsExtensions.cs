using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Provides extension methods to run and manage tests for <see cref="StringAndCollectionExtensionsTests"/>.
    /// This class contains helper methods for executing test methods in bulk and collecting test metadata.
    /// </summary>
    public static class StringAndCollectionExtensionsTestsExtensions
    {
        /// <summary>
        /// Runs all string conversion tests (ToPascalCase, ToSnakeCase, Truncate) on the test instance.
        /// </summary>
        /// <param name="testInstance">The test instance containing the methods to invoke.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testInstance"/> is null.</exception>
        public static void RunAllStringConversionTests(this StringAndCollectionExtensionsTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            var stringMethods = typeof(StringAndCollectionExtensionsTests)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith("ToPascalCase") || m.Name.StartsWith("ToSnakeCase") || m.Name.StartsWith("Truncate"))
                .ToList();

            foreach (var method in stringMethods)
            {
                method.Invoke(testInstance, null);
            }
        }

        /// <summary>
        /// Runs all collection batching tests (Batch) on the test instance.
        /// </summary>
        /// <param name="testInstance">The test instance containing the methods to invoke.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testInstance"/> is null.</exception>
        public static void RunAllCollectionBatchingTests(this StringAndCollectionExtensionsTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            var batchMethods = typeof(StringAndCollectionExtensionsTests)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith("Batch"))
                .ToList();

            foreach (var method in batchMethods)
            {
                method.Invoke(testInstance, null);
            }
        }

        /// <summary>
        /// Gets the names of all public instance methods in <see cref="StringAndCollectionExtensionsTests"/>.
        /// </summary>
        /// <param name="testInstance">The test instance (unused, required for extension method syntax).</param>
        /// <returns>A list of method names.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testInstance"/> is null.</exception>
        public static List<string> GetAllTestNames(this StringAndCollectionExtensionsTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            return typeof(StringAndCollectionExtensionsTests)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(m => m.Name)
                .ToList();
        }

        /// <summary>
        /// Invokes all public instance methods on the test instance and returns true if all succeed.
        /// </summary>
        /// <param name="testInstance">The test instance containing the methods to invoke.</param>
        /// <returns>True if all tests pass; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testInstance"/> is null.</exception>
        /// <remarks>
        /// This method catches exceptions and logs them to the console, allowing execution to continue
        /// through all methods even if some fail. This is useful for bulk test execution scenarios.
        /// </remarks>
        public static bool AssertAllTestsPass(this StringAndCollectionExtensionsTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            var allMethods = typeof(StringAndCollectionExtensionsTests)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .ToList();

            foreach (var method in allMethods)
            {
                try
                {
                    method.Invoke(testInstance, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Test failed: {method.Name} - {ex.InnerException?.Message ?? ex.Message}");
                    return false;
                }
            }

            return true;
        }
    }
}