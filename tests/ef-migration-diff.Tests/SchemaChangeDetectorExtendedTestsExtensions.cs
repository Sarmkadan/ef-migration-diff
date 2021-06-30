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
        /// <exception cref="ArgumentNullException"><paramref name="testInstance"/> is <see langword="null"/>.</exception>
        public static void RunAllDetectChangesTests(this SchemaChangeDetectorExtendedTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

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
        /// <exception cref="ArgumentNullException"><paramref name="testInstance"/> is <see langword="null"/>.</exception>
        public static void RunAllIsMigrationSafeTests(this SchemaChangeDetectorExtendedTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

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
        /// <exception cref="ArgumentNullException"><paramref name="testInstance"/> is <see langword="null"/>.</exception>
        public static List<string> GetAllTestMethodNames(this SchemaChangeDetectorExtendedTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

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
        /// <exception cref="ArgumentNullException"><paramref name="testInstance"/> is <see langword="null"/>.</exception>
        public static bool AssertAllTestsPass(this SchemaChangeDetectorExtendedTests testInstance)
        {
            ArgumentNullException.ThrowIfNull(testInstance);

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

        /// <summary>
        /// Filters public instance methods by name prefix and validates method signature.
        /// </summary>
        /// <param name="type">The type to search for methods.</param>
        /// <param name="prefix">The method name prefix to filter by.</param>
        /// <returns>An enumerable of matching method infos.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="prefix"/> is <see langword="null"/>.</exception>
        private static IEnumerable<MethodInfo> GetTestMethods(Type type, string prefix)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(prefix);

            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.Name.StartsWith(prefix, StringComparison.Ordinal) &&
                    m.GetParameters().Length == 0 &&
                    m.ReturnType == typeof(void));
        }
    }
}