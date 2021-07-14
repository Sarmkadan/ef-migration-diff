using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Provides extension methods for executing and managing test methods in <see cref="StringAndCollectionExtensionsTests"/>.
    /// This class enables bulk execution of test methods and collection of test metadata.
    /// </summary>
    public static class StringAndCollectionExtensionsTestsExtensions
    {
        /// <summary>
        /// Invokes all string conversion test methods (ToPascalCase, ToSnakeCase, Truncate) on the test instance.
        /// </summary>
        /// <param name="testInstance">The test instance containing the string conversion test methods to execute.</param>
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
        /// Invokes all collection batching test methods (Batch) on the test instance.
        /// </summary>
        /// <param name="testInstance">The test instance containing the collection batching test methods to execute.</param>
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
        /// Retrieves the names of all public instance methods in <see cref="StringAndCollectionExtensionsTests"/>.
        /// </summary>
        /// <param name="testInstance">The test instance (required for extension method syntax, not used in implementation).</param>
        /// <returns>A list of method names as strings.</returns>
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
        /// Executes all public instance methods on the test instance and returns true if all succeed.
        /// </summary>
        /// <param name="testInstance">The test instance containing the methods to invoke.</param>
        /// <returns>True if all tests pass successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testInstance"/> is null.</exception>
        /// <remarks>
        /// This method catches exceptions during test execution, logs failure details to the console,
        /// and continues executing remaining tests. This allows bulk test execution with detailed failure reporting.
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
