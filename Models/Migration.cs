#nullable enable
using System.Text.Json.Serialization;

namespace EfMigrationDiff.Models;

/// <summary>
/// Represents an Entity Framework migration with metadata and content.
/// </summary>
public class Migration
{
    /// <summary>
    /// Gets or sets the unique identifier of the migration.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the migration.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the migration timestamp in EF format (<c>yyyyMMddHHmmss</c>).
    /// </summary>
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC date and time at which the migration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the name of the DbContext type that owns the migration.
    /// </summary>
    public string DbContextName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw source text of the migration.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw source text of the migration's metadata content.
    /// </summary>
    public string MetadataContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the migration.
    /// </summary>
    public MigrationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets a free-text description of the migration.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the position of the migration in its ordered sequence.
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Gets or sets the schema changes extracted from the migration. Not serialized to JSON.
    /// </summary>
    [JsonIgnore]
    public List<SchemaChange> SchemaChanges { get; set; } = [];

    /// <summary>
    /// Gets or sets the conflicts detected for the migration. Not serialized to JSON.
    /// </summary>
    [JsonIgnore]
    public List<ConflictInfo> DetectedConflicts { get; set; } = [];

    /// <summary>
    /// Initializes a new, empty migration with default property values.
    /// </summary>
    public Migration()
    {
    }

    /// <summary>
    /// Initializes a new migration with the given identifier, name and DbContext name.
    /// Sets <see cref="Timestamp"/> to the current UTC time, <see cref="CreatedAt"/> to the current UTC date and time,
    /// and <see cref="Status"/> to <see cref="MigrationStatus.Pending"/>.
    /// </summary>
    /// <param name="id">The unique identifier of the migration.</param>
    /// <param name="name">The name of the migration.</param>
    /// <param name="dbContextName">The name of the DbContext type that owns the migration.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public Migration(string id, string name, string dbContextName)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(dbContextName);

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
               !string.IsNullOrWhiteSpace(DbContextName);
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

    /// <summary>
    /// Returns a string in the form <c>{Name} ({Id}) - {Status}</c>.
    /// </summary>
    /// <returns>A short textual representation of the migration.</returns>
    public override string ToString()
    {
        return $"{Name} ({Id}) - {Status}";
    }
}
