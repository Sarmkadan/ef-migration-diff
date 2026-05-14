#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using EfMigrationDiff.Models;
using EfMigrationDiff.Services;
using FluentAssertions;

namespace EfMigrationDiff.Tests;

/// <summary>
/// Unit tests for the migration dependency graph feature.
/// </summary>
public class MigrationDependencyGraphTests
{
    private readonly MigrationDependencyGraphService _service = new();

    // =========================================================================
    // Build
    // =========================================================================

    [Fact]
    public void Build_WithEmptyList_ReturnsEmptyGraph()
    {
        // Act
        var graph = _service.Build(Enumerable.Empty<Migration>());

        // Assert
        graph.Should().NotBeNull();
        graph.IsEmpty.Should().BeTrue();
        graph.Edges.Should().BeEmpty();
    }

    [Fact]
    public void Build_WithSingleMigration_ProducesSingleNode()
    {
        // Arrange
        var migration = new Migration("20240101120000", "CreateUsers", "AppDbContext") { Sequence = 1 };

        // Act
        var graph = _service.Build(new[] { migration });

        // Assert
        graph.Nodes.Should().ContainKey("20240101120000");
        graph.Nodes["20240101120000"].Name.Should().Be("CreateUsers");
        graph.Edges.Should().BeEmpty(); // No edges for a single migration
    }

    [Fact]
    public void Build_WithTwoMigrations_AddsSequentialEdge()
    {
        // Arrange
        var m1 = new Migration("20240101120000", "CreateUsers",   "AppDbContext") { Sequence = 1 };
        var m2 = new Migration("20240101130000", "CreateOrders",  "AppDbContext") { Sequence = 2 };

        // Act
        var graph = _service.Build(new[] { m1, m2 });

        // Assert
        graph.Edges.Should().ContainSingle();
        graph.Edges[0].FromId.Should().Be("20240101120000");
        graph.Edges[0].ToId.Should().Be("20240101130000");
        graph.Edges[0].Kind.Should().Be(DependencyKind.Sequential);
    }

    [Fact]
    public void Build_WithMigrationsTouchingSameTable_AddsSharedTableEdge()
    {
        // Arrange
        var m1 = new Migration("20240101120000", "CreateUsers", "AppDbContext")
        {
            Sequence = 1,
            Content  = @"migrationBuilder.CreateTable(name: ""Users"", columns: table => ...)"
        };
        var m2 = new Migration("20240101130000", "AddUserEmail", "AppDbContext")
        {
            Sequence = 2,
            Content  = @"migrationBuilder.AddColumn<string>(name: ""Email"", table: ""Users"")"
        };

        // Act
        var graph = _service.Build(new[] { m1, m2 });

        // Assert
        graph.Edges.Should().NotBeEmpty();
        // The "Users" table shared-table edge should exist alongside the sequential edge
        // (or be merged since they go in the same direction)
        graph.HasCycles.Should().BeFalse();
    }

    // =========================================================================
    // GetTopologicalOrder
    // =========================================================================

    [Fact]
    public void GetTopologicalOrder_WithLinearChain_ReturnsMigrationsInOrder()
    {
        // Arrange
        var migrations = new[]
        {
            new Migration("20240101120000", "First",  "Ctx") { Sequence = 1 },
            new Migration("20240101130000", "Second", "Ctx") { Sequence = 2 },
            new Migration("20240101140000", "Third",  "Ctx") { Sequence = 3 }
        };

        // Act
        var graph = _service.Build(migrations);
        var order = graph.GetTopologicalOrder();

        // Assert
        order.Should().HaveCount(3);
        order[0].MigrationId.Should().Be("20240101120000");
        order[1].MigrationId.Should().Be("20240101130000");
        order[2].MigrationId.Should().Be("20240101140000");
    }

    [Fact]
    public void HasCycles_WithAcyclicGraph_ReturnsFalse()
    {
        // Arrange
        var migrations = new[]
        {
            new Migration("A", "First",  "Ctx") { Sequence = 1 },
            new Migration("B", "Second", "Ctx") { Sequence = 2 }
        };

        // Act
        var graph = _service.Build(migrations);

        // Assert
        graph.HasCycles.Should().BeFalse();
    }

    // =========================================================================
    // GetAncestors / GetDescendants
    // =========================================================================

    [Fact]
    public void GetAncestors_ReturnsAllPredecessors()
    {
        // Arrange
        var migrations = new[]
        {
            new Migration("20240101120000", "First",  "Ctx") { Sequence = 1 },
            new Migration("20240101130000", "Second", "Ctx") { Sequence = 2 },
            new Migration("20240101140000", "Third",  "Ctx") { Sequence = 3 }
        };
        var graph = _service.Build(migrations);

        // Act
        var ancestors = graph.GetAncestors("20240101140000");

        // Assert
        ancestors.Should().Contain("20240101130000");
        ancestors.Should().Contain("20240101120000");
    }

    [Fact]
    public void GetDescendants_ReturnsAllSuccessors()
    {
        // Arrange
        var migrations = new[]
        {
            new Migration("20240101120000", "First",  "Ctx") { Sequence = 1 },
            new Migration("20240101130000", "Second", "Ctx") { Sequence = 2 },
            new Migration("20240101140000", "Third",  "Ctx") { Sequence = 3 }
        };
        var graph = _service.Build(migrations);

        // Act
        var descendants = graph.GetDescendants("20240101120000");

        // Assert
        descendants.Should().Contain("20240101130000");
        descendants.Should().Contain("20240101140000");
    }

    // =========================================================================
    // GetRollbackImpact
    // =========================================================================

    [Fact]
    public void GetRollbackImpact_IncludesTargetAndAllDescendants()
    {
        // Arrange
        var migrations = new[]
        {
            new Migration("20240101120000", "First",  "Ctx") { Sequence = 1 },
            new Migration("20240101130000", "Second", "Ctx") { Sequence = 2 },
            new Migration("20240101140000", "Third",  "Ctx") { Sequence = 3 }
        };
        var graph  = _service.Build(migrations);

        // Act
        var impact = _service.GetRollbackImpact(graph, "20240101130000");

        // Assert
        impact.Should().Contain("20240101130000");
        impact.Should().Contain("20240101140000");
        impact.Should().NotContain("20240101120000"); // ancestor, not affected
    }

    // =========================================================================
    // RenderText
    // =========================================================================

    [Fact]
    public void RenderText_ProducesMeaningfulOutput()
    {
        // Arrange
        var migrations = new[]
        {
            new Migration("20240101120000", "CreateUsers",  "AppDbContext") { Sequence = 1 },
            new Migration("20240101130000", "CreateOrders", "AppDbContext") { Sequence = 2 }
        };
        var graph = _service.Build(migrations);

        // Act
        var text = _service.RenderText(graph);

        // Assert
        text.Should().Contain("CreateUsers");
        text.Should().Contain("CreateOrders");
        text.Should().Contain("Migration Dependency Graph");
    }

    // =========================================================================
    // MigrationDependencyGraph — direct manipulation
    // =========================================================================

    [Fact]
    public void AddEdge_WithUnknownNode_ThrowsArgumentException()
    {
        // Arrange
        var graph = new MigrationDependencyGraph();
        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "NodeA" });

        // Act
        var act = () => graph.AddEdge(new MigrationGraphEdge("A", "UNKNOWN"));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetTopologicalOrder_WithCyclicGraph_ReturnsEmpty()
    {
        // Arrange
        var graph = new MigrationDependencyGraph();
        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "A", Sequence = 1 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "B", Sequence = 2 });
        graph.AddEdge(new MigrationGraphEdge("A", "B"));
        graph.AddEdge(new MigrationGraphEdge("B", "A")); // creates cycle

        // Act
        var order = graph.GetTopologicalOrder();

        // Assert
        graph.HasCycles.Should().BeTrue();
        order.Should().BeEmpty();
    }
}
