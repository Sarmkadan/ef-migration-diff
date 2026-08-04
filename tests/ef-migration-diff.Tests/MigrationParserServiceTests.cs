#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using Xunit;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Additional unit tests for <see cref="MigrationParserService"/>.
    /// These tests focus on extracting Up/Down SQL operations and handling malformed inputs.
    /// </summary>
    public class MigrationParserServiceTests : IDisposable
    {
        private readonly MigrationParserService _parser = new();
        private readonly List<string> _tempFiles = new();

        public void Dispose()
        {
            // Clean up any temporary files created during the tests
            foreach (var file in _tempFiles)
            {
                try
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
                catch
                {
                    // ignore cleanup errors
                }
            }
        }

        private string CreateTempMigrationFile(string fileName, string content)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{fileName}");
            File.WriteAllText(tempPath, content);
            _tempFiles.Add(tempPath);
            return tempPath;
        }

        private MigrationFile BuildMigrationFile(string fileName, string dbContextName, string content)
        {
            var path = CreateTempMigrationFile(fileName, content);
            var migrationFile = new MigrationFile
            {
                FileName = path,
                DbContextName = dbContextName
            };
            // Load the content from the temporary file.
            migrationFile.LoadContentAsync().GetAwaiter().GetResult();
            return migrationFile;
        }

        [Fact]
        public void ParseMigrationFile_ValidFile_ReturnsMigrationWithCorrectIdAndName()
        {
            // Arrange
            const string fileName = "20230101120000_AddUserTable.cs";
            const string dbContext = "MyDbContext";
            const string content = @"
using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddUserTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: ""Users"",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation(""SqlServer:Identity"", ""1, 1""),
                Name = table.Column<string>(nullable: true)
            },
            constraints: table => { table.PrimaryKey(""PK_Users"", x => x.Id); });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: ""Users"");
    }
}
";

            var migrationFile = BuildMigrationFile(fileName, dbContext, content);

            // Act
            var migration = _parser.ParseMigrationFile(migrationFile);

            // Assert
            Assert.NotNull(migration);
            Assert.Equal("20230101120000", migration!.Id);
            Assert.Equal("AddUserTable", migration.Name);
            Assert.Equal(dbContext, migration.DbContextName);
        }

        [Fact]
        public void ExtractSqlOperations_ContainsSqlCalls_ReturnsAllOperations()
        {
            // Arrange
            const string fileName = "20230101120001_AddRawSql.cs";
            const string dbContext = "MyDbContext";
            const string content = @"
using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddRawSql : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(""CREATE INDEX IX_Users_Name ON Users(Name)"");
        migrationBuilder.Sql(""UPDATE Users SET Name = 'Anonymous' WHERE Name IS NULL"");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(""DROP INDEX IX_Users_Name ON Users"");
    }
}
";

            var migrationFile = BuildMigrationFile(fileName, dbContext, content);
            var migration = _parser.ParseMigrationFile(migrationFile)!;

            // Act
            var sqlOps = _parser.ExtractSqlOperations(migration);

            // Assert
            Assert.Equal(3, sqlOps.Count); // two in Up, one in Down
            Assert.All(sqlOps, op => Assert.Contains("migrationBuilder.Sql", op));
        }

        [Fact]
        public void GetMigrationDependencies_WithAnnotation_ReturnsDependency()
        {
            // Arrange
            const string fileName = "20230101120002_Dependency.cs";
            const string dbContext = "MyDbContext";
            const string content = @"
using Microsoft.EntityFrameworkCore.Migrations;

public partial class DependentMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // No operations
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // No operations
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation(""EfMigrationDiff:DependsOn"", ""20230101120000_AddUserTable"");
    }
}
";

            var migrationFile = BuildMigrationFile(fileName, dbContext, content);
            var migration = _parser.ParseMigrationFile(migrationFile)!;

            // Act
            var deps = _parser.GetMigrationDependencies(migration);

            // Assert
            Assert.Single(deps);
            Assert.Equal("20230101120000_AddUserTable", deps[0]);
        }

        [Fact]
        public void ValidateMigrationFile_ValidFile_ReturnsEmptyErrorList()
        {
            // Arrange
            const string fileName = "20230101120003_Valid.cs";
            const string dbContext = "MyDbContext";
            const string content = @"
using Microsoft.EntityFrameworkCore.Migrations;

public partial class ValidMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) { }
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
";

            var migrationFile = BuildMigrationFile(fileName, dbContext, content);

            // Act
            var errors = _parser.ValidateMigrationFile(migrationFile);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void ValidateMigrationFile_MalformedFile_ReturnsExpectedErrors()
        {
            // Arrange: missing class declaration and Up method
            const string fileName = "BadFile.cs";
            const string dbContext = "MyDbContext";
            const string content = @"
using Microsoft.EntityFrameworkCore.Migrations;

// Missing class and Up method
public class NotAPartialMigration
{
    // No Up method
}
";

            var migrationFile = BuildMigrationFile(fileName, dbContext, content);

            // Act
            var errors = _parser.ValidateMigrationFile(migrationFile);

            // Assert
            Assert.Contains(errors, e => e.Contains("Missing 'public partial class'"));
            Assert.Contains(errors, e => e.Contains("Missing Up method"));
            Assert.Contains(errors, e => e.Contains("Should contain exactly one public partial class"));
        }
    }
}
