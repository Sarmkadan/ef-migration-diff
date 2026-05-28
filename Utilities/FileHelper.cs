#nullable enable
using EfMigrationDiff.Exceptions;

namespace EfMigrationDiff.Utilities;

/// <summary>
/// Helper class for file operations and validation.
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// Safely reads a file with error handling.
    /// </summary>
    public static string? ReadFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > Constants.FileSize.MaxMigrationFileSize)
                throw new FileOperationException(filePath, "read - file too large");

            return File.ReadAllText(filePath);
        }
        catch (Exception ex) when (!(ex is FileOperationException))
        {
            throw new FileOperationException($"Error reading file: {filePath}", "read", ex);
        }
    }

    /// <summary>
    /// Safely writes content to a file with error handling.
    /// </summary>
    public static void WriteFile(string filePath, string content)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, content);
        }
        catch (Exception ex)
        {
            throw new FileOperationException($"Error writing to file: {filePath}", "write", ex);
        }
    }

    /// <summary>
    /// Gets all migration files from a directory.
    /// </summary>
    public static List<string> GetMigrationFiles(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
                return [];

            return Directory.GetFiles(directoryPath, Constants.Paths.MigrationExtension)
                           .Where(f => !f.EndsWith(Constants.Paths.MigrationDesignerSuffix + Constants.Paths.MigrationExtension))
                           .ToList();
        }
        catch (Exception ex)
        {
            throw new FileOperationException($"Error listing migration files: {directoryPath}", "list", ex);
        }
    }

    /// <summary>
    /// Validates that a path is a valid migration directory.
    /// </summary>
    public static bool IsValidMigrationDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return false;

        try
        {
            var files = Directory.GetFiles(directoryPath, Constants.Paths.MigrationExtension);
            return files.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the size of a file in bytes.
    /// </summary>
    public static long GetFileSize(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return 0;

            return new FileInfo(filePath).Length;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets a human-readable file size string.
    /// </summary>
    public static string GetHumanReadableFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Creates a directory if it doesn't exist.
    /// </summary>
    public static void EnsureDirectoryExists(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        catch (Exception ex)
        {
            throw new FileOperationException($"Error creating directory: {directoryPath}", "create", ex);
        }
    }

    /// <summary>
    /// Gets all subdirectories matching a pattern.
    /// </summary>
    public static List<string> GetSubdirectories(string parentPath, string pattern = "*")
    {
        try
        {
            if (!Directory.Exists(parentPath))
                return [];

            return Directory.GetDirectories(parentPath, pattern).ToList();
        }
        catch (Exception ex)
        {
            throw new FileOperationException($"Error listing subdirectories: {parentPath}", "list", ex);
        }
    }

    /// <summary>
    /// Deletes a file with error handling.
    /// </summary>
    public static bool DeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            throw new FileOperationException($"Error deleting file: {filePath}", "delete", ex);
        }
    }

    /// <summary>
    /// Copies a file with error handling.
    /// </summary>
    public static void CopyFile(string sourcePath, string destinationPath, bool overwrite = true)
    {
        try
        {
            if (!File.Exists(sourcePath))
                throw new FileOperationException(sourcePath, "copy - source not found");

            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(sourcePath, destinationPath, overwrite);
        }
        catch (Exception ex) when (!(ex is FileOperationException))
        {
            throw new FileOperationException($"Error copying file: {sourcePath} to {destinationPath}", "copy", ex);
        }
    }

    /// <summary>
    /// Gets the last modified time of a file.
    /// </summary>
    public static DateTime GetLastModifiedTime(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return DateTime.MinValue;

            return File.GetLastWriteTimeUtc(filePath);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Combines path segments into a full path.
    /// </summary>
    public static string CombinePath(params string[] segments)
    {
        return Path.Combine(segments);
    }

    /// <summary>
    /// Gets the relative path from base to target.
    /// </summary>
    public static string GetRelativePath(string basePath, string targetPath)
    {
        try
        {
            return Path.GetRelativePath(basePath, targetPath);
        }
        catch
        {
            return targetPath;
        }
    }
}
