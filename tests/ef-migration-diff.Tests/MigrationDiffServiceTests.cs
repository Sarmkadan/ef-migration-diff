#nullable enable

using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using EfMigrationDiff.Repositories;
using FluentAssertions;

namespace EfMigrationDiff.Tests;

public class MigrationDiffServiceTests
{
    [Fact]
    public void CompareBranches_WithNullSourceBranch_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<MigrationRepository>();
        var mockConflictDetector = new Mock<ConflictDetectionService>();
        var mockSchemaDetector = new Mock<SchemaChangeDetectorService>();
        var service = new MigrationDiffService(mockRepository.Object, mockConflictDetector.Object, mockSchemaDetector.Object);

        var targetBranch = new BranchInfo("main", "abc123");

        // Act & Assert
        var act = () => service.CompareBranches(null!, targetBranch);
        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void CompareBranches_WithNullTargetBranch_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<MigrationRepository>();
        var mockConflictDetector = new Mock<ConflictDetectionService>();
        var mockSchemaDetector = new Mock<SchemaChangeDetectorService>();
        var service = new MigrationDiffService(mockRepository.Object, mockConflictDetector.Object, mockSchemaDetector.Object);

        var sourceBranch = new BranchInfo("feature", "def456");

        // Act & Assert
        var act = () => service.CompareBranches(sourceBranch, null!);
        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void CompareBranches_WithIdenticalMigrations_CategorizesCorrectly()
    {
        // Arrange
        var repository = new MigrationRepository();
        var conflictDetector = new ConflictDetectionService();
        var schemaDetector = new SchemaChangeDetectorService();
        var service = new MigrationDiffService(repository, conflictDetector, schemaDetector);

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

    [Fact]
    public void CompareBranches_WithSourceOnlyMigration_CategorizesCorrectly()
    {
        // Arrange
        var repository = new MigrationRepository();
        var conflictDetector = new ConflictDetectionService();
        var schemaDetector = new SchemaChangeDetectorService();
        var service = new MigrationDiffService(repository, conflictDetector, schemaDetector);

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

    [Fact]
    public void CompareBranches_WithTargetOnlyMigration_CategorizesCorrectly()
    {
        // Arrange
        var repository = new MigrationRepository();
        var conflictDetector = new ConflictDetectionService();
        var schemaDetector = new SchemaChangeDetectorService();
        var service = new MigrationDiffService(repository, conflictDetector, schemaDetector);

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

    [Fact]
    public void CompareBranches_DetectsSchemaChanges()
    {
        // Arrange
        var repository = new MigrationRepository();
        var conflictDetector = new ConflictDetectionService();
        var schemaDetector = new SchemaChangeDetectorService();
        var service = new MigrationDiffService(repository, conflictDetector, schemaDetector);

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

    [Fact]
    public void CompareBranches_WithEmptyBranches_ReturnsEmptyDiff()
    {
        // Arrange
        var repository = new MigrationRepository();
        var conflictDetector = new ConflictDetectionService();
        var schemaDetector = new SchemaChangeDetectorService();
        var service = new MigrationDiffService(repository, conflictDetector, schemaDetector);

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
}
