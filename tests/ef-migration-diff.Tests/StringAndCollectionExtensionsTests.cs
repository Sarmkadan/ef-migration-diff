// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Extensions;
using FluentAssertions;

namespace EfMigrationDiff.Tests;

public class StringAndCollectionExtensionsTests
{
    [Fact]
    public void ToPascalCase_WithUnderscoreSeparatedWords_ReturnsPascalCase()
    {
        // Arrange
        const string input = "hello_world";

        // Act
        var result = input.ToPascalCase();

        // Assert
        result.Should().Be("HelloWorld");
    }

    [Fact]
    public void ToSnakeCase_WithPascalCaseInput_InsertsUnderscoreBeforeUppercase()
    {
        // Arrange
        const string input = "HelloWorld";

        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be("hello_world");
    }

    [Fact]
    public void Truncate_WhenStringExceedsMaxLength_ReturnsTruncatedStringWithEllipsis()
    {
        // Arrange
        const string input = "Hello World";

        // Act
        var result = input.Truncate(8);

        // Assert
        result.Should().Be("Hello...");
        result.Length.Should().Be(8);
    }

    [Fact]
    public void Batch_WithTenItemsAndBatchSizeThree_CreatesFourBatchesWithLastPartial()
    {
        // Arrange
        var items = Enumerable.Range(1, 10);

        // Act
        var batches = items.Batch(3).ToList();

        // Assert
        batches.Should().HaveCount(4);
        batches[0].Should().HaveCount(3);
        batches[3].Should().HaveCount(1);
    }
}
