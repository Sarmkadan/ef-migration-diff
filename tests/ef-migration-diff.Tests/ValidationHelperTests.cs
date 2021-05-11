#nullable enable
using EfMigrationDiff.Utilities;
using FluentAssertions;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Tests for the ValidationHelper class.
/// </summary>
public class ValidationHelperTests
{
    /// <summary>
    /// Tests the IsValidMigrationTimestamp method with various inputs.
    /// </summary>
    [Theory]
    [InlineData("20240115093045", true)]
    [InlineData("2024011509304X", false)]
    [InlineData("202401150930", false)]
    [InlineData("", false)]
    public void IsValidMigrationTimestamp_WithVariousInputs_ReturnsExpectedResult(
        /// <param name="timestamp">The migration timestamp to test.</param>
        /// <param name="expected">The expected result.</param>
        string timestamp, bool expected)
    {
        // Act
        var result = ValidationHelper.IsValidMigrationTimestamp(timestamp);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests the IsValidTableName method with a bracketed schema and table name.
    /// </summary>
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

    /// <summary>
    /// Tests the IsValidColumnName method with a column name starting with a digit.
    /// </summary>
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

    /// <summary>
    /// Tests the SanitizeInput method with SQL injection characters.
    /// </summary>
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
