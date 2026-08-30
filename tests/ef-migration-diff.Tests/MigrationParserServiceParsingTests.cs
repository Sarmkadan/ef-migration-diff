#nullable enable

using System;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using Xunit;

namespace EfMigrationDiff.Tests;

public class MigrationParserServiceParsingTests
{
    private readonly MigrationParserService _parser = new();

    [Fact]
    public void ParseMigrationFile_InvalidMigrationFile_ReturnsNull()
    {
        var migrationFile = new MigrationFile
        {
            Content = "public partial class AddUsers : Migration { }",
            DbContextName = "TestDbContext"
        };

        var result = _parser.ParseMigrationFile(migrationFile);

        Assert.Null(result);
    }

    [Fact]
    public void ParseMigrationFile_FileNameWithoutTimestamp_ReturnsNull()
    {
        var migrationFile = new MigrationFile
        {
            FileName = "AddUsers.cs",
            Content = "public partial class AddUsers : Migration { }",
            DbContextName = "TestDbContext"
        };

        var result = _parser.ParseMigrationFile(migrationFile);

        Assert.Null(result);
    }

    [Fact]
    public void ParseMigrationFile_ValidFileName_ExtractsMigrationIdAndName()
    {
        var migrationFile = new MigrationFile
        {
            FileName = "20240101120000_AddUsers.cs",
            Content = "public partial class AddUsers : Migration { }",
            DbContextName = "TestDbContext"
        };

        var result = _parser.ParseMigrationFile(migrationFile);

        Assert.NotNull(result);
        Assert.Equal("20240101120000", result.Id);
        Assert.Equal("AddUsers", result.Name);
    }

    [Fact]
    public void ParseMigrationFile_ContentWithSqlOperations_DetectsSqlOperations()
    {
        var migrationFile = new MigrationFile
        {
            FileName = "20240101120000_AddUsers.cs",
            Content = """
                public partial class AddUsers : Migration
                {
                    protected override void Up(MigrationBuilder migrationBuilder)
                    {
                        migrationBuilder.Sql("CREATE TABLE Users (Id int);");
                        migrationBuilder.Sql("CREATE INDEX IX_Users_Id ON Users (Id);");
                    }
                }
                """,
            DbContextName = "TestDbContext"
        };

        var result = _parser.ParseMigrationFile(migrationFile);

        Assert.NotNull(result);
        Assert.Contains("SqlOperationsCount: 2", result.MetadataContent);
    }

    [Fact]
    public void ParseMigrationFile_PublicPartialMigrationClass_ExtractsClassName()
    {
        var migrationFile = new MigrationFile
        {
            FileName = "20240101120000_AddUsers.cs",
            Content = "public partial class AddUsers : Migration { }",
            DbContextName = "TestDbContext"
        };

        var result = _parser.ParseMigrationFile(migrationFile);

        Assert.NotNull(result);
        Assert.Contains("ClassName: AddUsers", result.MetadataContent);
    }

    [Fact]
    public void ParseMigrationFile_NullArgument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _parser.ParseMigrationFile(null!));
    }
}
