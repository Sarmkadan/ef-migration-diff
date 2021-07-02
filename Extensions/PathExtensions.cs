#nullable enable
namespace EfMigrationDiff.Extensions;

/// <summary>
/// Extension methods for file and directory path operations.
/// Provides cross-platform path handling, normalization, and manipulation utilities.
/// </summary>
public static class PathExtensions
{
    /// <summary>
    /// Normalizes a path to use forward slashes and removes redundant separators.
    /// Works cross-platform (Windows and Unix paths).
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path with forward slashes and no redundant separators.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    public static string NormalizePath(this string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        // Replace backslashes with forward slashes
        var normalized = path.Replace('\\', '/');

        // Remove duplicate slashes using StringBuilder for better performance
        var sb = new System.Text.StringBuilder(normalized);
        int i = 0;
        while (i < sb.Length - 1)
        {
            if (sb[i] == '/' && sb[i + 1] == '/')
            {
                sb.Remove(i, 1);
            }
            else
            {
                i++;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts a relative path to an absolute path based on the given root directory.
    /// </summary>
    /// <param name="path">The path to convert to absolute.</param>
    /// <param name="rootDirectory">The root directory to use as base.</param>
    /// <returns>The absolute path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="rootDirectory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> or <paramref name="rootDirectory"/> is empty.</exception>
    public static string ToAbsolutePath(this string path, string rootDirectory)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrEmpty(rootDirectory);

        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (Path.IsPathRooted(path))
            return path;

        return Path.Combine(rootDirectory, path);
    }

    /// <summary>
    /// Converts an absolute path to a relative path from the given base directory.
    /// </summary>
    /// <param name="path">The absolute path to convert.</param>
    /// <param name="basePath">The base directory path.</param>
    /// <returns>The relative path from basePath, or the original path if conversion fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="basePath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> or <paramref name="basePath"/> is empty.</exception>
    public static string ToRelativePath(this string path, string basePath)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrEmpty(basePath);

        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        try
        {
            var fullPath = Path.GetFullPath(path);
            var fullBasePath = Path.GetFullPath(basePath);

            // Ensure both paths end with separator for correct comparison
            if (!fullBasePath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                fullBasePath += Path.DirectorySeparatorChar;

            if (fullPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.Substring(fullBasePath.Length);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or IOException)
        {
            // Path operations can throw various exceptions for invalid paths
            // Return original path as fallback
        }

        return path;
    }

    /// <summary>
    /// Checks if a path is under a parent directory (recursive check).
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="parentDirectory">The parent directory path to check against.</param>
    /// <returns>True if path is under parentDirectory; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="parentDirectory"/> is null.</exception>
    public static bool IsUnderDirectory(this string path, string parentDirectory)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(parentDirectory);

        try
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(parentDirectory))
                return false;

            var fullPath = Path.GetFullPath(path);
            var fullParentPath = Path.GetFullPath(parentDirectory);

            // Ensure parent path ends with separator
            if (!fullParentPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                fullParentPath += Path.DirectorySeparatorChar;

            return fullPath.StartsWith(fullParentPath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or IOException)
        {
            // Path operations can throw various exceptions for invalid paths
            return false;
        }
    }

    /// <summary>
    /// Ensures a directory path ends with a separator.
    /// </summary>
    /// <param name="path">The path to ensure has trailing separator.</param>
    /// <returns>The path with trailing separator added if needed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    public static string EnsureTrailingSeparator(this string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (string.IsNullOrEmpty(path))
            return path;

        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            return path + Path.DirectorySeparatorChar;

        return path;
    }

    /// <summary>
    /// Removes trailing path separator if present.
    /// </summary>
    /// <param name="path">The path to remove trailing separator from.</param>
    /// <returns>The path with trailing separators removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    public static string RemoveTrailingSeparator(this string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (string.IsNullOrEmpty(path))
            return path;

        var endsWithDirSeparator = Path.DirectorySeparatorChar.ToString();
        var endsWithAltSeparator = Path.AltDirectorySeparatorChar.ToString();

        while (path.EndsWith(endsWithDirSeparator, StringComparison.Ordinal) ||
               path.EndsWith(endsWithAltSeparator, StringComparison.Ordinal))
        {
            path = path.Substring(0, path.Length - 1);
        }

        return path;
    }

    /// <summary>
    /// Gets the common parent directory of multiple paths.
    /// </summary>
    /// <param name="paths">The collection of paths to find common directory for.</param>
    /// <returns>The common parent directory path, or empty string if no common directory exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="paths"/> is null.</exception>
    public static string GetCommonDirectory(this IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        try
        {
            var pathList = paths.Select(p => Path.GetFullPath(p)).ToList();

            if (pathList.Count == 0)
                return string.Empty;

            if (pathList.Count == 1)
                return Path.GetDirectoryName(pathList[0]) ?? string.Empty;

            var commonPath = pathList[0];
            foreach (var path in pathList.Skip(1))
            {
                while (!path.StartsWith(commonPath, StringComparison.OrdinalIgnoreCase) && commonPath != Path.GetDirectoryName(commonPath))
                {
                    commonPath = Path.GetDirectoryName(commonPath) ?? string.Empty;
                }
            }

            return commonPath;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or IOException)
        {
            // Path operations can throw various exceptions for invalid paths
            return string.Empty;
        }
    }

    /// <summary>
    /// Safely combines path segments, handling null or empty segments.
    /// </summary>
    /// <param name="segments">The path segments to combine.</param>
    /// <returns>The combined path, or empty string if no valid segments provided.</returns>
    public static string CombinePathSafely(params string[] segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        var validSegments = segments.Where(s => !string.IsNullOrEmpty(s)).ToArray();
        return validSegments.Length > 0 ? Path.Combine(validSegments) : string.Empty;
    }

    /// <summary>
    /// Gets a safe filename from a path, replacing invalid characters.
    /// </summary>
    /// <param name="filename">The filename to sanitize.</param>
    /// <returns>The sanitized filename with invalid characters removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filename"/> is null.</exception>
    public static string GetSafeFileName(this string filename)
    {
        ArgumentNullException.ThrowIfNull(filename);

        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(filename.Where(c => !invalidChars.Contains(c)).ToArray());
    }

    /// <summary>
    /// Checks if a path appears to be a directory path (ends with separator or has no extension).
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path appears to be a directory; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    public static bool LooksLikeDirectory(this string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (string.IsNullOrEmpty(path))
            return false;

        var endsWithSeparator = path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                                path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal);

        var hasNoExtension = !Path.HasExtension(path);

        return endsWithSeparator || hasNoExtension;
    }
}
