#nullable enable
using EfMigrationDiff.Extensions;
using FluentAssertions;
using Xunit;

namespace EfMigrationDiff.Tests;

public sealed class StringExtensionsEdgeCaseTests
{
    [Fact]
    public void IsNullOrEmpty_Null_ReturnsTrue() =>
        ((string?)null).IsNullOrEmpty().Should().BeTrue();

    [Fact]
    public void IsNullOrEmpty_Empty_ReturnsTrue() =>
        "".IsNullOrEmpty().Should().BeTrue();

    [Fact]
    public void IsNullOrEmpty_ValidString_ReturnsFalse() =>
        "migration".IsNullOrEmpty().Should().BeFalse();

    [Fact]
    public void IsNullOrWhiteSpace_Whitespace_ReturnsTrue() =>
        "   ".IsNullOrWhiteSpace().Should().BeTrue();

    [Fact]
    public void OrEmpty_Null_ReturnsEmpty() =>
        ((string?)null).OrEmpty().Should().BeEmpty();

    [Fact]
    public void OrEmpty_ValidString_ReturnsOriginal() =>
        "value".OrEmpty().Should().Be("value");

    [Fact]
    public void Or_NullWithDefault_ReturnsDefault() =>
        ((string?)null).Or("default").Should().Be("default");

    [Fact]
    public void Or_EmptyWithDefault_ReturnsDefault() =>
        "".Or("default").Should().Be("default");

    [Fact]
    public void Or_ValidStringWithDefault_ReturnsOriginal() =>
        "value".Or("default").Should().Be("value");

    [Fact]
    public void EnsureEndsWith_AlreadyEnds_ReturnsUnchanged() =>
        "file.sql".EnsureEndsWith(".sql").Should().Be("file.sql");

    [Fact]
    public void EnsureEndsWith_DoesNotEnd_AppendsSuffix() =>
        "file".EnsureEndsWith(".sql").Should().Be("file.sql");

    [Fact]
    public void EnsureEndsWith_NullInput_ThrowsArgumentNull()
    {
        var act = () => ((string)null!).EnsureEndsWith(".sql");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnsureStartsWith_NullInput_ThrowsArgumentNull()
    {
        var act = () => ((string)null!).EnsureStartsWith("prefix");
        act.Should().Throw<ArgumentNullException>();
    }
}
