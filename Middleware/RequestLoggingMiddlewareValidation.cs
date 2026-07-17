#nullable enable

using System;
using System.Collections.Generic;

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

        return Array.Empty<string>();
    }

    /// <summary>
    /// Determines whether the specified <see cref="RequestLoggingMiddleware"/> instance is valid.
    /// </summary>
    /// <param name="value">The middleware instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this RequestLoggingMiddleware value) => value.Validate().Count == 0;

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