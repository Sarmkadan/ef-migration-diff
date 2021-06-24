#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace EfMigrationDiff.Middleware;

/// <summary>
/// Provides validation helpers for <see cref="RequestLoggingMiddleware"/> instances.
/// Validates constructor parameters and ensures middleware is properly configured for use.
/// </summary>
public static class RequestLoggingMiddlewareValidation
{
    /// <summary>
    /// Validates the specified <see cref="RequestLoggingMiddleware"/> instance.
    /// </summary>
    /// <param name="value">The middleware instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this RequestLoggingMiddleware value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate the logger field (cannot be null due to constructor validation)
        // The logger is validated during construction, so we just check it's not null
        if (value.GetType().GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(value) is null)
        {
            problems.Add("Logger instance is null.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="RequestLoggingMiddleware"/> instance is valid.
    /// </summary>
    /// <param name="value">The middleware instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this RequestLoggingMiddleware value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="RequestLoggingMiddleware"/> instance is valid.
    /// </summary>
    /// <param name="value">The middleware instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this RequestLoggingMiddleware value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"RequestLoggingMiddleware is not valid. Problems: {string.Join(" ", problems)}",
                nameof(value));
        }
    }
}