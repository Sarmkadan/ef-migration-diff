# MigrationEnums

Documents the enumerations declared in `Models/MigrationEnums.cs`. These enums describe the kind of schema change detected in a migration, the severity and type of a conflict between migrations, the lifecycle status of a migration, and the overall result of comparing two migrations. Values are listed in declaration order with their underlying integer.

> **Note:** `Analysis/ConflictResolutionEngine.cs` declares a second, unrelated `ConflictSeverity` enum (`Low`, `Medium`, `High`, `Critical`). It is not the type described below. Make sure the `using` directives in a file select the intended one.

## SqlChangeType

Represents the type of SQL change detected in a migration. The detector assigns one of these values to each `SchemaChange`.

| Value | Int | Meaning |
|---|---|---|
| `Unknown` | 0 | The operation was recognised as a schema change but does not map to a more specific type. This is also the default value for an unset change. |
| `CreateTable` | 1 | A table is created. |
| `DropTable` | 2 | A table is dropped. |
| `AddColumn` | 3 | A column is added to an existing table. |
| `DropColumn` | 4 | A column is removed from a table. |
| `ModifyColumn` | 5 | An existing column is altered, for example its type, nullability or length. |
| `CreateIndex` | 6 | An index is created. |
| `DropIndex` | 7 | An index is dropped. |
| `AddForeignKey` | 8 | A foreign key constraint is added. |
| `DropForeignKey` | 9 | A foreign key constraint is removed. |
| `CreateProcedure` | 10 | A stored procedure is created. |
| `DropProcedure` | 11 | A stored procedure is dropped. |
| `CreateView` | 12 | A view is created. |
| `DropView` | 13 | A view is dropped. |
| `AlterTable` | 14 | A generic `ALTER TABLE` operation that does not fit a more specific value, such as adding a constraint. |
| `Rename` | 15 | A table or column is renamed. |

## ConflictSeverity

Represents the severity of a conflict when migrations from two branches conflict. `ConflictInfo` uses this type and defaults to `Error`. Conflicts at `Error` or `Critical` are treated as blocking.

| Value | Int | Meaning |
|---|---|---|
| `None` | 0 | No conflict. |
| `Warning` | 1 | A conflict that is worth reviewing but does not block the merge. It is eligible for automatic resolution. |
| `Error` | 2 | A conflict that must be resolved before merging. This is the default for a new `ConflictInfo`. |
| `Critical` | 3 | The most severe conflict. It is blocking and is used for conflicting table definitions. |

## MigrationStatus

Represents the lifecycle status of a migration. A new `Migration` starts as `Pending`. Repositories can filter migrations by status.

| Value | Int | Meaning |
|---|---|---|
| `Pending` | 0 | The migration exists in source but has not been applied to the database. This is the default. |
| `Applied` | 1 | The migration has been applied to the database. |
| `Reverted` | 2 | The migration was applied and then rolled back. |
| `Failed` | 3 | An attempt to apply the migration failed. |
| `Superseded` | 4 | The migration has been replaced by a later migration. |

> **Note:** Only `Pending` is assigned in production code today. The other values are set by callers, for example in tests or by data loaded from elsewhere.

## ConflictType

Represents the category of a conflict between migrations. `ConflictInfo.IsValid()` rejects `None`, so a valid conflict must have a real type.

| Value | Int | Meaning |
|---|---|---|
| `None` | 0 | No conflict type. Not valid for a `ConflictInfo`. |
| `TableConflict` | 1 | Both branches change the same table in incompatible ways, such as creating or dropping it. |
| `ColumnConflict` | 2 | Both branches change the same column. |
| `IndexConflict` | 3 | Both branches change the same index. |
| `ConstraintConflict` | 4 | Both branches change the same constraint, such as a foreign key. |
| `OperationConflict` | 5 | Operations from two migrations conflict because their order matters. Labelled "Operation Order Conflict". |
| `DependencyConflict` | 6 | A dependency between migrations is broken. Labelled "Dependency Conflict". No production code currently raises this value. |
| `NameConflict` | 7 | Both branches create the same table with different definitions. Labelled "Naming Conflict". |

## ComparisonResult

Represents the overall result of comparing two migrations or two branches. `MigrationDiff` sets this value from its contents, and `GetResultDescription()` turns it into text.

| Value | Int | Meaning |
|---|---|---|
| `Identical` | 0 | No schema changes and no differences between the two sides. |
| `Similar` | 1 | Both sides have schema changes, but there are no unmatched elements and no blocking conflicts. |
| `Different` | 2 | The two sides differ, with elements present on only one side, and there are no conflicts. |
| `Conflicting` | 3 | At least one blocking conflict exists. Set when a conflict is added or when blocking conflicts are present. |
| `Incompatible` | 4 | Elements differ between the two sides and conflicts exist, but the conflicts are not blocking. |
