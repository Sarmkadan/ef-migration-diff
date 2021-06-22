using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Provides extension methods for <see cref="ConflictDetectionServiceTests"/> to facilitate test execution and analysis.
    /// </summary>
    public static class ConflictDetectionServiceTestsExtensions
    {
        /// <summary>
        /// Executes all test methods in the <see cref="ConflictDetectionServiceTests"/> instance.
        /// </summary>
        /// <param name="testInstance">The test instance to execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testInstance"/> is null.</exception>
        public static void RunAllConflictDetectionTests(this ConflictDetectionServiceTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            var testMethods = GetTestMethods(testInstance.GetType(), "DetectConflicts_");
            foreach (var method in testMethods)
            {
                method.Invoke(testInstance, null);
            }
        }

        /// <summary>
        /// Retrieves the names of all test methods in the <see cref="ConflictDetectionServiceTests"/> instance.
        /// </summary>
        /// <param name="testInstance">The test instance to analyze.</param>
        /// <returns>An <see cref="IReadOnlyList{T}"/> containing the names of all test methods.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testInstance"/> is null.</exception>
        public static IReadOnlyList<string> GetAllConflictTestNames(this ConflictDetectionServiceTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            return GetTestMethods(testInstance.GetType(), "DetectConflicts_")
                .Select(m => m.Name)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Verifies that all test methods in the <see cref="ConflictDetectionServiceTests"/> instance pass without exceptions.
        /// </summary>
        /// <param name="testInstance">The test instance to validate.</param>
        /// <returns>True if all tests pass; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testInstance"/> is null.</exception>
        public static bool AssertAllConflictTestsPass(this ConflictDetectionServiceTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

            try
            {
                RunAllConflictDetectionTests(testInstance);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Gets all test methods from the specified type that match the naming pattern.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="prefix">The prefix used to identify test methods.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="MethodInfo"/> representing the test methods.</returns>
        private static IEnumerable<MethodInfo> GetTestMethods(Type type, string prefix)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith(prefix, StringComparison.Ordinal) && m.ReturnType == typeof(void));
        }
    }
}
