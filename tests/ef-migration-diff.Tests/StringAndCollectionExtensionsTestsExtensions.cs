using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EfMigrationDiff.Tests
{
    public static class StringAndCollectionExtensionsTestsExtensions
    {
        public static void RunAllStringConversionTests(this StringAndCollectionExtensionsTests testInstance)
        {
            var stringMethods = typeof(StringAndCollectionExtensionsTests)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith("ToPascalCase") || m.Name.StartsWith("ToSnakeCase") || m.Name.StartsWith("Truncate"))
                .ToList();

            foreach (var method in stringMethods)
            {
                method.Invoke(testInstance, null);
            }
        }

        public static void RunAllCollectionBatchingTests(this StringAndCollectionExtensionsTests testInstance)
        {
            var batchMethods = typeof(StringAndCollectionExtensionsTests)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith("Batch"))
                .ToList();

            foreach (var method in batchMethods)
            {
                method.Invoke(testInstance, null);
            }
        }

        public static List<string> GetAllTestNames(this StringAndCollectionExtensionsTests testInstance)
        {
            return typeof(StringAndCollectionExtensionsTests)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(m => m.Name)
                .ToList();
        }

        public static bool AssertAllTestsPass(this StringAndCollectionExtensionsTests testInstance)
        {
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
                    Console.WriteLine($"Test failed: {method.Name} - {ex.Message}");
                    return false;
                }
            }
            return true;
        }
    }
}
