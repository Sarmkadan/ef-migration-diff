#nullable enable

using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using FluentAssertions;
using System.Text.Json;

namespace EfMigrationDiff.Tests;

public class ReportGenerationServiceTests
{
    private readonly ReportGenerationService _reportService = new();

    [Fact]
    public void GenerateTextReport_WithDiffContainingConflicts_IncludesConflictSummary()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");
        var conflict = new ConflictInfo("mig1", "mig2", ConflictType.TableConflict)
        {
            Description = "Table [Users] already exists"
        };
        diff.AddConflict(conflict);

        // Act
        var report = _reportService.GenerateTextReport(diff);

        // Assert
        report.Should().Contain("EF Migration Diff Report");
        report.Should().Contain("Users");
        report.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateTextReport_WithMultipleMigrations_IncludesMigrationSummary()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");
        diff.OnlyInSource.Add(new Migration("20240115093045", "CreateUsers", "AppDbContext"));
        diff.OnlyInTarget.Add(new Migration("20240115093046", "CreateProducts", "AppDbContext"));
        diff.InBoth.Add(new Migration("20240115093044", "Initial", "AppDbContext"));

        // Act
        var report = _reportService.GenerateTextReport(diff);

        // Assert
        report.Should().Contain("CreateUsers");
        report.Should().Contain("CreateProducts");
        report.Should().Contain("Initial");
    }

    [Fact]
    public void GenerateTextReport_WithSchemaChanges_IncludesSchemaChangeSummary()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");
        var change = new SchemaChange("20240115093045", SqlChangeType.CreateTable, "CreateTable(name: \"Users\")")
        {
            TableName = "Users"
        };
        diff.SourceSchemaChanges.Add(change);

        // Act
        var report = _reportService.GenerateTextReport(diff);

        // Assert
        report.Should().Contain("Users");
        report.Should().Contain("CreateTable");
    }

    [Fact]
    public void GenerateTextReport_WithNoIssues_ReportsCleanComparison()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");

        // Act
        var report = _reportService.GenerateTextReport(diff);

        // Assert
        report.Should().NotBeEmpty();
        report.Should().Contain("EF Migration Diff Report");
    }

    [Fact]
    public void GenerateJsonReport_ProducesValidJson()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");
        diff.OnlyInSource.Add(new Migration("20240115093045", "CreateUsers", "AppDbContext"));

        // Act
        var report = _reportService.GenerateJsonReport(diff);

        // Assert
        var act = () => JsonDocument.Parse(report);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateJsonReport_IncludesAllMigrationCategories()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");
        diff.OnlyInSource.Add(new Migration("20240115093045", "SourceOnly", "AppDbContext"));
        diff.OnlyInTarget.Add(new Migration("20240115093046", "TargetOnly", "AppDbContext"));
        diff.InBoth.Add(new Migration("20240115093044", "Common", "AppDbContext"));

        // Act
        var report = _reportService.GenerateJsonReport(diff);
        var jsonDoc = JsonDocument.Parse(report);

        // Assert
        jsonDoc.RootElement.TryGetProperty("Migrations", out var migrations).Should().BeTrue();
        migrations.TryGetProperty("SourceOnly", out _).Should().BeTrue();
        migrations.TryGetProperty("TargetOnly", out _).Should().BeTrue();
        migrations.TryGetProperty("Common", out _).Should().BeTrue();
    }

    [Fact]
    public void GenerateJsonReport_IncludesConflicts()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");
        var conflict = new ConflictInfo("mig1", "mig2", ConflictType.TableConflict)
        {
            Description = "Table conflict",
            Severity = ConflictSeverity.Error
        };
        diff.AddConflict(conflict);

        // Act
        var report = _reportService.GenerateJsonReport(diff);
        var jsonDoc = JsonDocument.Parse(report);

        // Assert
        jsonDoc.RootElement.TryGetProperty("Conflicts", out var conflicts).Should().BeTrue();
        conflicts.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void GenerateJsonReport_IncludesSchemaChanges()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");
        var change = new SchemaChange("20240115093045", SqlChangeType.CreateTable, "CreateTable(...)")
        {
            TableName = "Users"
        };
        diff.SourceSchemaChanges.Add(change);

        // Act
        var report = _reportService.GenerateJsonReport(diff);
        var jsonDoc = JsonDocument.Parse(report);

        // Assert
        jsonDoc.RootElement.TryGetProperty("SchemaChanges", out var schemaChanges).Should().BeTrue();
    }

    [Fact]
    public void GenerateHtmlReport_ProducesValidHtml()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");
        diff.OnlyInSource.Add(new Migration("20240115093045", "CreateUsers", "AppDbContext"));

        // Act
        var report = _reportService.GenerateHtmlReport(diff);

        // Assert
        report.Should().Contain("<!DOCTYPE");
        report.Should().Contain("html");
        report.Should().Contain("CreateUsers");
    }

    [Fact]
    public void GenerateConflictSummary_WithConflicts_IncludesAllConflictDetails()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");
        var conflict = new ConflictInfo("mig1", "mig2", ConflictType.NameConflict)
        {
            Description = "Name conflict detected",
            Severity = ConflictSeverity.Error
        };
        diff.AddConflict(conflict);

        // Act
        var report = _reportService.GenerateConflictSummary(diff);

        // Assert
        report.Should().NotBeEmpty();
        report.Should().Contain("Name conflict detected");
        report.Should().Contain("CONFLICT ANALYSIS");
    }

    [Fact]
    public void GenerateConflictSummary_WithNoConflicts_ReturnsNoConflictsMessage()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");

        // Act
        var report = _reportService.GenerateConflictSummary(diff);

        // Assert
        report.Should().Contain("No conflicts detected");
    }

    [Fact]
    public void GenerateReport_WithDifferentFormats_AllProduceSomeOutput()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");
        diff.OnlyInSource.Add(new Migration("20240115093045", "Migration1", "AppDbContext"));

        // Act
        var textReport = _reportService.GenerateTextReport(diff);
        var jsonReport = _reportService.GenerateJsonReport(diff);
        var htmlReport = _reportService.GenerateHtmlReport(diff);

        // Assert
        textReport.Should().NotBeEmpty();
        jsonReport.Should().NotBeEmpty();
        htmlReport.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateJsonReport_WithDestructiveChanges_IncludesDestructiveChanges()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");
        var dropTableChange = new SchemaChange("mig1", SqlChangeType.DropTable, "DropTable(name: \"Users\")")
        {
            TableName = "Users"
        };
        diff.SourceSchemaChanges.Add(dropTableChange);

        // Act
        var report = _reportService.GenerateJsonReport(diff);

        // Assert
        report.Should().Contain("DropTable");
    }

    [Fact]
    public void GenerateTextReport_IncludesTimestamp()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");

        // Act
        var report = _reportService.GenerateTextReport(diff);

        // Assert
        report.Should().Contain("Generated:");
        report.Should().Contain("UTC");
    }

    [Fact]
    public void GenerateHtmlReport_WithMultipleConflicts_CreatesProperTable()
    {
        // Arrange
        var diff = new MigrationDiff("branch1", "branch2");
        for (int i = 0; i < 3; i++)
        {
            var conflict = new ConflictInfo($"mig{i}", $"mig{i+1}", ConflictType.TableConflict)
            {
                Description = $"Conflict {i}",
                Severity = ConflictSeverity.Error
            };
            diff.AddConflict(conflict);
        }

        // Act
        var report = _reportService.GenerateHtmlReport(diff);

        // Assert
        report.Should().Contain("<table>");
        report.Should().Contain("Conflicts");
        report.Should().Contain("Conflict 0");
        report.Should().Contain("Conflict 1");
        report.Should().Contain("Conflict 2");
    }
}
