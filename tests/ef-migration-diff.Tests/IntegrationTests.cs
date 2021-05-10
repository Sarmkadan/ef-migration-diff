#nullable enable

using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using EfMigrationDiff.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EfMigrationDiff.Tests;

public class IntegrationTests
{
    [Fact]
    public void EndToEnd_ParseParseCompareAndReport_CompletesSuccessfully()
    {
        // Arrange
        var parser = new MigrationParserService();
        var detector = new SchemaChangeDetectorService();
        var conflictDetector = new ConflictDetectionService(NullLogger<ConflictDetectionService>.Instance);
        var reportService = new ReportGenerationService();

        var sourceFile = new MigrationFile
        {
            FileName = "20240115093045_CreateUsersTable.cs",
            Content = @"migrationBuilder.CreateTable(name: ""Users"", columns: table => new { Id = table.Column<int>() });",
            DbContextName = "AppDbContext"
        };

        var targetFile = new MigrationFile
        {
            FileName = "20240115093044_InitialCreate.cs",
            Content = @"migrationBuilder.CreateTable(name: ""Products"", columns: table => new { Id = table.Column<int>() });",
            DbContextName = "AppDbContext"
        };

        // Act
        var sourceMigration = parser.ParseMigrationFile(sourceFile)!;
        var targetMigration = parser.ParseMigrationFile(targetFile)!;

        var sourceChanges = detector.DetectChanges(sourceMigration);
        var targetChanges = detector.DetectChanges(targetMigration);

        var conflicts = conflictDetector.DetectConflicts(sourceChanges, targetChanges);

        var diff = new MigrationDiff("feature", "main")
        {
            OnlyInSource = new List<Migration> { sourceMigration },
            OnlyInTarget = new List<Migration> { targetMigration },
        };
        foreach (var conflict in conflicts)
            diff.AddConflict(conflict);

        var report = reportService.GenerateTextReport(diff);

        // Assert
        sourceMigration.Should().NotBeNull();
        sourceMigration.Id.Should().Be("20240115093045");
        targetMigration.Should().NotBeNull();
        targetMigration.Id.Should().Be("20240115093044");
        sourceChanges.Should().NotBeEmpty();
        targetChanges.Should().NotBeEmpty();
        report.Should().NotBeEmpty();
    }

    [Fact]
    public void FullWorkflow_MultipleDbContexts_HandlesCorrectly()
    {
        // Arrange
        var parser = new MigrationParserService();
        var repository = new MigrationRepository();

        var files = new List<MigrationFile>
        {
            new MigrationFile
            {
                FileName = "20240115093045_CreateUsers.cs",
                Content = @"migrationBuilder.CreateTable(name: ""Users"")",
                DbContextName = "AppDbContext"
            },
            new MigrationFile
            {
                FileName = "20240115093046_CreateProducts.cs",
                Content = @"migrationBuilder.CreateTable(name: ""Products"")",
                DbContextName = "SalesDbContext"
            }
        };

        // Act
        var migrations = new List<Migration>();
        foreach (var file in files)
        {
            var migration = parser.ParseMigrationFile(file)!;
            repository.Add(migration);
            migrations.Add(migration);
        }

        var appDbContextMigrations = repository.GetByDbContext("AppDbContext");
        var salesDbContextMigrations = repository.GetByDbContext("SalesDbContext");

        // Assert
        appDbContextMigrations.Should().HaveCount(1);
        appDbContextMigrations[0].DbContextName.Should().Be("AppDbContext");
        salesDbContextMigrations.Should().HaveCount(1);
        salesDbContextMigrations[0].DbContextName.Should().Be("SalesDbContext");
    }

    [Fact]
    public void ConcurrentMigrationProcessing_MultipleThreadsProcessDifferentMigrations_AllProcessed()
    {
        // Arrange
        var parser = new MigrationParserService();
        var repository = new MigrationRepository();
        var detector = new SchemaChangeDetectorService();

        const int migrationCount = 20;
        var threads = new List<Thread>();
        var parsedMigrations = new List<Migration>();
        var lockObj = new object();

        // Act
        for (int i = 0; i < migrationCount; i++)
        {
            var index = i;
            var thread = new Thread(() =>
            {
                var file = new MigrationFile
                {
                    FileName = $"202401150930{index:D2}_Migration{index}.cs",
                    Content = $@"migrationBuilder.CreateTable(name: ""Table{index}"")",
                    DbContextName = "AppDbContext"
                };

                var migration = parser.ParseMigrationFile(file);
                if (migration != null)
                {
                    repository.Add(migration);
                    var changes = detector.DetectChanges(migration);

                    lock (lockObj)
                    {
                        parsedMigrations.Add(migration);
                    }
                }
            });

            threads.Add(thread);
            thread.Start();
        }

        foreach (var thread in threads)
            thread.Join();

        // Assert
        parsedMigrations.Should().HaveCount(migrationCount);
        repository.GetByDbContext("AppDbContext").Should().HaveCount(migrationCount);
    }

    [Fact]
    public void ReportGeneration_WithDifferentFormats_AllFormatsProduceConsistentData()
    {
        // Arrange
        var diff = new MigrationDiff("feature", "main");
        diff.OnlyInSource.Add(new Migration("20240115093045", "CreateUsers", "AppDbContext"));
        diff.OnlyInTarget.Add(new Migration("20240115093046", "CreateProducts", "AppDbContext"));
        diff.InBoth.Add(new Migration("20240115093044", "Initial", "AppDbContext"));

        var reportService = new ReportGenerationService();

        // Act
        var textReport = reportService.GenerateTextReport(diff);
        var jsonReport = reportService.GenerateJsonReport(diff);
        var htmlReport = reportService.GenerateHtmlReport(diff);

        // Assert
        textReport.Should().Contain("CreateUsers");
        textReport.Should().Contain("CreateProducts");
        textReport.Should().Contain("Initial");

        jsonReport.Should().Contain("CreateUsers");
        jsonReport.Should().Contain("CreateProducts");
        jsonReport.Should().Contain("Initial");

        htmlReport.Should().Contain("CreateUsers");
        htmlReport.Should().Contain("CreateProducts");
        htmlReport.Should().Contain("Initial");
    }

    [Fact]
    public void SchemaChangeDetectionPipeline_ComplexMigration_DetectsAllOperations()
    {
        // Arrange
        var complexContent = @"
            migrationBuilder.CreateTable(
                name: ""Users"",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    Email = table.Column<string>(nullable: false)
                });

            migrationBuilder.CreateIndex(
                name: ""IX_Users_Email"",
                table: ""Users"",
                column: ""Email"",
                unique: true);

            migrationBuilder.AddColumn<string>(
                name: ""Phone"",
                table: ""Users"",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: ""Email"",
                table: ""Users"",
                nullable: false);
        ";

        var migration = new Migration("20240115093045", "CreateUsersTable", "AppDbContext")
        {
            Content = complexContent
        };

        var detector = new SchemaChangeDetectorService();

        // Act
        var changes = detector.DetectChanges(migration);

        // Assert
        changes.Should().HaveCount(4);
        changes.Should().Contain(c => c.ChangeType == SqlChangeType.CreateTable);
        changes.Should().Contain(c => c.ChangeType == SqlChangeType.CreateIndex);
        changes.Should().Contain(c => c.ChangeType == SqlChangeType.AddColumn);
        changes.Should().Contain(c => c.ChangeType == SqlChangeType.ModifyColumn);
    }

    [Fact]
    public void ConflictDetection_WithTableNameConflict_IdentifiesConflict()
    {
        // Arrange
        var detector = new SchemaChangeDetectorService();
        var conflictDetector = new ConflictDetectionService(NullLogger<ConflictDetectionService>.Instance);

        var sourceMigration = new Migration("20240115093045", "CreateUsersV1", "AppDbContext")
        {
            Content = @"migrationBuilder.CreateTable(name: ""Users"", columns: table => new { Id = table.Column<int>() });"
        };

        var targetMigration = new Migration("20240115093046", "CreateUsersV2", "AppDbContext")
        {
            Content = @"migrationBuilder.CreateTable(name: ""Users"", columns: table => new { Id = table.Column<long>() });"
        };

        var sourceChanges = detector.DetectChanges(sourceMigration);
        var targetChanges = detector.DetectChanges(targetMigration);

        // Act
        var conflicts = conflictDetector.DetectConflicts(sourceChanges, targetChanges);

        // Assert
        conflicts.Should().NotBeEmpty();
        conflicts.Should().Contain(c => c.ConflictType == ConflictType.NameConflict);
    }

    [Fact]
    public void MigrationValidation_WithValidAndInvalidMigrations_IdentifiesInvalidOnes()
    {
        // Arrange
        var validMigration = new Migration("20240115093045", "ValidMigration", "AppDbContext");
        var invalidMigration = new Migration(); // Missing required fields

        // Act
        var validResult = validMigration.IsValid();
        var invalidResult = invalidMigration.IsValid();

        // Assert
        validResult.Should().BeTrue();
        invalidResult.Should().BeFalse();
    }

    [Fact]
    public void MultipleDbContextComparison_IndependentContexts_ProcessesWithoutInterference()
    {
        // Arrange
        var parser = new MigrationParserService();
        var detector = new SchemaChangeDetectorService();

        var appDbMigration = new MigrationFile
        {
            FileName = "20240115093045_CreateUsers.cs",
            Content = @"migrationBuilder.CreateTable(name: ""Users"")",
            DbContextName = "AppDbContext"
        };

        var identityDbMigration = new MigrationFile
        {
            FileName = "20240115093045_CreateRoles.cs",
            Content = @"migrationBuilder.CreateTable(name: ""Roles"")",
            DbContextName = "IdentityDbContext"
        };

        // Act
        var appDbMig = parser.ParseMigrationFile(appDbMigration)!;
        var identityDbMig = parser.ParseMigrationFile(identityDbMigration)!;

        var appChanges = detector.DetectChanges(appDbMig);
        var identityChanges = detector.DetectChanges(identityDbMig);

        // Assert
        appDbMig.DbContextName.Should().Be("AppDbContext");
        identityDbMig.DbContextName.Should().Be("IdentityDbContext");
        appChanges.Should().Contain(c => c.TableName == "Users");
        identityChanges.Should().Contain(c => c.TableName == "Roles");
    }

    [Fact]
    public void ReadmeExample_BasicComparison_WorksAsDocumented()
    {
        // This test verifies the main use case described in README:
        // Compare migrations between two branches and detect conflicts

        // Arrange
        var sourceBranch = new BranchInfo("feature/add-notifications", "abc123");
        var targetBranch = new BranchInfo("main", "def456");

        var parser = new MigrationParserService();

        var featureMigration = new MigrationFile
        {
            FileName = "20240115093050_AddNotificationTable.cs",
            Content = @"migrationBuilder.CreateTable(name: ""Notifications"", columns: table => new { Id = table.Column<int>() });",
            DbContextName = "AppDbContext"
        };

        var mainMigration = new MigrationFile
        {
            FileName = "20240115093049_AddLoggingTable.cs",
            Content = @"migrationBuilder.CreateTable(name: ""Logs"", columns: table => new { Id = table.Column<int>() });",
            DbContextName = "AppDbContext"
        };

        // Act
        var featureMig = parser.ParseMigrationFile(featureMigration)!;
        var mainMig = parser.ParseMigrationFile(mainMigration)!;

        var diff = new MigrationDiff(sourceBranch.Id, targetBranch.Id);
        diff.OnlyInSource.Add(featureMig);
        diff.OnlyInTarget.Add(mainMig);

        var detector = new SchemaChangeDetectorService();
        var featureChanges = detector.DetectChanges(featureMig);
        var mainChanges = detector.DetectChanges(mainMig);

        diff.SourceSchemaChanges.AddRange(featureChanges);
        diff.TargetSchemaChanges.AddRange(mainChanges);

        var reportService = new ReportGenerationService();
        var report = reportService.GenerateJsonReport(diff);

        // Assert
        featureMig.Should().NotBeNull();
        mainMig.Should().NotBeNull();
        diff.OnlyInSource.Should().ContainSingle();
        diff.OnlyInTarget.Should().ContainSingle();
        featureChanges.Should().NotBeEmpty();
        mainChanges.Should().NotBeEmpty();
        report.Should().NotBeEmpty();
    }
}
