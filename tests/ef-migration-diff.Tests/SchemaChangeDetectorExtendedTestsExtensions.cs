using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Extension methods that make it easier to work with <see cref="SchemaChangeDetectorExtendedTests"/>
    /// in a programmatic way (e.g., when re‑using the test logic outside of a test runner).
    /// </summary>
    public static class SchemaChangeDetectorExtendedTestsExtensions
    {
        /// <summary>
        /// Executes all public methods whose name starts with <c>DetectChanges_</c> on the supplied test instance.
        /// </summary>
        /// <param name="testInstance">The test class instance.</param>
        public static void RunAllDetectChangesTests(this SchemaChangeDetectorExtendedTests testInstance)
        {
            if (testInstance == null) throw new ArgumentNullException(nameof(testInstance));

            var methods = GetTestMethods(testInstance.GetType(), "DetectChanges_");
            foreach (var method in methods)
            {
                method.Invoke(testInstance, null);
            }
        }

        /// <summary>
        /// Executes all public methods whose name starts with <c>IsMigrationSafe_</c> on the supplied test instance.
        /// </summary>
        /// <param name="testInstance">The test class instance.</param>
        public static void RunAllIsMigrationSafeTests(this SchemaChangeDetectorExtendedTests testInstance)
        {
            if (testInstance == null) throw new ArgumentNullException(nameof(testInstance));

            var methods = GetTestMethods(testInstance.GetType(), "IsMigrationSafe_");
            foreach (var method in methods)
            {
                method.Invoke(testInstance, null);
            }
        }

        /// <summary>
        /// Returns the names of all public test methods defined on <see cref="SchemaChangeDetectorExtendedTests"/>.
        /// </summary>
        /// <param name="testInstance">The test class instance.</param>
        /// <returns>A list of method names.</returns>
        public static List<string> GetAllTestMethodNames(this SchemaChangeDetectorExtendedTests testInstance)
        {
            if (testInstance == null) throw new ArgumentNullException(nameof(testInstance));

            return testInstance.GetType()
                               .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                               .Where(m => m.GetParameters().Length == 0 && m.ReturnType == typeof(void))
                               .Select(m => m.Name)
                               .ToList();
        }

        /// <summary>
        /// Executes every test method on the instance and returns <c>true</c> if none of them threw an exception.
        /// </summary>
        /// <param name="testInstance">The test class instance.</param>
        /// <returns><c>true</c> when all tests pass; otherwise <c>false</c>.</returns>
        public static bool AssertAllTestsPass(this SchemaChangeDetectorExtendedTests testInstance)
        {
            if (testInstance == null) throw new ArgumentNullException(nameof(testInstance));

            var methods = testInstance.GetType()
                                      .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                      .Where(m => m.GetParameters().Length == 0 && m.ReturnType == typeof(void));

            foreach (var method in methods)
            {
                try
                {
                    method.Invoke(testInstance, null);
                }
                catch
                {
                    // If any test throws, we consider the whole suite failed.
                    return false;
                }
            }

            return true;
        }

        // Helper that filters methods by a given prefix.
        private static IEnumerable<MethodInfo> GetTestMethods(Type type, string prefix)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                       .Where(m => m.Name.StartsWith(prefix, StringComparison.Ordinal) &&
                                   m.GetParameters().Length == 0 &&
                                   m.ReturnType == typeof(void));
        }
    }
}
