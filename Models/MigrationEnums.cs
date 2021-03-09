#nullable enable
namespace EfMigrationDiff.Models;

/// <summary>
/// Represents the type of SQL change detected in a migration.
/// </summary>
public enum SqlChangeType
{
    Unknown = 0,
    CreateTable = 1,
    DropTable = 2,
    AddColumn = 3,
    DropColumn = 4,
    ModifyColumn = 5,
    CreateIndex = 6,
    DropIndex = 7,
    AddForeignKey = 8,
    DropForeignKey = 9,
    CreateProcedure = 10,
    DropProcedure = 11,
    CreateView = 12,
    DropView = 13,
    AlterTable = 14,
    Rename = 15
}

/// <summary>
/// Represents the conflict severity level when migrations conflict.
/// </summary>
public enum ConflictSeverity
{
    None = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

/// <summary>
/// Represents the status of a migration.
/// </summary>
public enum MigrationStatus
{
    Pending = 0,
    Applied = 1,
    Reverted = 2,
    Failed = 3,
    Superseded = 4
}

/// <summary>
/// Represents the type of conflict detected between migrations.
/// </summary>
public enum ConflictType
{
    None = 0,
    TableConflict = 1,
    ColumnConflict = 2,
    IndexConflict = 3,
    ConstraintConflict = 4,
    OperationConflict = 5,
    DependencyConflict = 6,
    NameConflict = 7
}

/// <summary>
/// Represents comparison result between two migrations.
/// </summary>
public enum ComparisonResult
{
    Identical = 0,
    Similar = 1,
    Different = 2,
    Conflicting = 3,
    Incompatible = 4
}
