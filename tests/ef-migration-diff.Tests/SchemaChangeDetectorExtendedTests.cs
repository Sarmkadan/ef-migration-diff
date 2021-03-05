#nullable enable

using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using FluentAssertions;

namespace EfMigrationDiff.Tests;

public class SchemaChangeDetectorExtendedTests
{
    private readonly SchemaChangeDetectorService _detector = new();

    [Fact]
    public void DetectChanges_WithDropTableContent_DetectsOneDropTableChange()
    {
        // Arrange
        var migration = new Migration("20240115093045", "DropLegacyTable", "AppDbContext")
        {
            Content = @"migrationBuilder.DropTable(name: ""LegacyTable"","
        };

        // Act
        var changes = _detector.DetectChanges(migration);

        // Assert
        changes.Should().ContainSingle();
        changes[0].ChangeType.Should().Be(SqlChangeType.DropTable);
        changes[0].TableName.Should().Be("LegacyTable");
    }

    [Fact]
    public void DetectChanges_WithAlterTableContent_DetectsAlterTableChange()
    {
        // Arrange
        var migration = new Migration("20240115093045", "AlterUsersTable", "AppDbContext")
        {
            Content = @"migrationBuilder.AlterTable(name: ""Users"""
        };

        // Act
        var changes = _detector.DetectChanges(migration);

        // Assert
        changes.Should().ContainSingle();
        changes[0].ChangeType.Should().Be(SqlChangeType.AlterTable);
    }

    [Fact]
    public void DetectChanges_WithAddColumnContent_DetectsAddColumnChange()
    {
        // Arrange
        var migration = new Migration("20240115093045", "AddColumnToUsers", "AppDbContext")
        {
            Content = @"migrationBuilder.AddColumn<string>(name: ""Email"", table: ""Users"""
        };

        // Act
        var changes = _detector.DetectChanges(migration);

        // Assert
        changes.Should().ContainSingle();
        changes[0].ChangeType.Should().Be(SqlChangeType.AddColumn);
    }

    [Fact]
    public void DetectChanges_WithDropColumnContent_DetectsDropColumnChange()
    {
        // Arrange
        var migration = new Migration("20240115093045", "RemoveColumn", "AppDbContext")
        {
            Content = @"migrationBuilder.DropColumn(name: ""OldColumn"", table: ""Users"""
        };

        // Act
        var changes = _detector.DetectChanges(migration);

        // Assert
        changes.Should().ContainSingle();
        changes[0].ChangeType.Should().Be(SqlChangeType.DropColumn);
    }

    [Fact]
    public void DetectChanges_WithCreateIndexContent_DetectsCreateIndexChange()
    {
        // Arrange
        var migration = new Migration("20240115093045", "CreateIndexOnEmail", "AppDbContext")
        {
            Content = @"migrationBuilder.CreateIndex(name: ""IX_Users_Email"", table: ""Users"", column: ""Email"""
        };

        // Act
        var changes = _detector.DetectChanges(migration);

        // Assert
        changes.Should().ContainSingle();
        changes[0].ChangeType.Should().Be(SqlChangeType.CreateIndex);
    }

    [Fact]
    public void DetectChanges_WithMultipleDifferentOperations_DetectsAllChanges()
    {
        // Arrange
        var migration = new Migration("20240115093045", "ComplexMigration", "AppDbContext")
        {
            Content = @"
                migrationBuilder.CreateTable(name: ""Orders"",
                migrationBuilder.AddColumn<string>(name: ""Status"", table: ""Orders"",
                migrationBuilder.CreateIndex(name: ""IX_Orders_CustomerId"", table: ""Orders"""
        };

        // Act
        var changes = _detector.DetectChanges(migration);

        // Assert
        changes.Should().HaveCount(3);
        changes.Should().Contain(c => c.ChangeType == SqlChangeType.CreateTable);
        changes.Should().Contain(c => c.ChangeType == SqlChangeType.AddColumn);
        changes.Should().Contain(c => c.ChangeType == SqlChangeType.CreateIndex);
    }

    [Fact]
    public void DetectChanges_WithEmptyContent_ReturnsEmptyList()
    {
        // Arrange
        var migration = new Migration("20240115093045", "EmptyMigration", "AppDbContext")
        {
            Content = string.Empty
        };

        // Act
        var changes = _detector.DetectChanges(migration);

        // Assert
        changes.Should().BeEmpty();
    }

    [Fact]
    public void DetectChanges_WithUnrelatedContent_ReturnsEmptyList()
    {
        // Arrange
        var migration = new Migration("20240115093045", "UnrelatedCode", "AppDbContext")
        {
            Content = "// This is just a comment\nvar x = 5;"
        };

        // Act
        var changes = _detector.DetectChanges(migration);

        // Assert
        changes.Should().BeEmpty();
    }

    [Fact]
    public void IsMigrationSafe_WithCreateTableOnly_ReturnsTrue()
    {
        // Arrange
        var migration = new Migration("20240115093045", "CreateTable", "AppDbContext")
        {
            Content = @"migrationBuilder.CreateTable(name: ""NewTable"""
        };

        // Act
        var isSafe = _detector.IsMigrationSafe(migration);

        // Assert
        isSafe.Should().BeTrue();
    }

    [Fact]
    public void IsMigrationSafe_WithDropTable_ReturnsFalse()
    {
        // Arrange
        var migration = new Migration("20240115093045", "DropTable", "AppDbContext")
        {
            Content = @"migrationBuilder.DropTable(name: ""Users"""
        };

        // Act
        var isSafe = _detector.IsMigrationSafe(migration);

        // Assert
        isSafe.Should().BeFalse();
    }

    [Fact]
    public void IsMigrationSafe_WithDropColumn_ReturnsFalse()
    {
        // Arrange
        var migration = new Migration("20240115093045", "DropColumn", "AppDbContext")
        {
            Content = @"migrationBuilder.DropColumn(name: ""Email"", table: ""Users"""
        };

        // Act
        var isSafe = _detector.IsMigrationSafe(migration);

        // Assert
        isSafe.Should().BeFalse();
    }

    [Fact]
    public void IsMigrationSafe_WithAddColumnNonNullable_ReturnsFalse()
    {
        // Arrange
        var migration = new Migration("20240115093045", "AddNonNullableColumn", "AppDbContext")
        {
            Content = @"migrationBuilder.AddColumn<string>(name: ""RequiredField"", table: ""Users"", nullable: false"
        };

        // Act
        var isSafe = _detector.IsMigrationSafe(migration);

        // Assert
        isSafe.Should().BeFalse();
    }

    [Fact]
    public void IsMigrationSafe_WithAddColumnNullable_ReturnsTrue()
    {
        // Arrange
        var migration = new Migration("20240115093045", "AddNullableColumn", "AppDbContext")
        {
            Content = @"migrationBuilder.AddColumn<string>(name: ""OptionalField"", table: ""Users"", nullable: true"
        };

        // Act
        var isSafe = _detector.IsMigrationSafe(migration);

        // Assert
        isSafe.Should().BeTrue();
    }

    [Fact]
    public void DetectChanges_WithRenameTableOperation_DetectsRenameChange()
    {
        // Arrange
        var migration = new Migration("20240115093045", "RenameTable", "AppDbContext")
        {
            Content = @"migrationBuilder.RenameTable(name: ""OldName"", newName: ""NewName"""
        };

        // Act
        var changes = _detector.DetectChanges(migration);

        // Assert
        changes.Should().NotBeEmpty();
        changes.Should().Contain(c => c.ChangeType == SqlChangeType.Rename);
    }

    [Fact]
    public void DetectChanges_ExtractsTableNameFromCreateTable()
    {
        // Arrange
        var migration = new Migration("20240115093045", "CreateUsers", "AppDbContext")
        {
            Content = @"migrationBuilder.CreateTable(name: ""Users"","
        };

        // Act
        var changes = _detector.DetectChanges(migration);

        // Assert
        changes.Should().ContainSingle();
        changes[0].TableName.Should().Be("Users");
    }

    [Fact]
    public void DetectChanges_WithCaseSensitiveTableNames_PreservesCase()
    {
        // Arrange
        var migration = new Migration("20240115093045", "CreateTable", "AppDbContext")
        {
            Content = @"migrationBuilder.CreateTable(name: ""UserProfiles"""
        };

        // Act
        var changes = _detector.DetectChanges(migration);

        // Assert
        changes[0].TableName.Should().Be("UserProfiles");
    }
}
