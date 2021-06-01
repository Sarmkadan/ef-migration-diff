#nullable enable
using EfMigrationDiff.CLI;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Tests for the MigrationServices class.
/// </summary>
public class MigrationServicesTests
{
    private readonly SchemaChangeDetectorService _detector = new();
    private readonly ConflictDetectionService _conflictDetector = new(NullLogger<ConflictDetectionService>.Instance);

    /// <summary>
    /// Tests that the DetectChanges method correctly detects a single CreateTable change.
    /// </summary>
    [Fact]
    public void DetectChanges_WithCreateTableContent_DetectsOneCreateTableChange()
    {
        // Arrange
        var migration = new Migration("20240101120000", "CreateUsers", "AppDbContext")
        {
            Content = @"migrationBuilder.CreateTable(name: ""Users"","
        };

        // Act
        var changes = _detector.DetectChanges(migration);

        // Assert
        changes.Should().ContainSingle();
        changes[0].ChangeType.Should().Be(SqlChangeType.CreateTable);
        changes[0].TableName.Should().Be("Users");
    }

    /// <summary>
    /// Tests that the IsMigrationSafe method returns false when the migration content contains a DropTable operation.
    /// </summary>
    [Fact]
    public void IsMigrationSafe_WithDropTableContent_ReturnsFalse()
    {
        // Arrange
        var migration = new Migration("20240101120001", "DropLegacyTable", "AppDbContext")
        {
            Content = @"migrationBuilder.DropTable(name: ""LegacyData"","
        };

        // Act
        var isSafe = _detector.IsMigrationSafe(migration);

        // Assert
        isSafe.Should().BeFalse();
    }

    /// <summary>
    /// Tests that the DetectConflicts method correctly detects a naming conflict when the same table is created with different schema.
    /// </summary>
    [Fact]
    public void DetectConflicts_WhenSameTableCreatedWithDifferentSchema_ReturnsNamingConflict()
    {
        // Arrange — two branches each create "Orders" but with diverged column definitions
        var sourceChanges = new List<SchemaChange>
        {
            new SchemaChange("mig_src", SqlChangeType.CreateTable, @"CreateTable(name: ""Orders"", Id INT)")
            {
                TableName = "Orders"
            }
        };

        var targetChanges = new List<SchemaChange>
        {
            new SchemaChange("mig_tgt", SqlChangeType.CreateTable, @"CreateTable(name: ""Orders"", Id BIGINT)")
            {
                TableName = "Orders"
            }
        };

        // Act
        var conflicts = _conflictDetector.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().Contain(c => c.ConflictType == ConflictType.NameConflict);
    }

    /// <summary>
    /// Tests that the DetectConflicts method correctly detects a column conflict when the same column is modified with different default values.
    /// </summary>
    [Fact]
    public void DetectConflicts_WhenSameColumnModifiedWithDifferentDefaultValue_ReturnsColumnConflict()
    {
        // Arrange - two branches modify the same column but with different default values
        var sourceMigration = new Migration("mig_src", "ModifyColumnWithDefault", "AppDbContext")
        {
            Content = @"migrationBuilder.AlterColumn<string>(name: ""Status"", table: ""Orders"", nullable: false, defaultValue: ""Pending"");"
        };
        var targetMigration = new Migration("mig_tgt", "ModifyColumnWithDifferentDefault", "AppDbContext")
        {
            Content = @"migrationBuilder.AlterColumn<string>(name: ""Status"", table: ""Orders"", nullable: false, defaultValue: ""Approved"");"
        };

        var sourceChanges = _detector.DetectChanges(sourceMigration);
        var targetChanges = _detector.DetectChanges(targetMigration);

        // Act
        var conflicts = _conflictDetector.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().ContainSingle();
        conflicts[0].ConflictType.Should().Be(ConflictType.ColumnConflict);
        conflicts[0].Description.Should().Contain("Status");
        conflicts[0].Description.Should().Contain("conflicting operations");
    }

    /// <summary>
    /// Tests that the DetectConflicts method returns no conflicts when the same column is modified with the same default values.
    /// </summary>
    [Fact]
    public void DetectConflicts_WhenSameColumnModifiedWithSameDefaultValue_ReturnsNoConflicts()
    {
        // Arrange - two branches modify the same column with the same default values
        var sourceMigration = new Migration("mig_src", "ModifyColumnWithSameDefault", "AppDbContext")
        {
            Content = @"migrationBuilder.AlterColumn<string>(name: ""Status"", table: ""Orders"", nullable: false, defaultValue: ""Pending"");"
        };
        var targetMigration = new Migration("mig_tgt", "ModifyColumnWithSameDefault", "AppDbContext")
        {
            Content = @"migrationBuilder.AlterColumn<string>(name: ""Status"", table: ""Orders"", nullable: false, defaultValue: ""Pending"");"
        };

        var sourceChanges = _detector.DetectChanges(sourceMigration);
        var targetChanges = _detector.DetectChanges(targetMigration);

        // Act
        var conflicts = _conflictDetector.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the ExecuteAsync method invokes the registered command exactly once.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithRegisteredMockedCommand_InvokesCommandExactlyOnce()
    {
        // Arrange
        var mockCommand = new Mock<ICommand>();
        mockCommand
            .Setup(c => c.ExecuteAsync(It.IsAny<CommandContext>()))
            .ReturnsAsync(CommandResult.Ok("executed"));

        var mockServiceProvider = new Mock<IServiceProvider>();
        var executor = new CommandExecutor();
        executor.RegisterCommand("greet", mockCommand.Object);

        // Act
        var result = await executor.ExecuteAsync(
            "greet",
            Array.Empty<string>(),
            mockServiceProvider.Object);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("executed");
        mockCommand.Verify(c => c.ExecuteAsync(It.IsAny<CommandContext>()), Times.Once);
    }
}
