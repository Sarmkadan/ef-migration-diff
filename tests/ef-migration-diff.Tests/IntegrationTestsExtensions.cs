using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Provides extension methods for <see cref="IntegrationTests"/> to simplify integration test execution,
    /// including running tests by name prefix, asserting no exceptions, and parallel test execution.
    /// </summary>
    public static class IntegrationTestsExtensions
    {
        /// <summary>
        /// Executes all test methods in the <see cref="IntegrationTests"/> instance that start with the specified prefix.
        /// </summary>
        /// <param name="tests">The integration tests instance.</param>
        /// <param name="prefix">The prefix to match against method names. Must not be null or empty.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> or <paramref name="prefix"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="prefix"/> is empty or consists only of whitespace.</exception>
        public static void RunAllTestsStartingWith(this IntegrationTests tests, string prefix)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

            var methods = tests.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith(prefix, StringComparison.Ordinal))
                .ToList();

            foreach (var method in methods)
            {
                method.Invoke(tests, null);
            }
        }

        /// <summary>
        /// Executes the provided test method and asserts that it completes without throwing exceptions.
        /// </summary>
        /// <param name="tests">The integration tests instance.</param>
        /// <param name="testMethod">The test method to execute. Must not be null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> or <paramref name="testMethod"/> is null.</exception>
        public static void RunAndAssertNoExceptions(this IntegrationTests tests, Action testMethod)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentNullException.ThrowIfNull(testMethod);

            try
            {
                testMethod();
            }
            catch (Exception ex)
            {
                throw new Exception($"Test failed with exception: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executes multiple test methods in parallel using Task.Run.
        /// </summary>
        /// <param name="tests">The integration tests instance.</param>
        /// <param name="testMethods">The test methods to execute. Must not be null or contain null elements.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> or <paramref name="testMethods"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="testMethods"/> contains a null element.</exception>
        public static void RunTestsInParallel(this IntegrationTests tests, params Action[] testMethods)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentNullException.ThrowIfNull(testMethods);

            var tasks = testMethods
                .Where(testMethod => testMethod != null)
                .Select(testMethod => Task.Run(() => testMethod()))
                .ToList();

            if (tasks.Count > 0)
            {
                Task.WaitAll(tasks.ToArray());
            }
        }
    }
}