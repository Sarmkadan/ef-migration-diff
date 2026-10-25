#nullable enable
namespace EfMigrationDiff.Models;

/// <summary>
/// Represents a physical migration file in the filesystem.
/// </summary>
public class MigrationFile
{
    /// <summary>
    /// Full path to the migration file on disk.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File name including extension (for example <c>20240101120000_InitialCreate.cs</c>).
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Directory that contains the migration file. Empty when the path has no directory part.
    /// </summary>
    public string DirectoryPath { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes. Set by the constructor only when the file exists on disk.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// UTC time the file was last written. Set by the constructor only when the file exists on disk.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Source text of the migration file. Populated by <see cref="LoadContentAsync"/>.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Lowercase hexadecimal SHA256 hash of <see cref="Content"/>. Populated by <see cref="CalculateHash"/>.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Name of the DbContext this migration belongs to.
    /// </summary>
    public string DbContextName { get; set; } = string.Empty;

    /// <summary>
    /// Migration identifier associated with this file. Not set by this class; callers assign it.
    /// </summary>
    public string MigrationId { get; set; } = string.Empty;

    /// <summary>
    /// True when the file is an EF Core designer file (name ends with <c>.Designer.cs</c>).
    /// </summary>
    public bool IsDesigner { get; set; }

    /// <summary>
    /// Initializes an empty instance. Callers must set the properties they need.
    /// </summary>
    public MigrationFile()
    {
    }

    /// <summary>
    /// Initializes an instance from a file path and DbContext name. Derives <see cref="FileName"/>,
    /// <see cref="DirectoryPath"/> and <see cref="IsDesigner"/>, and reads size and last-write time if the file exists.
    /// </summary>
    /// <param name="filePath">Full path to the migration file.</param>
    /// <param name="dbContextName">Name of the DbContext this migration belongs to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> or <paramref name="dbContextName"/> is null.</exception>
    public MigrationFile(string filePath, string dbContextName)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(dbContextName);

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
    /// <returns>True when <see cref="FileName"/> ends with <c>.cs</c> or <c>.Designer.cs</c> and <see cref="DbContextName"/> is set; otherwise false.</returns>
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
        ArgumentNullException.ThrowIfNull(other);

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

    /// <summary>
    /// Returns the file name and size in bytes.
    /// </summary>
    /// <returns>A string in the form <c>FileName (FileSize bytes)</c>.</returns>
    public override string ToString()
    {
        return $"{FileName} ({FileSize} bytes)";
    }
}
