// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json.Serialization;

namespace EfMigrationDiff.Models;

/// <summary>
/// Represents an Entity Framework migration with metadata and content.
/// </summary>
public class Migration
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string DbContextName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string MetadataContent { get; set; } = string.Empty;
    public MigrationStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Sequence { get; set; }

    [JsonIgnore]
    public List<SchemaChange> SchemaChanges { get; set; } = [];

    [JsonIgnore]
    public List<ConflictInfo> DetectedConflicts { get; set; } = [];

    public Migration()
    {
    }

    public Migration(string id, string name, string dbContextName)
    {
        Id = id;
        Name = name;
        DbContextName = dbContextName;
        Timestamp = GenerateTimestamp();
        CreatedAt = DateTime.UtcNow;
        Status = MigrationStatus.Pending;
    }

    /// <summary>
    /// Validates the migration has minimum required properties.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Id) &&
               !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(DbContextName) &&
               !string.IsNullOrWhiteSpace(Content);
    }

    /// <summary>
    /// Generates a unique migration timestamp in EF format.
    /// </summary>
    public static string GenerateTimestamp()
    {
        return DateTime.UtcNow.ToString("yyyyMMddHHmmss");
    }

    /// <summary>
    /// Creates a copy of this migration with new ID.
    /// </summary>
    public Migration Clone()
    {
        return new Migration
        {
            Id = Guid.NewGuid().ToString(),
            Name = Name,
            Timestamp = Timestamp,
            CreatedAt = CreatedAt,
            DbContextName = DbContextName,
            Content = Content,
            MetadataContent = MetadataContent,
            Status = Status,
            Description = Description,
            Sequence = Sequence,
            SchemaChanges = new List<SchemaChange>(SchemaChanges),
            DetectedConflicts = new List<ConflictInfo>(DetectedConflicts)
        };
    }

    /// <summary>
    /// Gets the size of the migration content in bytes.
    /// </summary>
    public int GetContentSize()
    {
        return System.Text.Encoding.UTF8.GetByteCount(Content);
    }

    /// <summary>
    /// Counts the number of SQL statements in the migration.
    /// </summary>
    public int CountStatements()
    {
        return Content.Split(";", StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public override string ToString()
    {
        return $"{Name} ({Id}) - {Status}";
    }
}
