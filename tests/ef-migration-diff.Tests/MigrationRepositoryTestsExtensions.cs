using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Extension methods that make it easier to work with <see cref="MigrationRepositoryTests"/>.
    /// </summary>
    public static class MigrationRepositoryTestsExtensions
    {
        /// <summary>
        /// Executes all public, parameter‑less test methods on the supplied <paramref name="tests"/>
        /// instance whose names start with the specified <paramref name="prefix"/>.
        /// </summary>
        /// <param name="tests">The test class instance.</param>
        /// <param name="prefix">The method name prefix to filter on. Use an empty string to run every test.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="prefix"/> is <c>null</c>.</exception>
        public static void RunAllTestsStartingWith(this MigrationRepositoryTests tests, string prefix)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentException.ThrowIfNullOrEmpty(prefix);

            var methods = GetTestMethods(tests.GetType(), prefix);
            foreach (var method in methods)
            {
                // Let any exception bubble up – the test framework will treat it as a failure.
                method.Invoke(tests, null);
            }
        }

        /// <summary>
        /// Runs all test methods on <paramref name="tests"/> and returns <c>true</c> if none threw an exception.
        /// </summary>
        /// <param name="tests">The test class instance.</param>
        /// <returns><c>true</c> when every test method completed without throwing; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <c>null</c>.</exception>
        public static bool AssertAllTestsPass(this MigrationRepositoryTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            try
            {
                tests.RunAllTestsStartingWith(string.Empty);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves the names of all public, parameter‑less test methods on <paramref name="tests"/>
        /// that start with the supplied <paramref name="prefix"/>.
        /// </summary>
        /// <param name="tests">The test class instance.</param>
        /// <param name="prefix">The method name prefix to filter on. Use an empty string to include every test.</param>
        /// <returns>An <see cref="IEnumerable{String}"/> containing the matching method names.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="prefix"/> is <c>null</c>.</exception>
        public static IEnumerable<string> GetAllTestMethodNames(this MigrationRepositoryTests tests, string prefix)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentException.ThrowIfNullOrEmpty(prefix);

            return GetTestMethods(tests.GetType(), prefix)
                .Select(m => m.Name);
        }

        // Helper that isolates the reflection logic used by the public extensions.
        private static IEnumerable<MethodInfo> GetTestMethods(Type testType, string prefix) =>
            testType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.Name.StartsWith(prefix, StringComparison.Ordinal)
                    && m.GetParameters().Length == 0
                    && m.ReturnType == typeof(void));
    }
}
