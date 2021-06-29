using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Provides extension methods for <see cref="ReportGenerationServiceTests"/> to run test suites and analyze test metadata.
    /// </summary>
    public static class ReportGenerationServiceTestsExtensions
    {
        /// <summary>
        /// Executes all test methods in <see cref="ReportGenerationServiceTests"/> that start with "Generate".
        /// Throws <see cref="ArgumentNullException"/> if the test instance is null.
        /// </summary>
        /// <param name="testInstance">The test instance to execute methods on.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="testInstance"/> is null.</exception>
        public static void RunAllReportGenerationTests(this ReportGenerationServiceTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            var testMethods = GetTestMethods(testInstance.GetType(), "Generate");
            foreach (var method in testMethods)
            {
                method.Invoke(testInstance, Array.Empty<object>());
            }
        }

        /// <summary>
        /// Returns all test method names in <see cref="ReportGenerationServiceTests"/> that start with "Generate".
        /// Throws <see cref="ArgumentNullException"/> if the test instance is null.
        /// </summary>
        /// <param name="testInstance">The test instance to analyze.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of test method names.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="testInstance"/> is null.</exception>
        public static IEnumerable<string> GetAllReportGenerationTestNames(this ReportGenerationServiceTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            return GetTestMethods(testInstance.GetType(), "Generate")
                .Select(m => m.Name);
        }

        /// <summary>
        /// Asserts that all "Generate" prefixed tests in <see cref="ReportGenerationServiceTests"/> execute without exceptions.
        /// Throws <see cref="ArgumentNullException"/> if the test instance is null.
        /// </summary>
        /// <param name="testInstance">The test instance to validate.</param>
        /// <returns>True if all tests pass without exceptions, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="testInstance"/> is null.</exception>
        public static bool AssertAllReportGenerationTestsPass(this ReportGenerationServiceTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            var testMethods = GetTestMethods(testInstance.GetType(), "Generate");
            foreach (var method in testMethods)
            {
                try
                {
                    method.Invoke(testInstance, Array.Empty<object>());
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Discovers all public test methods in a type that start with the specified prefix.
        /// </summary>
        /// <param name="type">The type to search for test methods.</param>
        /// <param name="prefix">The prefix to filter test methods by.</param>
        /// <returns>An <see cref="IEnumerable{MethodInfo}"/> of matching test methods.</returns>
        private static IEnumerable<MethodInfo> GetTestMethods(Type type, string prefix)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith(prefix, StringComparison.Ordinal));
        }
    }
}
