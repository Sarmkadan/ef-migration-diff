// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace EfMigrationDiff.Models;

/// <summary>
/// Represents a single schema change operation within a migration.
/// </summary>
public class SchemaChange
{
    public string Id { get; set; } = string.Empty;
    public string MigrationId { get; set; } = string.Empty;
    public SqlChangeType ChangeType { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string Sql { get; set; } = string.Empty;
    public Dictionary<string, object?> Metadata { get; set; } = [];
    public int LineNumber { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public SchemaChange()
    {
    }

    public SchemaChange(string migrationId, SqlChangeType changeType, string sql)
    {
        Id = Guid.NewGuid().ToString();
        MigrationId = migrationId;
        ChangeType = changeType;
        Sql = sql;
    }

    /// <summary>
    /// Validates the schema change has required properties.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(MigrationId) &&
               ChangeType != SqlChangeType.Unknown &&
               !string.IsNullOrWhiteSpace(Sql);
    }

    /// <summary>
    /// Gets a human-readable description of the change.
    /// </summary>
    public string GetDescription()
    {
        return ChangeType switch
        {
            SqlChangeType.CreateTable => $"Create table [{TableName}]",
            SqlChangeType.DropTable => $"Drop table [{TableName}]",
            SqlChangeType.AddColumn => $"Add column [{ColumnName}] to [{TableName}]",
            SqlChangeType.DropColumn => $"Drop column [{ColumnName}] from [{TableName}]",
            SqlChangeType.ModifyColumn => $"Modify column [{ColumnName}] in [{TableName}]",
            SqlChangeType.CreateIndex => $"Create index on [{TableName}]",
            SqlChangeType.DropIndex => $"Drop index on [{TableName}]",
            SqlChangeType.AddForeignKey => $"Add foreign key to [{TableName}]",
            SqlChangeType.DropForeignKey => $"Drop foreign key from [{TableName}]",
            SqlChangeType.CreateProcedure => "Create stored procedure",
            SqlChangeType.DropProcedure => "Drop stored procedure",
            SqlChangeType.CreateView => "Create view",
            SqlChangeType.DropView => "Drop view",
            SqlChangeType.AlterTable => $"Alter table [{TableName}]",
            SqlChangeType.Rename => $"Rename [{OldValue}] to [{NewValue}]",
            _ => "Unknown change"
        };
    }

    /// <summary>
    /// Checks if this change affects the same table as another change.
    /// </summary>
    public bool AffectsSameTable(SchemaChange other)
    {
        if (string.IsNullOrEmpty(TableName) || string.IsNullOrEmpty(other.TableName))
            return false;

        return TableName.Equals(other.TableName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if this change conflicts with another change.
    /// </summary>
    public bool ConflictsWith(SchemaChange other)
    {
        // Cannot drop and create the same table in sequence
        if (ChangeType == SqlChangeType.DropTable && other.ChangeType == SqlChangeType.CreateTable &&
            AffectsSameTable(other))
            return true;

        // Cannot add and drop the same column
        if (ChangeType == SqlChangeType.AddColumn && other.ChangeType == SqlChangeType.DropColumn &&
            AffectsSameTable(other) && ColumnName == other.ColumnName)
            return true;

        return false;
    }

    /// <summary>
    /// Adds metadata key-value pair for this change.
    /// </summary>
    public void AddMetadata(string key, object? value)
    {
        Metadata[key] = value;
    }

    /// <summary>
    /// Gets metadata value by key, returns null if not found.
    /// </summary>
    public object? GetMetadata(string key)
    {
        return Metadata.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Determines if this change is a destructive operation.
    /// </summary>
    public bool IsDestructive()
    {
        return ChangeType is SqlChangeType.DropTable or
                             SqlChangeType.DropColumn or
                             SqlChangeType.DropIndex or
                             SqlChangeType.DropForeignKey or
                             SqlChangeType.DropProcedure or
                             SqlChangeType.DropView;
    }

    public override string ToString()
    {
        return $"{GetDescription()} (Line {LineNumber})";
    }
}
