#nullable enable

using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using FluentAssertions;
using Xunit;

namespace EfMigrationDiff.Tests;

public class ConflictDetectionServiceTests
{
    private readonly ConflictDetectionService _service;

    public ConflictDetectionServiceTests()
    {
        _service = new ConflictDetectionService();
    }

    [Fact]
    public void DetectConflicts_WithNoChanges_ReturnsEmptyList()
    {
        var sourceChanges = new List<SchemaChange>();
        var targetChanges = new List<SchemaChange>();

        var conflicts = _service.DetectConflicts(sourceChanges, targetChanges);

        conflicts.Should().BeEmpty();
    }

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
}
