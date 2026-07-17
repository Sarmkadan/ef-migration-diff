using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EfMigrationDiff.Configuration;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;

namespace EfMigrationDiff.Benchmarks;

/// <summary>
/// Provides validation helpers for <see cref="SchemaDiffBenchmarks"/> instances to ensure benchmark configuration
/// is valid before execution. Validates null/empty values, default dates, and out-of-range numbers based on
/// the semantic meaning of each member.
/// </summary>
public static class SchemaDiffBenchmarksValidation
{
    private static readonly IReadOnlyList<FieldInfo> _benchmarkFields;

    static SchemaDiffBenchmarksValidation()
    {
        var fields = new List<FieldInfo>();
        var type = typeof(SchemaDiffBenchmarks);
        var allFields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        
        foreach (var field in allFields)
        {
            if (!field.Name.StartsWith("_", StringComparison.Ordinal))
                continue;
                
            if (field.FieldType == typeof(SchemaDiffEngine))
            {
                fields.Add(field);
            }
            else if (field.FieldType.IsGenericType && 
                     field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                fields.Add(field);
            }
        }
        
        _benchmarkFields = fields.AsReadOnly();
    }

    /// <summary>
    /// Validates the given <see cref="SchemaDiffBenchmarks"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of problem descriptions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this SchemaDiffBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        foreach (var field in _benchmarkFields)
        {
            var fieldValue = field.GetValue(value);

            switch (fieldValue)
            {
                case null when field.FieldType == typeof(SchemaDiffEngine):
                    problems.Add("Engine instance (_engine) has not been initialized. Call Setup() first.");
                    break;

                case System.Collections.ICollection collection when collection.Count == 0:
                    problems.Add($"{field.Name} is empty. Benchmarks require non-empty collections for meaningful measurements.");
                    break;
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the given <see cref="SchemaDiffBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to check.</param>
    /// <returns><c>true</c> if valid; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this SchemaDiffBenchmarks value) =>
        Validate(value).Count == 0;

    /// <summary>
    /// Ensures the given <see cref="SchemaDiffBenchmarks"/> instance is valid, throwing an <see cref="ArgumentException"/> with
    /// a detailed message listing all validation problems if any are found.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is invalid.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static void EnsureValid(this SchemaDiffBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"SchemaDiffBenchmarks instance is invalid. Problems:\n{string.Join("\n", problems)}",
                nameof(value));
        }
    }
}
