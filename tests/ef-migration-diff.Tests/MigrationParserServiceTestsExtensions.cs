#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using FluentAssertions;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Extension methods for <see cref="MigrationParserServiceTests"/> that provide utility methods
/// for testing migration parsing scenarios in a more fluent and reusable way.
/// </summary>
public static class MigrationParserServiceTestsExtensions
{
    /// <summary>
    /// Creates a test migration file with the specified parameters for testing parsing scenarios.
    /// </summary>
    /// <param name="timestamp">The timestamp string (14 digits).</param>
    /// <param name="name">The migration name.</param>
    /// <param name="content">The migration content.</param>
    /// <param name="isDesignerFile">Whether this is a designer file.</param>
    /// <returns>A configured <see cref="MigrationFile"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timestamp"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="timestamp"/> is not 14 digits.</exception>
    public static MigrationFile CreateTestMigrationFile(
        this MigrationParserServiceTests _,
        string timestamp,
        string name,
        string content = "// migration content",
        bool isDesignerFile = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(timestamp);

        if (timestamp.Length != 14)
        {
            throw new ArgumentException("Timestamp must be a 14-digit string representing yyyyMMddHHmmss format.", nameof(timestamp));
        }

        if (!System.Linq.Enumerable.All(timestamp, char.IsDigit))
        {
            throw new ArgumentException("Timestamp must contain only digits.", nameof(timestamp));
        }

        var fileName = isDesignerFile
            ? $"{timestamp}_{name}.Designer.cs"
            : $"{timestamp}_{name}.cs";

        return new MigrationFile
        {
            FileName = fileName,
            Content = content,
            DbContextName = "TestDbContext"
        };
    }

    /// <summary>
    /// Asserts that a migration file parses correctly and returns a non-null result.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="migrationFile">The migration file to parse.</param>
    /// <returns>The parsed migration object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="migrationFile"/> is null.</exception>
    public static Migration ParseMigrationAndAssertSuccess(
        this MigrationParserServiceTests test,
        MigrationParserService parser,
        MigrationFile migrationFile)
    {
        ArgumentNullException.ThrowIfNull(migrationFile);
        ArgumentNullException.ThrowIfNull(parser);

        var result = parser.ParseMigrationFile(migrationFile);
        result.Should().NotBeNull("because the migration file should parse successfully");
        return result!;
    }

    /// <summary>
    /// Creates a collection of test cases for various valid migration file names.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <returns>An enumerable of test cases with file names and expected IDs.</returns>
    public static IEnumerable<(string FileName, string ExpectedId)> CreateValidMigrationNameTestCases(
        this MigrationParserServiceTests _)
    {
        yield return ("20240115093045_CreateUsersTable.cs", "20240115093045");
        yield return ("20240115093045_UpdateProductsTable.cs", "20240115093045");
        yield return ("20240115093045_DropLegacyData.cs", "20240115093045");
        yield return ("20251231235959_AddFinalSchema.cs", "20251231235959");
        yield return ("20230101000000_InitialCreate.Designer.cs", "20230101000000");
    }

    /// <summary>
    /// Asserts that parsing a migration file throws the expected <see cref="ArgumentNullException"/>.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="act">The action that should throw.</param>
    public static void AssertThrowsArgumentNullException(
        this MigrationParserServiceTests test,
        Action act)
    {
        ArgumentNullException.ThrowIfNull(act);

        act.Should().ThrowExactly<ArgumentNullException>(
            "because null migration files should throw ArgumentNullException");
    }
}