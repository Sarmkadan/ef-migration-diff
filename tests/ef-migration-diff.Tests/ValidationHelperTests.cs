// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Utilities;
using FluentAssertions;

namespace EfMigrationDiff.Tests;

public class ValidationHelperTests
{
    [Theory]
    [InlineData("20240115093045", true)]
    [InlineData("2024011509304X", false)]
    [InlineData("202401150930", false)]
    [InlineData("", false)]
    public void IsValidMigrationTimestamp_WithVariousInputs_ReturnsExpectedResult(
        string timestamp, bool expected)
    {
        // Act
        var result = ValidationHelper.IsValidMigrationTimestamp(timestamp);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsValidTableName_WithBracketedSchemaAndTableName_ReturnsTrue()
    {
        // Arrange
        const string tableName = "[dbo].[Users]";

        // Act
        var result = ValidationHelper.IsValidTableName(tableName);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidColumnName_WithDigitAsFirstCharacter_ReturnsFalse()
    {
        // Arrange
        const string columnName = "9InvalidColumn";

        // Act
        var result = ValidationHelper.IsValidColumnName(columnName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SanitizeInput_WithSqlInjectionCharacters_RemovesSemicolonAndSingleQuote()
    {
        // Arrange
        const string input = "'; DROP TABLE users; --";

        // Act
        var sanitized = ValidationHelper.SanitizeInput(input);

        // Assert
        sanitized.Should().NotContain("'");
        sanitized.Should().NotContain(";");
        sanitized.Should().Contain("DROP TABLE users");
    }
}
