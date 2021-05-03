#nullable enable
using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Unit tests for the auto-merge suggestions feature (MigrationAutoResolverService).
/// </summary>
public class AutoMergeSuggestionsTests
{
    private MigrationAutoResolverService CreateService() =>
        new(NullLogger<MigrationAutoResolverService>.Instance);

    // =========================================================================
    // ResolveAsync — happy paths
    // =========================================================================

    [Fact]
    public async Task ResolveAsync_WithNoConflicts_ReturnsEmptyResult()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ResolveAsync(Enumerable.Empty<ConflictInfo>());

        // Assert
        result.Should().NotBeNull();
        result.TotalConflicts.Should().Be(0);
        result.IsFullyResolved.Should().BeTrue();
        result.GetSummary().Should().Contain("No conflicts");
    }

    [Fact]
    public async Task ResolveAsync_WithIndexConflict_AutoResolvesViaSkip()
    {
        // Arrange
        var service  = CreateService();
        var conflict = new ConflictInfo("mig_src", "mig_tgt", ConflictType.IndexConflict)
        {
            Severity    = ConflictSeverity.Warning,
            Description = "Duplicate index on Users table"
        };

        // Act
        var result = await service.ResolveAsync(new[] { conflict });

        // Assert
        result.ResolvedCount.Should().Be(1);
        result.UnresolvedCount.Should().Be(0);
        result.IsFullyResolved.Should().BeTrue();
        var attempt = result.Attempts.Should().ContainSingle().Subject;
        attempt.Succeeded.Should().BeTrue();
        attempt.StrategyApplied.Should().Be(MergeStrategy.Skip);
    }

    [Fact]
    public async Task ResolveAsync_WithConstraintConflict_AutoResolvesViaCombine()
    {
        // Arrange
        var service  = CreateService();
        var conflict = new ConflictInfo("mig_src", "mig_tgt", ConflictType.ConstraintConflict)
        {
            Severity    = ConflictSeverity.Warning,
            Description = "Constraint added on both branches"
        };
        conflict.AddDetail("SourceSql", "ADD CONSTRAINT FK_Orders_Users ...");
        conflict.AddDetail("TargetSql", "ADD CONSTRAINT FK_Products_Users ...");

        // Act
        var result = await service.ResolveAsync(new[] { conflict });

        // Assert
        result.ResolvedCount.Should().Be(1);
        var attempt = result.Attempts.Should().ContainSingle().Subject;
        attempt.Succeeded.Should().BeTrue();
        attempt.StrategyApplied.Should().Be(MergeStrategy.Combine);
        attempt.MergedContent.Should().Contain("FK_Orders_Users");
        attempt.MergedContent.Should().Contain("FK_Products_Users");
    }

    [Fact]
    public async Task ResolveAsync_WithColumnConflict_LeavesUnresolved()
    {
        // Arrange
        var service  = CreateService();
        var conflict = new ConflictInfo("mig_src", "mig_tgt", ConflictType.ColumnConflict)
        {
            Severity    = ConflictSeverity.Error,
            Description = "Column definition conflict"
        };

        // Act
        var result = await service.ResolveAsync(new[] { conflict });

        // Assert
        result.ResolvedCount.Should().Be(0);
        result.UnresolvedConflicts.Should().ContainSingle();
    }

    [Fact]
    public async Task ResolveAsync_WithTableConflict_LeavesUnresolved()
    {
        // Arrange
        var service  = CreateService();
        var conflict = new ConflictInfo("mig_src", "mig_tgt", ConflictType.TableConflict)
        {
            Severity    = ConflictSeverity.Critical,
            Description = "Both branches create the same table"
        };

        // Act
        var result = await service.ResolveAsync(new[] { conflict });

        // Assert
        result.UnresolvedConflicts.Should().ContainSingle();
        result.HasBlockingConflicts.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_WithMixedConflicts_PartiallyResolves()
    {
        // Arrange
        var service = CreateService();
        var conflicts = new[]
        {
            new ConflictInfo("mig_src", "mig_tgt", ConflictType.IndexConflict)
            {
                Severity = ConflictSeverity.Warning,
                Description = "Duplicate index"
            },
            new ConflictInfo("mig_src2", "mig_tgt2", ConflictType.TableConflict)
            {
                Severity = ConflictSeverity.Critical,
                Description = "Table conflict"
            }
        };

        // Act
        var result = await service.ResolveAsync(conflicts);

        // Assert
        result.TotalConflicts.Should().Be(2);
        result.ResolvedCount.Should().Be(1);
        result.UnresolvedConflicts.Should().ContainSingle();
        result.IsFullyResolved.Should().BeFalse();
        result.GetSummary().Should().Contain("1/2");
    }

    // =========================================================================
    // ConfigureStrategy
    // =========================================================================

    [Fact]
    public async Task ConfigureStrategy_OverridesDefaultBehavior()
    {
        // Arrange
        var service = CreateService();
        service.ConfigureStrategy(ConflictType.NameConflict, MergeStrategy.LastWins);

        var conflict = new ConflictInfo("mig_src", "mig_tgt", ConflictType.NameConflict)
        {
            Severity = ConflictSeverity.Warning,
            Description = "Name conflict"
        };
        conflict.AddDetail("TargetSql", "-- target sql");

        // Act
        var result = await service.ResolveAsync(new[] { conflict });

        // Assert
        result.ResolvedCount.Should().Be(1);
        var attempt = result.Attempts.Should().ContainSingle().Subject;
        attempt.StrategyApplied.Should().Be(MergeStrategy.LastWins);
    }

    [Fact]
    public void GetStrategy_ForRegisteredType_ReturnsExpectedStrategy()
    {
        // Arrange
        var service = CreateService();

        // Act
        var strategy = service.GetStrategy(ConflictType.IndexConflict);

        // Assert
        strategy.Should().Be(MergeStrategy.Skip);
    }

    [Fact]
    public void GetStrategy_ForUnregisteredType_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var strategy = service.GetStrategy(ConflictType.TableConflict);

        // Assert
        strategy.Should().BeNull();
    }

    // =========================================================================
    // Cancellation
    // =========================================================================

    [Fact]
    public async Task ResolveAsync_WithCancelledToken_ThrowsOperationCancelledException()
    {
        // Arrange
        var service  = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var conflict = new ConflictInfo("mig_src", "mig_tgt", ConflictType.IndexConflict)
        {
            Severity    = ConflictSeverity.Warning,
            Description = "Index conflict"
        };

        // Act
        var act = async () => await service.ResolveAsync(new[] { conflict }, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
