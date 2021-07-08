using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Provides extension methods for <see cref="ValidationHelperTests"/> to enhance test execution and discovery.
    /// </summary>
    public static class ValidationHelperTestsExtensions
    {
        /// <summary>
        /// Gets the names of all test methods in the <see cref="ValidationHelperTests"/> class.
        /// </summary>
        /// <param name="testInstance">The test instance (not used, but required for extension method signature).</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the names of all test methods.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testInstance"/> is null.</exception>
        public static IEnumerable<string> GetTestMethodNames(this ValidationHelperTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            return typeof(ValidationHelperTests)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name.StartsWith("IsValid") || m.Name.StartsWith("Sanitize"))
                .Select(m => m.Name);
        }

        /// <summary>
        /// Runs a specific test method by name and asserts it completes without exceptions.
        /// </summary>
        /// <param name="testInstance">The test instance (not used, but required for extension method signature).</param>
        /// <param name="testMethodName">The name of the test method to execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testInstance"/> or <paramref name="testMethodName"/> is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the specified test method does not exist.</exception>
        public static void RunTest(this ValidationHelperTests testInstance, string testMethodName)
        {
            ArgumentNullException.ThrowIfNull(testInstance);
            ArgumentException.ThrowIfNullOrWhiteSpace(testMethodName);

            var method = typeof(ValidationHelperTests)
                .GetMethod(testMethodName, BindingFlags.Public | BindingFlags.Instance);

            if (method is null)
                throw new InvalidOperationException($"Test method '{testMethodName}' not found in {nameof(ValidationHelperTests)}.");

            method.Invoke(testInstance, null);
        }

        /// <summary>
        /// Runs all test methods in the <see cref="ValidationHelperTests"/> class.
        /// </summary>
        /// <param name="testInstance">The test instance (not used, but required for extension method signature).</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testInstance"/> is null.</exception>
        public static void RunAllTests(this ValidationHelperTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            foreach (var methodName in testInstance.GetTestMethodNames())
            {
                testInstance.RunTest(methodName);
            }
        }
    }
}
