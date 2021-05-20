using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EfMigrationDiff.Tests
{
    public static class IntegrationTestsExtensions
    {
        /// <summary>
        /// Executes all test methods in the IntegrationTests instance that start with the specified prefix.
        /// </summary>
        public static void RunAllTestsStartingWith(this IntegrationTests tests, string prefix)
        {
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
        public static void RunAndAssertNoExceptions(this IntegrationTests tests, Action testMethod)
        {
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
        public static void RunTestsInParallel(this IntegrationTests tests, params Action[] testMethods)
        {
            var tasks = testMethods.Select(testMethod => Task.Run(() =>
            {
                try
                {
                    testMethod();
                }
                catch (Exception ex)
                {
                    throw;
                }
            })).ToList();

            Task.WaitAll(tasks.ToArray());
        }
    }
}
