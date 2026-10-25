#nullable enable
namespace EfMigrationDiff.Models;

/// <summary>
/// Represents the type of SQL change detected in a migration.
/// </summary>
public enum SqlChangeType
{
    /// <summary>
    /// The change could not be classified.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A table is created.
    /// </summary>
    CreateTable = 1,

    /// <summary>
    /// A table is dropped.
    /// </summary>
    DropTable = 2,

    /// <summary>
    /// A column is added to a table.
    /// </summary>
    AddColumn = 3,

    /// <summary>
    /// A column is dropped from a table.
    /// </summary>
    DropColumn = 4,

    /// <summary>
    /// An existing column is modified.
    /// </summary>
    ModifyColumn = 5,

    /// <summary>
    /// An index is created.
    /// </summary>
    CreateIndex = 6,

    /// <summary>
    /// An index is dropped.
    /// </summary>
    DropIndex = 7,

    /// <summary>
    /// A foreign key constraint is added.
    /// </summary>
    AddForeignKey = 8,

    /// <summary>
    /// A foreign key constraint is dropped.
    /// </summary>
    DropForeignKey = 9,

    /// <summary>
    /// A stored procedure is created.
    /// </summary>
    CreateProcedure = 10,

    /// <summary>
    /// A stored procedure is dropped.
    /// </summary>
    DropProcedure = 11,

    /// <summary>
    /// A view is created.
    /// </summary>
    CreateView = 12,

    /// <summary>
    /// A view is dropped.
    /// </summary>
    DropView = 13,

    /// <summary>
    /// A table is altered in a way not covered by a more specific change type.
    /// </summary>
    AlterTable = 14,

    /// <summary>
    /// A table, column or other object is renamed.
    /// </summary>
    Rename = 15
}

/// <summary>
/// Represents the conflict severity level when migrations conflict.
/// </summary>
public enum ConflictSeverity
{
    /// <summary>
    /// No conflict is present.
    /// </summary>
    None = 0,

    /// <summary>
    /// A conflict that is worth reviewing but does not block a merge.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// A conflict that must be resolved before the migrations can be merged.
    /// </summary>
    Error = 2,

    /// <summary>
    /// A conflict that is likely to break the schema or the database if it is not fixed.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Represents the status of a migration.
/// </summary>
public enum MigrationStatus
{
    /// <summary>
    /// The migration has not been applied yet.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The migration has been applied.
    /// </summary>
    Applied = 1,

    /// <summary>
    /// The migration has been applied and then reverted.
    /// </summary>
    Reverted = 2,

    /// <summary>
    /// The migration was attempted but did not complete.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The migration has been replaced by a later migration.
    /// </summary>
    Superseded = 4
}

/// <summary>
/// Represents the type of conflict detected between migrations.
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// No conflict is present.
    /// </summary>
    None = 0,

    /// <summary>
    /// Two migrations change the same table.
    /// </summary>
    TableConflict = 1,

    /// <summary>
    /// Two migrations change the same column.
    /// </summary>
    ColumnConflict = 2,

    /// <summary>
    /// Two migrations change the same index.
    /// </summary>
    IndexConflict = 3,

    /// <summary>
    /// Two migrations change the same constraint.
    /// </summary>
    ConstraintConflict = 4,

    /// <summary>
    /// Two migrations apply operations that cannot be combined.
    /// </summary>
    OperationConflict = 5,

    /// <summary>
    /// A migration depends on a change that another migration removes or alters.
    /// </summary>
    DependencyConflict = 6,

    /// <summary>
    /// Two migrations use the same name for different objects.
    /// </summary>
    NameConflict = 7
}

/// <summary>
/// Represents comparison result between two migrations.
/// </summary>
public enum ComparisonResult
{
    /// <summary>
    /// The two migrations are identical.
    /// </summary>
    Identical = 0,

    /// <summary>
    /// The two migrations are similar but not identical.
    /// </summary>
    Similar = 1,

    /// <summary>
    /// The two migrations differ in their changes.
    /// </summary>
    Different = 2,

    /// <summary>
    /// The two migrations change the same schema objects in conflicting ways.
    /// </summary>
    Conflicting = 3,

    /// <summary>
    /// The two migrations cannot be applied together.
    /// </summary>
    Incompatible = 4
}
