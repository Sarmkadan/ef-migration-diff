#nullable enable

using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

/// <summary>
/// Tests for the ConflictDetectionService class.
/// </summary>
public class ConflictDetectionServiceTests
{
    private readonly ConflictDetectionService _service;
    private readonly Mock<ILogger<ConflictDetectionService>> _loggerMock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictDetectionServiceTests"/> class.
    /// </summary>
    public ConflictDetectionServiceTests()
    {
        _loggerMock = new Mock<ILogger<ConflictDetectionService>>();
        _service = new ConflictDetectionService(_loggerMock.Object);
    }

    /// <summary>
    /// Verifies that the DetectConflicts method returns an empty list when there are no changes.
    /// </summary>
    [Fact]
    public void DetectConflicts_WithNoChanges_ReturnsEmptyList()
    {
        var sourceChanges = new List<SchemaChange>();
        var targetChanges = new List<SchemaChange>();

        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        conflicts.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that the DetectConflicts method returns a table conflict when there are conflicting table operations.
    /// </summary>
    [Fact]
    public void DetectConflicts_WithConflictingTableOperations_ReturnsTableConflict()
    {
        var sourceChanges = new List<SchemaChange> {
            new SchemaChange("m1", SqlChangeType.CreateTable, "CREATE TABLE Users") { TableName = "Users" }
        };
        var targetChanges = new List<SchemaChange> {
            new SchemaChange("m2", SqlChangeType.DropTable, "DROP TABLE Users") { TableName = "Users" }
        };

        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        conflicts.Should().ContainSingle();
        conflicts.First().ConflictType.Should().Be(ConflictType.TableConflict);
        conflicts.First().Severity.Should().Be(ConflictSeverity.Error);
    }

    /// <summary>
    /// Verifies that the DetectConflicts method returns a column conflict when there are conflicting column operations.
    /// </summary>
    [Fact]
    public void DetectConflicts_WithConflictingColumnOperations_ReturnsColumnConflict()
    {
        var sourceChanges = new List<SchemaChange> {
            new SchemaChange("m1", SqlChangeType.AddColumn, "ALTER TABLE Users ADD Name") { TableName = "Users", ColumnName = "Name" }
        };
        var targetChanges = new List<SchemaChange> {
            new SchemaChange("m2", SqlChangeType.DropColumn, "ALTER TABLE Users DROP COLUMN Name") { TableName = "Users", ColumnName = "Name" }
        };

        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        conflicts.Should().ContainSingle();
        conflicts.First().ConflictType.Should().Be(ConflictType.ColumnConflict);
    }

    /// <summary>
    /// Verifies that the DetectConflicts method returns an empty list when there are non-conflicting changes.
    /// </summary>
    [Fact]
    public void DetectConflicts_WithNonConflictingChanges_ReturnsEmptyList()
    {
        var sourceChanges = new List<SchemaChange> {
            new SchemaChange("m1", SqlChangeType.CreateTable, "CREATE TABLE Users") { TableName = "Users" }
        };
        var targetChanges = new List<SchemaChange> {
            new SchemaChange("m2", SqlChangeType.CreateTable, "CREATE TABLE Products") { TableName = "Products" }
        };

        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        conflicts.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that the DetectConflicts method returns an error severity when there are conflicting table operations.
    /// </summary>
    [Fact]
    public void DetectConflicts_WithConflictingTableOperations_ReturnsErrorSeverity()
    {
        var sourceChanges = new List<SchemaChange> {
            new SchemaChange("m1", SqlChangeType.CreateTable, "CREATE TABLE Users") { TableName = "Users" }
        };
        var targetChanges = new List<SchemaChange> {
            new SchemaChange("m2", SqlChangeType.DropTable, "DROP TABLE Users") { TableName = "Users" }
        };

        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        conflicts.First().Severity.Should().Be(ConflictSeverity.Error);
    }

    /// <summary>
    /// Verifies that the DetectConflicts method returns a warning severity when there are conflicting index operations.
    /// </summary>
    [Fact]
    public void DetectConflicts_WithConflictingIndexOperations_ReturnsWarningSeverity()
    {
        var sourceChanges = new List<SchemaChange> {
            new SchemaChange("m1", SqlChangeType.CreateIndex, "CREATE INDEX Idx_Users_Name ON Users(Name)") { TableName = "Users" }
        };
        sourceChanges.First().AddMetadata("IndexName", "Idx_Users_Name");
        var targetChanges = new List<SchemaChange> {
            new SchemaChange("m2", SqlChangeType.DropIndex, "DROP INDEX Idx_Users_Name ON Users") { TableName = "Users" }
        };
        targetChanges.First().AddMetadata("IndexName", "Idx_Users_Name");

        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        conflicts.Should().ContainSingle();
        conflicts.First().ConflictType.Should().Be(ConflictType.IndexConflict);
        conflicts.First().Severity.Should().Be(ConflictSeverity.Warning);
    }

    /// <summary>
    /// Tests that two migrations modifying the same column with different default values are detected as conflicts.
    /// </summary>
    [Fact]
    public void DetectConflicts_TwoMigrationsModifyingSameColumnWithDifferentDefaultValues_ReturnsColumnConflict()
    {
        // Arrange
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.ModifyColumn, "ALTER TABLE Users ALTER COLUMN Email")
            {
                TableName = "Users",
                ColumnName = "Email",
                DefaultValue = null
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.ModifyColumn, "ALTER TABLE Users ALTER COLUMN Email")
            {
                TableName = "Users",
                ColumnName = "Email",
                DefaultValue = "user@example.com"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().ContainSingle();
        conflicts.First().ConflictType.Should().Be(ConflictType.ColumnConflict);
        conflicts.First().Severity.Should().Be(ConflictSeverity.Error);
    }

    /// <summary>
    /// Tests that two migrations altering the same table are detected as conflicts.
    /// </summary>
    [Fact]
    public void DetectConflicts_TwoMigrationsAlteringSameTable_ReturnsTableConflict()
    {
        // Arrange
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.AlterTable, "ALTER TABLE Users ADD CONSTRAINT")
            {
                TableName = "Users"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.DropTable, "DROP TABLE Users")
            {
                TableName = "Users"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().ContainSingle();
        conflicts.First().ConflictType.Should().Be(ConflictType.TableConflict);
        conflicts.First().Severity.Should().Be(ConflictSeverity.Error);
    }

    /// <summary>
    /// Tests that drop and drop table operations on same table don't conflict (idempotent).
    /// </summary>
    [Fact]
    public void DetectConflicts_DropAndDropTableSameTable_ReturnsNoConflicts()
    {
        // Arrange
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.DropTable, "DROP TABLE Users")
            {
                TableName = "Users"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.DropTable, "DROP TABLE Users")
            {
                TableName = "Users"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that two migrations creating different tables don't conflict.
    /// </summary>
    [Fact]
    public void DetectConflicts_TwoMigrationsCreatingDifferentTables_ReturnsNoConflicts()
    {
        // Arrange
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.CreateTable, "CREATE TABLE Users")
            {
                TableName = "Users"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.CreateTable, "CREATE TABLE Products")
            {
                TableName = "Products"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that two migrations adding different columns to same table don't conflict.
    /// </summary>
    [Fact]
    public void DetectConflicts_TwoMigrationsAddingDifferentColumnsToSameTable_ReturnsNoConflicts()
    {
        // Arrange
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.AddColumn, "ALTER TABLE Users ADD Email")
            {
                TableName = "Users",
                ColumnName = "Email"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.AddColumn, "ALTER TABLE Users ADD Phone")
            {
                TableName = "Users",
                ColumnName = "Phone"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that identical table operations don't create conflicts.
    /// </summary>
    [Fact]
    public void DetectConflicts_IdenticalTableOperations_ReturnsNoConflicts()
    {
        // Arrange - identical operations should not conflict (different migration IDs)
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.AddColumn, "ALTER TABLE Users ADD Email")
            {
                TableName = "Users",
                ColumnName = "Email"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.AddColumn, "ALTER TABLE Users ADD Email")
            {
                TableName = "Users",
                ColumnName = "Email"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert - identical operations on different columns don't conflict
        conflicts.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that rename vs modify conflicts are detected when a column is renamed in one branch
    /// and modified in another branch.
    /// </summary>
    [Fact]
    public void DetectConflicts_RenameVsModifyColumn_DetectsCriticalConflict()
    {
        // Arrange - Branch A renames a column, Branch B modifies the same column
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.Rename, "RENAME COLUMN Users.Email TO UserEmail")
            {
                TableName = "Users",
                OldValue = "Email",
                NewValue = "UserEmail"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.ModifyColumn, "ALTER TABLE Users ALTER COLUMN UserEmail")
            {
                TableName = "Users",
                ColumnName = "UserEmail"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().ContainSingle();
        conflicts.First().ConflictType.Should().Be(ConflictType.ColumnConflict);
        conflicts.First().Severity.Should().Be(ConflictSeverity.Critical);
        conflicts.First().Description.Should().Contain("renamed in one branch");
        conflicts.First().AffectedElements.Should().ContainSingle().Which.Should().Be("Users.UserEmail");
    }

    /// <summary>
    /// Tests that rename vs modify conflicts are detected in both directions.
    /// </summary>
    [Fact]
    public void DetectConflicts_ModifyVsRenameColumn_DetectsCriticalConflict()
    {
        // Arrange - Branch A modifies a column, Branch B renames the same column
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.ModifyColumn, "ALTER TABLE Users ALTER COLUMN Email")
            {
                TableName = "Users",
                ColumnName = "Email"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.Rename, "RENAME COLUMN Users.Email TO UserEmail")
            {
                TableName = "Users",
                OldValue = "Email",
                NewValue = "UserEmail"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().ContainSingle();
        conflicts.First().ConflictType.Should().Be(ConflictType.ColumnConflict);
        conflicts.First().Severity.Should().Be(ConflictSeverity.Critical);
    }

    /// <summary>
    /// Tests that rename operations conflict with other operations on the same table.
    /// </summary>
    [Fact]
    public void DetectConflicts_RenameTableWithOtherOperations_DetectsConflict()
    {
        // Arrange - Branch A renames a table, Branch B alters the same table
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.Rename, "RENAME TABLE Users TO AppUsers")
            {
                TableName = "Users",
                OldValue = "Users",
                NewValue = "AppUsers"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.AlterTable, "ALTER TABLE Users ADD COLUMN IsActive")
            {
                TableName = "Users"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().ContainSingle();
        conflicts.First().ConflictType.Should().Be(ConflictType.TableConflict);
        conflicts.First().Severity.Should().Be(ConflictSeverity.Error);
    }

    /// <summary>
    /// Tests that identical operations in different order don't create false conflicts.
    /// </summary>
    [Fact]
    public void DetectConflicts_IdenticalOperationsDifferentOrder_NoFalseConflicts()
    {
        // Arrange - Both branches add the same column, just in different order
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.AddColumn, "ALTER TABLE Users ADD Email")
            {
                TableName = "Users",
                ColumnName = "Email"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.AddColumn, "ALTER TABLE Users ADD Email")
            {
                TableName = "Users",
                ColumnName = "Email"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert - identical operations should not conflict
        conflicts.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that index definition conflicts are detected when the same index name
    /// has different definitions in each branch.
    /// </summary>
    [Fact]
    public void DetectConflicts_IndexWithSameNameDifferentDefinitions_DetectsConflict()
    {
        // Arrange - Both branches create an index with the same name but different definitions
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.CreateIndex, "CREATE INDEX Idx_Users_Email ON Users(Email)")
            {
                TableName = "Users"
            }
        };
        sourceChanges.First().AddMetadata("IndexName", "Idx_Users_Email");

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.CreateIndex, "CREATE INDEX Idx_Users_Email ON Users(Email, IsActive)")
            {
                TableName = "Users"
            }
        };
        targetChanges.First().AddMetadata("IndexName", "Idx_Users_Email");

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().ContainSingle();
        conflicts.First().ConflictType.Should().Be(ConflictType.IndexConflict);
        conflicts.First().Severity.Should().Be(ConflictSeverity.Error);
        conflicts.First().Description.Should().Contain("different definitions");
    }

    /// <summary>
    /// Tests that drop operations on the same object in different branches don't conflict
    /// as they are idempotent.
    /// </summary>
    [Fact]
    public void DetectConflicts_DropOperationsSameObject_IdempotentNoConflict()
    {
        // Arrange - Both branches drop the same table
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.DropTable, "DROP TABLE Users")
            {
                TableName = "Users"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.DropTable, "DROP TABLE Users")
            {
                TableName = "Users"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert - drop operations are idempotent, should not conflict
        conflicts.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that rename operations are detected as conflicts when combined with other operations.
    /// </summary>
    [Fact]
    public void DetectConflicts_RenameOperationWithDifferentOperation_DetectsConflict()
    {
        // Arrange - Branch A renames a column, Branch B drops a different column
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.Rename, "RENAME COLUMN Users.Email TO UserEmail")
            {
                TableName = "Users",
                OldValue = "Email",
                NewValue = "UserEmail"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.DropColumn, "ALTER TABLE Users DROP COLUMN Phone")
            {
                TableName = "Users",
                ColumnName = "Phone"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert - rename conflicts with any operation on the same table
        conflicts.Should().ContainSingle();
        conflicts.First().ConflictType.Should().Be(ConflictType.TableConflict);
    }

    /// <summary>
    /// Tests that null source changes throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void DetectConflicts_NullSourceChanges_ThrowsArgumentNullException()
    {
        // Arrange
        var targetChanges = new List<SchemaChange>();

        // Act & Assert
        var act = () => _service.DetectConflicts(null!, targetChanges);
        act.Should().ThrowExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that null target changes throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void DetectConflicts_NullTargetChanges_ThrowsArgumentNullException()
    {
        // Arrange
        var sourceChanges = new List<SchemaChange>();

        // Act & Assert
        var act = () => _service.DetectConflicts(sourceChanges, null!);
        act.Should().ThrowExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Tests multiple different conflict types are all detected in one call.
    /// </summary>
    [Fact]
    public void DetectConflicts_MultipleConflictTypes_AllDetected()
    {
        // Arrange
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.CreateTable, "CREATE TABLE Users")
            {
                TableName = "Users"
            },
            new SchemaChange("m3", SqlChangeType.AddColumn, "ALTER TABLE Orders ADD Total")
            {
                TableName = "Orders",
                ColumnName = "Total"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.DropTable, "DROP TABLE Users")
            {
                TableName = "Users"
            },
            new SchemaChange("m4", SqlChangeType.DropColumn, "ALTER TABLE Orders DROP COLUMN Total")
            {
                TableName = "Orders",
                ColumnName = "Total"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().HaveCount(2);

        var tableConflict = conflicts.First(c => c.ConflictType == ConflictType.TableConflict);
        tableConflict.Severity.Should().Be(ConflictSeverity.Error);
        tableConflict.AffectedElements.Should().ContainSingle().Which.Should().Be("Users");

        var columnConflict = conflicts.First(c => c.ConflictType == ConflictType.ColumnConflict);
        columnConflict.Severity.Should().Be(ConflictSeverity.Error);
        columnConflict.AffectedElements.Should().ContainSingle().Which.Should().Be("Orders.Total");
    }

    /// <summary>
    /// Tests that the service properly logs when conflicts are detected.
    /// </summary>
    [Fact]
    public void DetectConflicts_WithConflicts_LogsWarning()
    {
        // Arrange
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.CreateTable, "CREATE TABLE Users")
            {
                TableName = "Users"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.DropTable, "DROP TABLE Users")
            {
                TableName = "Users"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("conflicts")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }

    /// <summary>
    /// Tests that the service properly logs when no conflicts are detected.
    /// </summary>
    [Fact]
    public void DetectConflicts_WithoutConflicts_LogsDebug()
    {
        // Arrange
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.CreateTable, "CREATE TABLE Users")
            {
                TableName = "Users"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.CreateTable, "CREATE TABLE Products")
            {
                TableName = "Products"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No conflicts")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }

    /// <summary>
    /// Tests that identical operations in different order don't create false conflicts.
    /// This is the key improvement: identical operations should NOT conflict regardless of order.
    /// </summary>
    [Fact]
    public void DetectConflicts_IdenticalOperationsDifferentOrder_ShouldNotConflict()
    {
        // Arrange - Both branches have identical operations but in different order
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.AddColumn, "ALTER TABLE Users ADD Email")
            {
                TableName = "Users",
                ColumnName = "Email"
            },
            new SchemaChange("m2", SqlChangeType.AddColumn, "ALTER TABLE Users ADD Phone")
            {
                TableName = "Users",
                ColumnName = "Phone"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m3", SqlChangeType.AddColumn, "ALTER TABLE Users ADD Phone")
            {
                TableName = "Users",
                ColumnName = "Phone"
            },
            new SchemaChange("m4", SqlChangeType.AddColumn, "ALTER TABLE Users ADD Email")
            {
                TableName = "Users",
                ColumnName = "Email"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert - identical operations in different order should NOT conflict
        conflicts.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that rename vs modify conflict detection when a column is renamed in one branch
    /// and modified in another branch (the rename hides the target).
    /// </summary>
    [Fact]
    public void DetectConflicts_RenameColumnVsModifyColumn_DetectsCriticalConflict()
    {
        // Arrange - Branch A renames Email to UserEmail, Branch B modifies the original Email column
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.Rename, "RENAME COLUMN Users.Email TO UserEmail")
            {
                TableName = "Users",
                OldValue = "Email",
                NewValue = "UserEmail"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.ModifyColumn, "ALTER TABLE Users ALTER COLUMN Email")
            {
                TableName = "Users",
                ColumnName = "Email"
            }
        };

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().ContainSingle();
        conflicts.First().ConflictType.Should().Be(ConflictType.ColumnConflict);
        conflicts.First().Severity.Should().Be(ConflictSeverity.Critical);
        conflicts.First().Description.Should().Contain("renamed in one branch");
        conflicts.First().AffectedElements.Should().ContainSingle().Which.Should().Be("Users.Email");
    }

    /// <summary>
    /// Tests that index definition conflicts are detected when the same index name
    /// has different SQL definitions in each branch.
    /// </summary>
    [Fact]
    public void DetectConflicts_IndexSameNameDifferentDefinitions_DetectsConflict()
    {
        // Arrange - Both branches create an index with the same name but different columns
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("m1", SqlChangeType.CreateIndex, "CREATE INDEX Idx_Users_Email ON Users(Email)")
            {
                TableName = "Users"
            }
        };
        sourceChanges.First().AddMetadata("IndexName", "Idx_Users_Email");

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("m2", SqlChangeType.CreateIndex, "CREATE INDEX Idx_Users_Email ON Users(Email, IsActive)")
            {
                TableName = "Users"
            }
        };
        targetChanges.First().AddMetadata("IndexName", "Idx_Users_Email");

        // Act
        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().ContainSingle();
        conflicts.First().ConflictType.Should().Be(ConflictType.IndexConflict);
        conflicts.First().Severity.Should().Be(ConflictSeverity.Error);
        conflicts.First().Description.Should().Contain("different definitions");
    }
}
