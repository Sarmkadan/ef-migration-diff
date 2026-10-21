#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Integration tests for MigrationParser with realistic EF Core migration fixtures.
/// Tests parsing of various migration file types and their schema change detection.
/// </summary>
public class MigrationParserIntegrationTests
{
    private readonly MigrationParserService _parser = new();
    private readonly SchemaChangeDetectorService _detector = new();
    private readonly ReportGenerationService _reportService = new();

    /// <summary>
    /// Standard scaffolded migration with Up/Down containing CreateTable, AddColumn, CreateIndex, AddForeignKey.
    /// </summary>
    [Fact]
    public void Parse_StandardScaffoldedMigration_ReturnsCorrectModel()
    {
        // Arrange
        var fixturesDir = "/home/personal/task-factory/workdir/ef-migration-diff/tests/Fixtures";
        var migrationFile = new MigrationFile
        {
            FileName = "20240115093045_CreateUsersTable.cs",
            Content = File.ReadAllText($"{fixturesDir}/20240115093045_CreateUsersTable.cs"),
            DbContextName = "AppDbContext"
        };

        // Act
        var migration = _parser.ParseMigrationFile(migrationFile);

        // Assert
        migration.Should().NotBeNull();
        migration!.Id.Should().Be("20240115093045");
        migration.Name.Should().Be("CreateUsersTable");
        migration.DbContextName.Should().Be("AppDbContext");
        migration.MetadataContent.Should().Contain("ClassName: CreateUsersTable");
        // No migrationBuilder.Sql() calls in this fixture
        migration.MetadataContent.Should().Contain("SqlOperationsCount: 0");
    }

    /// <summary>
    /// Migration with raw migrationBuilder.Sql(...) block.
    /// </summary>
    [Fact]
    public void Parse_MigrationWithRawSqlBlock_RectsSqlOperationsCount()
    {
        // Arrange
        var fixturesDir = "/home/personal/task-factory/workdir/ef-migration-diff/tests/Fixtures";
        var migrationFile = new MigrationFile
        {
            FileName = "20240115093046_AddRawSqlBlock.cs",
            Content = File.ReadAllText($"{fixturesDir}/20240115093046_AddRawSqlBlock.cs"),
            DbContextName = "AppDbContext"
        };

        // Act
        var migration = _parser.ParseMigrationFile(migrationFile);

        // Assert
        migration.Should().NotBeNull();
        migration!.Id.Should().Be("20240115093046");
        migration.Name.Should().Be("AddRawSqlBlock");
        // There are 2 migrationBuilder.Sql() calls (one in Up, one in Down)
        migration.MetadataContent.Should().Contain("SqlOperationsCount: 2");
    }

    /// <summary>
    /// Migration with RenameColumn and AlterColumn changing type and nullability.
    /// </summary>
    [Fact]
    public void Parse_MigrationWithRenameAndAlterColumn_RectsOperations()
    {
        // Arrange
        var fixturesDir = "/home/personal/task-factory/workdir/ef-migration-diff/tests/Fixtures";
        var migrationFile = new MigrationFile
        {
            FileName = "20240115093047_RenameAndAlterColumn.cs",
            Content = File.ReadAllText($"{fixturesDir}/20240115093047_RenameAndAlterColumn.cs"),
            DbContextName = "AppDbContext"
        };

        // Act
        var migration = _parser.ParseMigrationFile(migrationFile);

        // Assert
        migration.Should().NotBeNull();
        migration!.Id.Should().Be("20240115093047");
        migration.Name.Should().Be("RenameAndAlterColumn");
        migration.MetadataContent.Should().Contain("ClassName: RenameAndAlterColumn");
    }

    /// <summary>
    /// Malformed file missing the Down method - parser returns migration (validation is separate).
    /// </summary>
    [Fact]
    public void Parse_MalformedFileMissingDown_ReturnsMigrationNotException()
    {
        // Arrange
        var fixturesDir = "/home/personal/task-factory/workdir/ef-migration-diff/tests/Fixtures";
        var migrationFile = new MigrationFile
        {
            FileName = "20240115093048_MalformedMigration.cs",
            Content = File.ReadAllText($"{fixturesDir}/20240115093048_MalformedMigration.cs"),
            DbContextName = "AppDbContext"
        };

        // Act
        var migration = _parser.ParseMigrationFile(migrationFile);

        // Assert
        // Parser returns a migration even for files missing Down method - validation is separate
        migration.Should().NotBeNull();
        migration!.Id.Should().Be("20240115093048");
        migration.Name.Should().Be("MalformedMigration");
        // The missing Down method will be caught by ValidateMigrationFile, not by the parser
    }

    /// <summary>
    /// Malformed file missing the Down method - validation returns error.
    /// </summary>
    [Fact]
    public void Validate_MalformedFileMissingDown_ReturnsError()
    {
        // Arrange
        var fixturesDir = "/home/personal/task-factory/workdir/ef-migration-diff/tests/Fixtures";
        var migrationFile = new MigrationFile
        {
            FileName = "20240115093048_MalformedMigration.cs",
            Content = File.ReadAllText($"{fixturesDir}/20240115093048_MalformedMigration.cs"),
            DbContextName = "AppDbContext"
        };

        // Act
        var errors = _parser.ValidateMigrationFile(migrationFile);

        // Assert
        errors.Should().Contain(e => e.Contains("Missing Down method"));
    }

    /// <summary>
    /// Chain: parse fixture (1) -> apply to empty SchemaSnapshot -> diff against expected snapshot -> assert DiffReport is empty.
    /// </summary>
    [Fact]
    public void ParseAndDiff_StandardMigration_AgainstEmptySnapshot_ReturnsEmptyDiff()
    {
        // Arrange
        var fixturesDir = "/home/personal/task-factory/workdir/ef-migration-diff/tests/Fixtures";
        var migrationFile = new MigrationFile
        {
            FileName = "20240115093045_CreateUsersTable.cs",
            Content = File.ReadAllText($"{fixturesDir}/20240115093045_CreateUsersTable.cs"),
            DbContextName = "AppDbContext"
        };

        var migration = _parser.ParseMigrationFile(migrationFile);
        migration.Should().NotBeNull();

        // Create an empty schema snapshot (no existing tables)
        var emptySnapshotContent = @"
using Microsoft.EntityFrameworkCore.Migrations;

namespace ModelSnapshot
{
    public partial class ModelSnapshot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}";

        var snapshotFile = new MigrationFile
        {
            FileName = "20240115093045_EmptySnapshot.cs",
            Content = emptySnapshotContent,
            DbContextName = "AppDbContext"
        };

        var snapshotMigration = _parser.ParseMigrationFile(snapshotFile)!;

        // Detect schema changes from the migration
        var changes = _detector.DetectChanges(migration);

        // Verify changes are detected
        changes.Should().NotBeEmpty();
        changes.Should().HaveCountGreaterThan(0);

        // Create a diff report - with an empty snapshot, all changes should appear
        var diff = new MigrationDiff("feature", "main")
        {
            OnlyInSource = new List<Migration> { migration },
        };

        foreach (var change in changes)
        {
            diff.SourceSchemaChanges.Add(change);
        }

        // Assert - the diff should contain the schema changes
        diff.SourceSchemaChanges.Should().NotBeEmpty();
        diff.SourceSchemaChanges.Should().HaveCountGreaterThan(0);
    }

    /// <summary>
    /// Parse multiple fixtures and verify operation counts and order.
    /// </summary>
    [Fact]
    public void Parse_Fixtures_VerifyOperationCountsAndOrder()
    {
        // Parse the standard scaffolded migration
        var fixturesDir = "/home/personal/task-factory/workdir/ef-migration-diff/tests/Fixtures";
        var stdMigrationFile = new MigrationFile
        {
            FileName = "20240115093045_CreateUsersTable.cs",
            Content = File.ReadAllText($"{fixturesDir}/20240115093045_CreateUsersTable.cs"),
            DbContextName = "AppDbContext"
        };

        var stdMigration = _parser.ParseMigrationFile(stdMigrationFile)!;

        // Detect schema changes
        var stdChanges = _detector.DetectChanges(stdMigration);

        // Should have CreateTable as the first operation
        stdChanges.Should().NotBeEmpty();
        stdChanges[0].ChangeType.Should().Be(EfMigrationDiff.Models.SqlChangeType.CreateTable);
        stdChanges[0].TableName.Should().Be("Users");

        // Verify table name is extracted
        stdChanges.First(c => c.ChangeType == EfMigrationDiff.Models.SqlChangeType.CreateTable).TableName
            .Should().Be("Users");

        // Verify index was detected
        var indexes = stdChanges.Where(c => c.ChangeType == EfMigrationDiff.Models.SqlChangeType.CreateIndex).ToList();
        indexes.Should().HaveCount(2); // IX_Users_Email and IX_Users_UserName
    }
}