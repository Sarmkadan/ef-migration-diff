#nullable enable

using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using FluentAssertions;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Tests for the MigrationParserService class.
/// </summary>
public class MigrationParserServiceTests
{
    private readonly MigrationParserService _parser = new();

    /// <summary>
    /// Tests that a valid migration file is parsed correctly.
    /// </summary>
    [Fact]
    public void ParseMigrationFile_WithValidMigrationFile_ReturnsMigrationObject()
    {
        // Arrange
        var migrationFile = new MigrationFile
        {
            FileName = "20240115093045_CreateUsersTable.cs",
            Content = "migrationBuilder.CreateTable(name: \"Users\"",
            DbContextName = "ApplicationDbContext"
        };

        // Act
        var result = _parser.ParseMigrationFile(migrationFile);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("20240115093045");
        result.Name.Should().Be("CreateUsersTable");
        result.DbContextName.Should().Be("ApplicationDbContext");
        result.Content.Should().Contain("CreateTable");
    }

    /// <summary>
    /// Tests that a designer file is parsed correctly, extracting the migration ID.
    /// </summary>
    [Fact]
    public void ParseMigrationFile_WithDesignerFile_ExtractsCorrectMigrationId()
    {
        // Arrange
        var migrationFile = new MigrationFile
        {
            FileName = "20240115093045_CreateUsersTable.Designer.cs",
            Content = "namespace Migrations { }",
            DbContextName = "AppDbContext"
        };

        // Act
        var result = _parser.ParseMigrationFile(migrationFile);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("20240115093045");
        result.Name.Should().Be("CreateUsersTable");
    }

    /// <summary>
    /// Tests that an invalid timestamp returns null.
    /// </summary>
    [Fact]
    public void ParseMigrationFile_WithInvalidTimestamp_ReturnsNull()
    {
        // Arrange
        var migrationFile = new MigrationFile
        {
            FileName = "InvalidTimestamp_CreateUsersTable.cs",
            Content = "migrationBuilder.CreateTable(...)",
            DbContextName = "AppDbContext"
        };

        // Act
        var result = _parser.ParseMigrationFile(migrationFile);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that an empty content migration file still parses metadata correctly.
    /// </summary>
    [Fact]
    public void ParseMigrationFile_WithEmptyContent_StillParsesMetadata()
    {
        // Arrange
        var migrationFile = new MigrationFile
        {
            FileName = "20240115093045_EmptyMigration.cs",
            Content = string.Empty,
            DbContextName = "AppDbContext"
        };

        // Act
        var result = _parser.ParseMigrationFile(migrationFile);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("20240115093045");
        result.Content.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that various valid migration file names are parsed correctly.
    /// </summary>
    /// <param name="fileName">The name of the migration file.</param>
    [Theory]
    [InlineData("20240115093045_CreateUsersTable.cs")]
    [InlineData("20240115093045_UpdateProductsTable.cs")]
    [InlineData("20240115093045_DropLegacyData.cs")]
    public void ParseMigrationFile_WithVariousValidNames_ExtractionSucceeds(string fileName)
    {
        // Arrange
        var migrationFile = new MigrationFile
        {
            FileName = fileName,
            Content = "// migration content",
            DbContextName = "DbContext"
        };

        // Act
        var result = _parser.ParseMigrationFile(migrationFile);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("20240115093045");
        result!.Id.Should().HaveLength(14);
        result.Name.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that a null migration file throws an exception.
    /// </summary>
    [Fact]
    public void ParseMigrationFile_WithNullMigrationFile_ThrowsException()
    {
        // Act & Assert
        var act = () => _parser.ParseMigrationFile(null!);
        act.Should().ThrowExactly<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that a complex migration file content is parsed correctly.
    /// </summary>
    [Fact]
    public void ParseMigrationFile_WithComplexContent_ParsesSuccessfully()
    {
        // Arrange
        var complexContent = @"
            migrationBuilder.CreateTable(
                name: ""Orders"",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    CustomerId = table.Column<int>(nullable: false),
                    OrderDate = table.Column<DateTime>(nullable: false)
                });
            migrationBuilder.CreateIndex(name: ""IX_Orders_CustomerId"", table: ""Orders"", column: ""CustomerId"");
        ";

        var migrationFile = new MigrationFile
        {
            FileName = "20240115093045_CreateOrdersTable.cs",
            Content = complexContent,
            DbContextName = "SalesDbContext"
        };

        // Act
        var result = _parser.ParseMigrationFile(migrationFile);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("CreateTable");
        result.Content.Should().Contain("CreateIndex");
    }
}
