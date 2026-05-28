#nullable enable
namespace EfMigrationDiff.Models;

/// <summary>
/// Represents a physical migration file in the filesystem.
/// </summary>
public class MigrationFile
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DirectoryPath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string DbContextName { get; set; } = string.Empty;
    public string MigrationId { get; set; } = string.Empty;
    public bool IsDesigner { get; set; }

    public MigrationFile()
    {
    }

    public MigrationFile(string filePath, string dbContextName)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        DirectoryPath = Path.GetDirectoryName(filePath) ?? string.Empty;
        DbContextName = dbContextName;
        IsDesigner = FileName.EndsWith(".Designer.cs");

        if (File.Exists(filePath))
        {
            FileSize = new FileInfo(filePath).Length;
            LastModified = File.GetLastWriteTimeUtc(filePath);
        }
    }

    /// <summary>
    /// Loads the file content from disk.
    /// </summary>
    public async Task LoadContentAsync()
    {
        if (File.Exists(FilePath))
        {
            Content = await File.ReadAllTextAsync(FilePath);
            CalculateHash();
        }
    }

    /// <summary>
    /// Calculates SHA256 hash of the file content.
    /// </summary>
    public void CalculateHash()
    {
        if (!string.IsNullOrEmpty(Content))
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Content));
            Hash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    /// <summary>
    /// Extracts the migration ID from the filename.
    /// </summary>
    public string ExtractMigrationId()
    {
        var baseName = Path.GetFileNameWithoutExtension(FileName);
        if (IsDesigner)
        {
            baseName = baseName.Replace(".Designer", "");
        }

        var parts = baseName.Split('_');
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    /// <summary>
    /// Validates the migration file has the required properties set.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(FileName) &&
               (FileName.EndsWith(".cs") || FileName.EndsWith(".Designer.cs")) &&
               !string.IsNullOrEmpty(DbContextName);
    }

    /// <summary>
    /// Compares this file with another based on content hash.
    /// </summary>
    public bool HasSameContent(MigrationFile other)
    {
        if (string.IsNullOrEmpty(Hash) || string.IsNullOrEmpty(other.Hash))
        {
            return Content == other.Content;
        }

        return Hash == other.Hash;
    }

    /// <summary>
    /// Gets the relative path for display purposes.
    /// </summary>
    public string GetDisplayPath(string? basePath = null)
    {
        if (string.IsNullOrEmpty(basePath))
            return FilePath;

        if (FilePath.StartsWith(basePath))
            return FilePath[basePath.Length..].TrimStart(Path.DirectorySeparatorChar);

        return FilePath;
    }

    public override string ToString()
    {
        return $"{FileName} ({FileSize} bytes)";
    }
}
