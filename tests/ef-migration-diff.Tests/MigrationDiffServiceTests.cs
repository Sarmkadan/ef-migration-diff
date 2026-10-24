#nullable enable

using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using EfMigrationDiff.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Provides unit tests for the <see cref="MigrationDiffService"/> class.
/// Tests various scenarios for comparing Entity Framework Core migrations between branches,
/// including identical migrations, migrations present in only one branch, schema changes,
/// and conflict detection.
/// </summary>
public class MigrationDiffServiceTests
{
    /// <summary>
    /// Tests that comparing branches with a null source branch throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void CompareBranches_WithNullSourceBranch_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<MigrationRepository>();
        var mockConflictDetector = new Mock<ConflictDetectionService>(NullLogger<ConflictDetectionService>.Instance);
        var mockSchemaDetector = new Mock<SchemaChangeDetectorService>();
        var service = new MigrationDiffService(mockRepository.Object, mockConflictDetector.Object, mockSchemaDetector.Object, NullLogger<MigrationDiffService>.Instance);

        var targetBranch = new BranchInfo("main", "abc123");

        // Act & Assert
        var act = () => service.CompareBranches(null!, targetBranch);
        act.Should().ThrowExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that comparing branches with a null target branch throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void CompareBranches_WithNullTargetBranch_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<MigrationRepository>();
        var mockConflictDetector = new Mock<ConflictDetectionService>(NullLogger<ConflictDetectionService>.Instance);
        var mockSchemaDetector = new Mock<SchemaChangeDetectorService>();
        var service = new MigrationDiffService(mockRepository.Object, mockConflictDetector.Object, mockSchemaDetector.Object, NullLogger<MigrationDiffService>.Instance);

        var sourceBranch = new BranchInfo("feature", "def456");

        // Act & Assert
        var act = () => service.CompareBranches(sourceBranch, null!);
        act.Should().ThrowExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that when both branches contain the same migrations, they are correctly categorized in the 'InBoth' collection.
    /// Verifies that no migrations are incorrectly categorized as being only in one branch.
    /// </summary>
    [Fact]
    public void CompareBranches_WithIdenticalMigrations_CategorizesCorrectly()
    {
        // Arrange
        var repository = new MigrationRepository();
        var conflictDetector = new ConflictDetectionService(NullLogger<ConflictDetectionService>.Instance);
        var schemaDetector = new SchemaChangeDetectorService();
        var service = new MigrationDiffService(repository, conflictDetector, schemaDetector, NullLogger<MigrationDiffService>.Instance);

        var migration = new Migration("20240115093045", "CreateUsers", "AppDbContext")
        {
            Content = "migrationBuilder.CreateTable(...)"
        };
        repository.Add(migration);

        var sourceBranch = new BranchInfo("feature", "abc123");
        var targetBranch = new BranchInfo("main", "def456");
        sourceBranch.AddMigration("20240115093045");
        targetBranch.AddMigration("20240115093045");

        // Act
        var result = service.CompareBranches(sourceBranch, targetBranch);

        // Assert
        result.Should().NotBeNull();
        result.InBoth.Should().ContainSingle();
        result.OnlyInSource.Should().BeEmpty();
        result.OnlyInTarget.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that migrations present only in the source branch are correctly identified and categorized in the 'OnlyInSource' collection.
    /// Verifies that the migration ID is properly extracted and returned.
    /// </summary>
    [Fact]
    public void CompareBranches_WithSourceOnlyMigration_CategorizesCorrectly()
    {
        // Arrange
        var repository = new MigrationRepository();
        var conflictDetector = new ConflictDetectionService(NullLogger<ConflictDetectionService>.Instance);
        var schemaDetector = new SchemaChangeDetectorService();
        var service = new MigrationDiffService(repository, conflictDetector, schemaDetector, NullLogger<MigrationDiffService>.Instance);

        var sourceMigration = new Migration("20240115093045", "CreateNewTable", "AppDbContext");
        var targetMigration = new Migration("20240115093044", "OldMigration", "AppDbContext");

        repository.Add(sourceMigration);
        repository.Add(targetMigration);

        var sourceBranch = new BranchInfo("feature", "abc123");
        var targetBranch = new BranchInfo("main", "def456");
        sourceBranch.AddMigration("20240115093045");
        sourceBranch.AddMigration("20240115093044");
        targetBranch.AddMigration("20240115093044");

        // Act
        var result = service.CompareBranches(sourceBranch, targetBranch);

        // Assert
        result.OnlyInSource.Should().ContainSingle();
        result.OnlyInSource[0].Id.Should().Be("20240115093045");
        result.InBoth.Should().ContainSingle();
    }

    /// <summary>
    /// Tests that migrations present only in the target branch are correctly identified and categorized in the 'OnlyInTarget' collection.
    /// Verifies that the migration ID is properly extracted and returned.
    /// </summary>
    [Fact]
    public void CompareBranches_WithTargetOnlyMigration_CategorizesCorrectly()
    {
        // Arrange
        var repository = new MigrationRepository();
        var conflictDetector = new ConflictDetectionService(NullLogger<ConflictDetectionService>.Instance);
        var schemaDetector = new SchemaChangeDetectorService();
        var service = new MigrationDiffService(repository, conflictDetector, schemaDetector, NullLogger<MigrationDiffService>.Instance);

        var sourceMigration = new Migration("20240115093044", "OldMigration", "AppDbContext");
        var targetMigration = new Migration("20240115093046", "TargetOnlyMigration", "AppDbContext");

        repository.Add(sourceMigration);
        repository.Add(targetMigration);

        var sourceBranch = new BranchInfo("feature", "abc123");
        var targetBranch = new BranchInfo("main", "def456");
        sourceBranch.AddMigration("20240115093044");
        targetBranch.AddMigration("20240115093044");
        targetBranch.AddMigration("20240115093046");

        // Act
        var result = service.CompareBranches(sourceBranch, targetBranch);

        // Assert
        result.OnlyInTarget.Should().ContainSingle();
        result.OnlyInTarget[0].Id.Should().Be("20240115093046");
    }

    /// <summary>
    /// Tests that schema changes introduced by migrations are correctly detected and categorized in the 'SourceSchemaChanges' collection.
    /// Verifies that CREATE TABLE operations are properly identified from migration content.
    /// </summary>
    [Fact]
    public void CompareBranches_DetectsSchemaChanges()
    {
        // Arrange
        var repository = new MigrationRepository();
        var conflictDetector = new ConflictDetectionService(NullLogger<ConflictDetectionService>.Instance);
        var schemaDetector = new SchemaChangeDetectorService();
        var service = new MigrationDiffService(repository, conflictDetector, schemaDetector, NullLogger<MigrationDiffService>.Instance);

        var sourceMigration = new Migration("20240115093045", "CreateUsers", "AppDbContext")
        {
            Content = @"migrationBuilder.CreateTable(name: ""Users"""
        };

        repository.Add(sourceMigration);

        var sourceBranch = new BranchInfo("feature", "abc123");
        var targetBranch = new BranchInfo("main", "def456");
        sourceBranch.AddMigration("20240115093045");

        // Act
        var result = service.CompareBranches(sourceBranch, targetBranch);

        // Assert
        result.SourceSchemaChanges.Should().NotBeEmpty();
        result.SourceSchemaChanges.Should().Contain(c => c.ChangeType == SqlChangeType.CreateTable);
    }

    /// <summary>
    /// Tests that when both branches are empty (contain no migrations), the comparison returns an empty diff result.
    /// Verifies that all collections (OnlyInSource, OnlyInTarget, InBoth, Conflicts) are empty.
    /// </summary>
    [Fact]
    public void CompareBranches_WithEmptyBranches_ReturnsEmptyDiff()
    {
        // Arrange
        var repository = new MigrationRepository();
        var conflictDetector = new ConflictDetectionService(NullLogger<ConflictDetectionService>.Instance);
        var schemaDetector = new SchemaChangeDetectorService();
        var service = new MigrationDiffService(repository, conflictDetector, schemaDetector, NullLogger<MigrationDiffService>.Instance);

        var sourceBranch = new BranchInfo("feature", "abc123");
        var targetBranch = new BranchInfo("main", "def456");

        // Act
        var result = service.CompareBranches(sourceBranch, targetBranch);

        // Assert
        result.OnlyInSource.Should().BeEmpty();
        result.OnlyInTarget.Should().BeEmpty();
        result.InBoth.Should().BeEmpty();
        result.Conflicts.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a diff without schema changes yields all-zero statistics.
    /// </summary>
    [Fact]
    public void GetDiffStatistics_WithNoSchemaChanges_ReturnsZeroCounts()
    {
        // Arrange
        var service = CreateService();
        var diff = new MigrationDiff("feature", "main");

        // Act
        var stats = service.GetDiffStatistics(diff);

        // Assert
        stats.Should().Be(new DiffStatistics(0, 0, 0, 0, 0, 0, 0));
    }

    /// <summary>
    /// Tests that table, column and index changes from both branches are counted by kind,
    /// with tables and columns counted distinctly by name.
    /// </summary>
    [Fact]
    public void GetDiffStatistics_WithMixedChanges_CountsByKind()
    {
        // Arrange
        var service = CreateService();
        var diff = new MigrationDiff("feature", "main");
        diff.SourceSchemaChanges.Add(CreateChange(SqlChangeType.CreateTable, "Orders"));
        diff.SourceSchemaChanges.Add(CreateChange(SqlChangeType.DropTable, "Legacy"));
        diff.SourceSchemaChanges.Add(CreateChange(SqlChangeType.AlterTable, "Users"));
        diff.SourceSchemaChanges.Add(CreateChange(SqlChangeType.AddColumn, "Users", "Email"));
        diff.SourceSchemaChanges.Add(CreateChange(SqlChangeType.AddColumn, "Users", "Email"));
        diff.SourceSchemaChanges.Add(CreateChange(SqlChangeType.CreateIndex, "Orders"));
        diff.TargetSchemaChanges.Add(CreateChange(SqlChangeType.DropColumn, "Users", "Phone"));
        diff.TargetSchemaChanges.Add(CreateChange(SqlChangeType.ModifyColumn, "Users", "Name"));
        diff.TargetSchemaChanges.Add(CreateChange(SqlChangeType.DropIndex, "Users"));

        // Act
        var stats = service.GetDiffStatistics(diff);

        // Assert
        stats.Should().Be(new DiffStatistics(
            TablesAdded: 1,
            TablesRemoved: 1,
            TablesModified: 1,
            ColumnsAdded: 1,
            ColumnsRemoved: 1,
            ColumnsModified: 1,
            IndexesChanged: 2));
    }

    /// <summary>
    /// Tests that passing a null diff throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void GetDiffStatistics_WithNullDiff_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var act = () => service.GetDiffStatistics(null!);
        act.Should().ThrowExactly<ArgumentNullException>();
    }

    private static MigrationDiffService CreateService()
    {
        return new MigrationDiffService(
            new Mock<MigrationRepository>().Object,
            new Mock<ConflictDetectionService>(NullLogger<ConflictDetectionService>.Instance).Object,
            new Mock<SchemaChangeDetectorService>().Object,
            NullLogger<MigrationDiffService>.Instance);
    }

    private static SchemaChange CreateChange(SqlChangeType type, string tableName, string columnName = "")
    {
        return new SchemaChange("20260101000000_Test", type, "SQL")
        {
            TableName = tableName,
            ColumnName = columnName
        };
    }
}
