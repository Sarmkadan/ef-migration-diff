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
}
