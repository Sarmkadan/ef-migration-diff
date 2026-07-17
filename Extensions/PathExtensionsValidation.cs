#nullable enable

namespace EfMigrationDiff.Extensions;

/// <summary>
/// Provides validation helpers for path operations from <see cref="PathExtensions"/>.
/// Validates path strings for correctness, safety, and common issues.
/// </summary>
public static class PathExtensionsValidation
{
    /// <summary>
    /// Validates a path string for common issues.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>A list of validation problems; empty if the path is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if path is null.</exception>
    public static IReadOnlyList<string> Validate(this string? path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(path))
        {
            problems.Add("Path is null, empty, or whitespace.");
        }
        else
        {
            // Check for invalid path characters
            var invalidChars = Path.GetInvalidPathChars()
                .Concat(Path.GetInvalidFileNameChars())
                .ToHashSet();

            if (path.IndexOfAny(invalidChars.ToArray()) >= 0)
            {
                problems.Add("Path contains invalid characters.");
            }

            // Check for relative path issues
            if (path.StartsWith(".", StringComparison.Ordinal) &&
                !path.StartsWith("./", StringComparison.Ordinal) &&
                !path.StartsWith("../", StringComparison.Ordinal))
            {
                problems.Add("Path appears to be a relative path starting with '.'. Consider using './' prefix.");
            }

            // Check for path length (Windows MAX_PATH is 260, but we allow longer for modern systems)
            if (path.Length > 260)
            {
                problems.Add("Path is longer than 260 characters, which may cause issues on some systems.");
            }

            // Check for consecutive slashes
            if (path.Contains("//", StringComparison.Ordinal) || path.Contains("\\\\", StringComparison.Ordinal))
            {
                problems.Add("Path contains consecutive slashes.");
            }

            // Check for trailing spaces (can cause issues on some systems)
            if (path.EndsWith(" ", StringComparison.Ordinal) || path.EndsWith(". ", StringComparison.Ordinal))
            {
                problems.Add("Path ends with whitespace, which may cause issues.");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a path string is valid.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if path is null.</exception>
    public static bool IsValid(this string? path) => Validate(path).Count == 0;

    /// <summary>
    /// Ensures that a path string is valid, throwing an exception if it is not.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <exception cref="ArgumentException">Thrown if the path is invalid.</exception>
    /// <exception cref="ArgumentNullException">Thrown if path is null.</exception>
    public static void EnsureValid(this string? path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var problems = Validate(path);

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Path is invalid: {string.Join(" ", problems)}",
                nameof(path));
        }
    }
}