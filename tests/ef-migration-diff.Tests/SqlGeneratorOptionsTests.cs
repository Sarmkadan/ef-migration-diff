#nullable enable
using EfMigrationDiff.Configuration;
using EfMigrationDiff.Exceptions;
using EfMigrationDiff.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Tests for <see cref="SqlGeneratorOptions"/> validation rules and the fail-fast behaviour of <see cref="SqlGenerator"/>.
/// </summary>
public class SqlGeneratorOptionsTests
{
    /// <summary>
    /// Verifies that the default options satisfy every rule.
    /// </summary>
    [Fact]
    public void Validate_DefaultOptions_ReturnsNoViolations()
    {
        // Act
        var violations = new SqlGeneratorOptions().Validate();

        // Assert
        violations.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that unsupported or missing dialects are reported.
    /// </summary>
    [Theory]
    [InlineData("Oracle")]
    [InlineData("Postgres")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_UnsupportedDialect_ReturnsDialectViolation(string dialect)
    {
        // Arrange
        var options = new SqlGeneratorOptions { Dialect = dialect };

        // Act
        var violations = options.Validate();

        // Assert
        violations.Should().ContainSingle().Which.Should().Contain("Dialect");
    }

    /// <summary>
    /// Verifies that a supported dialect is accepted regardless of letter case.
    /// </summary>
    [Theory]
    [InlineData("SqlServer")]
    [InlineData("sqlserver")]
    public void Validate_SupportedDialect_ReturnsNoViolations(string dialect)
    {
        // Arrange
        var options = new SqlGeneratorOptions { Dialect = dialect };

        // Act
        var violations = options.Validate();

        // Assert
        violations.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a set SchemaName must be a plain identifier.
    /// </summary>
    [Theory]
    [InlineData("1Sales")]
    [InlineData("my schema")]
    [InlineData("dbo;DROP")]
    [InlineData("[dbo]")]
    [InlineData("")]
    [InlineData("dbo\n")]
    public void Validate_InvalidSchemaName_ReturnsSchemaNameViolation(string schemaName)
    {
        // Arrange
        var options = new SqlGeneratorOptions { SchemaName = schemaName };

        // Act
        var violations = options.Validate();

        // Assert
        violations.Should().ContainSingle().Which.Should().Contain("SchemaName");
    }

    /// <summary>
    /// Verifies that valid identifiers and an unset (null) SchemaName produce no violations.
    /// </summary>
    [Theory]
    [InlineData("dbo")]
    [InlineData("Sales_2024")]
    [InlineData("_private")]
    [InlineData(null)]
    public void Validate_ValidOrUnsetSchemaName_ReturnsNoViolations(string? schemaName)
    {
        // Arrange
        var options = new SqlGeneratorOptions { SchemaName = schemaName };

        // Act
        var violations = options.Validate();

        // Assert
        violations.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that an empty BatchSeparator is rejected while batching is enabled.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_EmptyBatchSeparatorWithBatchingEnabled_ReturnsBatchSeparatorViolation(string separator)
    {
        // Arrange
        var options = new SqlGeneratorOptions { EnableBatching = true, BatchSeparator = separator };

        // Act
        var violations = options.Validate();

        // Assert
        violations.Should().ContainSingle().Which.Should().Contain("BatchSeparator");
    }

    /// <summary>
    /// Verifies that an empty BatchSeparator is allowed when batching is disabled.
    /// </summary>
    [Fact]
    public void Validate_EmptyBatchSeparatorWithBatchingDisabled_ReturnsNoViolations()
    {
        // Arrange
        var options = new SqlGeneratorOptions { EnableBatching = false, BatchSeparator = string.Empty };

        // Act
        var violations = options.Validate();

        // Assert
        violations.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that an OutputFilePath whose directory does not exist is reported.
    /// </summary>
    [Fact]
    public void Validate_OutputDirectoryMissing_ReturnsOutputPathViolation()
    {
        // Arrange: a GUID-named directory under the temp path is guaranteed not to exist
        var missingFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "script.sql");
        var options = new SqlGeneratorOptions { OutputFilePath = missingFile };

        // Act
        var violations = options.Validate();

        // Assert
        violations.Should().ContainSingle().Which.Should().Contain("does not exist");
    }

    /// <summary>
    /// Verifies that an OutputFilePath inside an existing directory produces no violations.
    /// </summary>
    [Fact]
    public void Validate_OutputDirectoryExists_ReturnsNoViolations()
    {
        // Arrange
        var options = new SqlGeneratorOptions { OutputFilePath = Path.Combine(Path.GetTempPath(), "script.sql") };

        // Act
        var violations = options.Validate();

        // Assert
        violations.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a set but blank OutputFilePath is reported.
    /// </summary>
    [Fact]
    public void Validate_BlankOutputFilePath_ReturnsOutputPathViolation()
    {
        // Arrange
        var options = new SqlGeneratorOptions { OutputFilePath = "   " };

        // Act
        var violations = options.Validate();

        // Assert
        violations.Should().ContainSingle().Which.Should().Contain("OutputFilePath");
    }

    /// <summary>
    /// Verifies that every violation is collected in one pass and reported together in one exception.
    /// </summary>
    [Fact]
    public void ValidateAndThrow_MultipleViolations_ReportsAllInOneException()
    {
        // Arrange
        var missingDirectoryFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "out.sql");
        var options = new SqlGeneratorOptions
        {
            Dialect = "Oracle",
            SchemaName = "bad schema",
            EnableBatching = true,
            BatchSeparator = "",
            OutputFilePath = missingDirectoryFile
        };

        // Act
        var act = () => options.ValidateAndThrow();

        // Assert
        var exception = act.Should().Throw<ConfigurationValidationException>().Which;
        exception.Violations.Should().HaveCount(4);
        exception.Violations.Should().Contain(v => v.Contains("Dialect"));
        exception.Violations.Should().Contain(v => v.Contains("SchemaName"));
        exception.Violations.Should().Contain(v => v.Contains("BatchSeparator"));
        exception.Violations.Should().Contain(v => v.Contains("OutputFilePath"));
        exception.Message.Should().Contain("4 error(s)");
        exception.Message.Should().Contain("Oracle").And.Contain("bad schema").And.Contain("BatchSeparator");
    }

    /// <summary>
    /// Verifies that the generator refuses to construct with invalid SQL options.
    /// </summary>
    [Fact]
    public void SqlGeneratorConstructor_InvalidOptions_ThrowsConfigurationValidationException()
    {
        // Arrange
        var config = new EfMigrationDiffOptions
        {
            SqlGenerator = new SqlGeneratorOptions { Dialect = "Oracle", SchemaName = "1bad" }
        };

        // Act
        var act = () => new SqlGenerator(NullLogger<SqlGenerator>.Instance, Options.Create(config));

        // Assert
        act.Should().Throw<ConfigurationValidationException>()
            .Which.Violations.Should().HaveCount(2);
    }
}
