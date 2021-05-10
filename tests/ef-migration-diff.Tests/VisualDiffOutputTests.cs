#nullable enable
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Unit tests for the visual diff output feature (SchemaDiffEngine and VisualDiffFormatter).
/// </summary>
public class VisualDiffOutputTests
{
    private readonly SchemaChangeDetectorService _detector = new();
    private readonly ConflictDetectionService    _conflictDetection = new(NullLogger<ConflictDetectionService>.Instance);

    private SchemaDiffEngine CreateEngine() => new(_conflictDetection, NullLogger<SchemaDiffEngine>.Instance);

    // =========================================================================
    // SchemaDiffEngine — two-way diff
    // =========================================================================

    [Fact]
    public void ComputeDiff_WithIdenticalChanges_ReturnsIdenticalResult()
    {
        // Arrange
        var engine = CreateEngine();
        var changes = new List<SchemaChange>
        {
            new("mig1", SqlChangeType.CreateTable, "CREATE TABLE Users (Id INT)")
            { TableName = "Users" }
        };

        // Act
        var result = engine.ComputeDiff(changes, changes);

        // Assert
        result.Should().NotBeNull();
        result.IsIdentical.Should().BeTrue();
        result.SourceOnlyChanges.Should().BeEmpty();
        result.TargetOnlyChanges.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_WithSourceOnlyChange_PopulatesSourceOnlyList()
    {
        // Arrange
        var engine = CreateEngine();
        var sourceChanges = new List<SchemaChange>
        {
            new("mig_src", SqlChangeType.CreateTable, "CREATE TABLE Orders (Id INT)")
            { TableName = "Orders" },
            new("mig_src", SqlChangeType.CreateTable, "CREATE TABLE Users (Id INT)")
            { TableName = "Users" }
        };
        var targetChanges = new List<SchemaChange>
        {
            new("mig_tgt", SqlChangeType.CreateTable, "CREATE TABLE Users (Id INT)")
            { TableName = "Users" }
        };

        // Act
        var result = engine.ComputeDiff(sourceChanges, targetChanges);

        // Assert
        result.IsIdentical.Should().BeFalse();
        result.SourceOnlyChanges.Should().ContainSingle();
        result.SourceOnlyChanges[0].TableName.Should().Be("Orders");
    }

    [Fact]
    public void ComputeDiff_WithTargetOnlyChange_PopulatesTargetOnlyList()
    {
        // Arrange
        var engine = CreateEngine();
        var sourceChanges = new List<SchemaChange>
        {
            new("mig_src", SqlChangeType.CreateTable, "CREATE TABLE Users (Id INT)")
            { TableName = "Users" }
        };
        var targetChanges = new List<SchemaChange>
        {
            new("mig_tgt", SqlChangeType.CreateTable, "CREATE TABLE Users (Id INT)")
            { TableName = "Users" },
            new("mig_tgt", SqlChangeType.AddColumn, "ALTER TABLE Users ADD Email NVARCHAR(255)")
            { TableName = "Users", ColumnName = "Email" }
        };

        // Act
        var result = engine.ComputeDiff(sourceChanges, targetChanges);

        // Assert
        result.TargetOnlyChanges.Should().ContainSingle();
        result.TargetOnlyChanges[0].ColumnName.Should().Be("Email");
    }

    [Fact]
    public void ComputeDiff_WithDestructiveChange_ReportsDestructive()
    {
        // Arrange
        var engine = CreateEngine();
        var sourceChanges = new List<SchemaChange>
        {
            new("mig_src", SqlChangeType.DropTable, "DROP TABLE LegacyData")
            { TableName = "LegacyData" }
        };

        // Act
        var result = engine.ComputeDiff(sourceChanges, new List<SchemaChange>());

        // Assert
        result.HasDestructiveChanges.Should().BeTrue();
    }

    [Fact]
    public void ComputeDiff_WithEmptyInputs_ReturnsIdentical()
    {
        // Arrange
        var engine = CreateEngine();

        // Act
        var result = engine.ComputeDiff(
            new List<SchemaChange>(),
            new List<SchemaChange>());

        // Assert
        result.IsIdentical.Should().BeTrue();
        result.TotalAdded.Should().Be(0);
        result.TotalRemoved.Should().Be(0);
    }

    // =========================================================================
    // SchemaDiffEngine — IMergeEditor
    // =========================================================================

    [Fact]
    public void AcceptSource_BuildsPlanWithAllSourceResolutions()
    {
        // Arrange
        var engine      = CreateEngine();
        var conflictId  = Guid.NewGuid();
        var threeWayDiff = new ThreeWayDiffResult
        {
            Id             = Guid.NewGuid(),
            BaseLabel      = "base",
            SourceLabel    = "source",
            TargetLabel    = "target",
            BaseToSource   = MakeEmptyDiff("base", "source"),
            BaseToTarget   = MakeEmptyDiff("base", "target"),
            ConflictRegions = new[]
            {
                new MergeConflictRegion
                {
                    Id          = conflictId,
                    HunkIndex   = 0,
                    Description = "test conflict"
                }
            }
        };

        // Act
        var plan = engine.AcceptSource(threeWayDiff);

        // Assert
        plan.Resolutions.Should().ContainKey(conflictId);
        plan.Resolutions[conflictId].Should().Be(MergeResolutionStrategy.AcceptSource);
    }

    [Fact]
    public void AutoMerge_WithTriviallyResolvableConflicts_ResolvesAll()
    {
        // Arrange
        var engine     = CreateEngine();
        var conflictId = Guid.NewGuid();
        var sharedLine = new DiffLine(DiffLineKind.Modified, 1, "same content");

        var region = new MergeConflictRegion
        {
            Id          = conflictId,
            HunkIndex   = 0,
            Description = "trivial",
            SourceLines = new[] { sharedLine },
            TargetLines = new[] { sharedLine }
        };

        var threeWayDiff = new ThreeWayDiffResult
        {
            Id              = Guid.NewGuid(),
            BaseLabel       = "base",
            SourceLabel     = "source",
            TargetLabel     = "target",
            BaseToSource    = MakeEmptyDiff("base", "source"),
            BaseToTarget    = MakeEmptyDiff("base", "target"),
            ConflictRegions = new[] { region }
        };

        // Act
        var plan = engine.AutoMerge(threeWayDiff);

        // Assert
        plan.Resolutions.Should().ContainKey(conflictId);
        plan.Resolutions[conflictId].Should().NotBe(MergeResolutionStrategy.Unresolved);
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    private static SchemaDiffResult MakeEmptyDiff(string source, string target) =>
        new SchemaDiffResult
        {
            Id          = Guid.NewGuid(),
            SourceLabel = source,
            TargetLabel = target
        };
}
