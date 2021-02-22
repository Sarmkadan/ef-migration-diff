// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public static string NormalizePath(this string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        // Replace backslashes with forward slashes
        var normalized = path.Replace('\\', '/');

        // Remove duplicate slashes
        while (normalized.Contains("//", StringComparison.Ordinal))
            normalized = normalized.Replace("//", "/");

        return normalized;
    }

    /// <summary>
    /// Converts a relative path to an absolute path based on the given root directory.
    /// </summary>
    public static string ToAbsolutePath(this string path, string rootDirectory)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (Path.IsPathRooted(path))
            return path;

        return Path.Combine(rootDirectory, path);
    }

    /// <summary>
    /// Converts an absolute path to a relative path from the given base directory.
    /// </summary>
    public static string ToRelativePath(this string path, string basePath)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        var fullPath = Path.GetFullPath(path);
        var fullBasePath = Path.GetFullPath(basePath);

        // Ensure both paths end with separator for correct comparison
        if (!fullBasePath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            fullBasePath += Path.DirectorySeparatorChar;

        if (fullPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(fullBasePath.Length);
        }

        return path;
    }

    /// <summary>
    /// Checks if a path is under a parent directory (recursive check).
    /// </summary>
    public static bool IsUnderDirectory(this string path, string parentDirectory)
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

    /// <summary>
    /// Ensures a directory path ends with a separator.
    /// </summary>
    public static string EnsureTrailingSeparator(this string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            return path + Path.DirectorySeparatorChar;

        return path;
    }

    /// <summary>
    /// Removes trailing path separator if present.
    /// </summary>
    public static string RemoveTrailingSeparator(this string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        while (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
               path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            path = path.Substring(0, path.Length - 1);
        }

        return path;
    }

    /// <summary>
    /// Gets the common parent directory of multiple paths.
    /// </summary>
    public static string GetCommonDirectory(this IEnumerable<string> paths)
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

    /// <summary>
    /// Safely combines path segments, handling null or empty segments.
    /// </summary>
    public static string CombinePathSafely(params string[] segments)
    {
        var validSegments = segments.Where(s => !string.IsNullOrEmpty(s)).ToArray();
        return validSegments.Length > 0 ? Path.Combine(validSegments) : string.Empty;
    }

    /// <summary>
    /// Gets a safe filename from a path, replacing invalid characters.
    /// </summary>
    public static string GetSafeFileName(this string filename)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(filename.Where(c => !invalidChars.Contains(c)).ToArray());
    }

    /// <summary>
    /// Checks if a path appears to be a directory path (ends with separator or has no extension).
    /// </summary>
    public static bool LooksLikeDirectory(this string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var endsWithSeparator = path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                                path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal);

        var hasNoExtension = !Path.HasExtension(path);

        return endsWithSeparator || hasNoExtension;
    }
}
