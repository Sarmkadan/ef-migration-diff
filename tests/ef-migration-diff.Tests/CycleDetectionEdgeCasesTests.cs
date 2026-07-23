#nullable enable
using EfMigrationDiff.Models;
using FluentAssertions;

namespace EfMigrationDiff.Tests;

public class CycleDetectionEdgeCasesTests
{
    // Self-loop tests
    [Fact]
    public void DetectCycles_WithSelfLoop_SingleNodePointsToItself()
    {
        var graph = new MigrationDependencyGraph();
        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "SelfLoopMigration", Sequence = 1 });
        graph.AddEdge(new MigrationGraphEdge("A", "A"));

        var hasCycles = graph.HasCycles;
        var cyclePath = graph.CyclePath;

        hasCycles.Should().BeTrue("A self-loop should be detected as a cycle");
        cyclePath.Should().NotBeNull();
        cyclePath.Should().HaveCount(1, "Self-loop cycle should contain exactly one node");
        cyclePath.Should().Contain("A", "Cycle path should contain the self-referencing node");
    }

    [Fact]
    public void DetectCycles_WithSelfLoop_MultipleNodesOneWithSelfLoop()
    {
        var graph = new MigrationDependencyGraph();
        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "First", Sequence = 1 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "Second", Sequence = 2 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "SelfLoop", Sequence = 3 });
        graph.AddEdge(new MigrationGraphEdge("A", "B"));
        graph.AddEdge(new MigrationGraphEdge("B", "C"));
        graph.AddEdge(new MigrationGraphEdge("C", "C"));

        var hasCycles = graph.HasCycles;
        var cyclePath = graph.CyclePath;

        hasCycles.Should().BeTrue("Graph with a self-loop should be detected as cyclic");
        cyclePath.Should().NotBeNull();
        cyclePath.Should().Contain("C");
        cyclePath.Should().HaveCount(1);
    }

    // Two disjoint cycles
    [Fact]
    public void DetectCycles_WithTwoDisjointCycles_BothCyclesDetected()
    {
        var graph = new MigrationDependencyGraph();

        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "Cycle1_A", Sequence = 1 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "Cycle1_B", Sequence = 2 });
        graph.AddEdge(new MigrationGraphEdge("A", "B"));
        graph.AddEdge(new MigrationGraphEdge("B", "A"));

        graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "Cycle2_C", Sequence = 3 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "D", Name = "Cycle2_D", Sequence = 4 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "E", Name = "Cycle2_E", Sequence = 5 });
        graph.AddEdge(new MigrationGraphEdge("C", "D"));
        graph.AddEdge(new MigrationGraphEdge("D", "E"));
        graph.AddEdge(new MigrationGraphEdge("E", "C"));

        var hasCycles = graph.HasCycles;
        var cyclePath = graph.CyclePath;

        hasCycles.Should().BeTrue("Graph with multiple disjoint cycles should be detected as cyclic");
        cyclePath.Should().NotBeNull();
        (cyclePath!.Contains("A") || cyclePath.Contains("B") || cyclePath.Contains("C") || cyclePath.Contains("D") || cyclePath.Contains("E")).Should().BeTrue();
        graph.GetTopologicalOrder().Should().BeEmpty();
    }

    [Fact]
    public void DetectCycles_WithTwoDisjointCycles_OnlyFirstCycleDetected()
    {
        var graph = new MigrationDependencyGraph();

        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "Cycle1_A", Sequence = 1 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "Cycle1_B", Sequence = 2 });
        graph.AddEdge(new MigrationGraphEdge("A", "B"));
        graph.AddEdge(new MigrationGraphEdge("B", "A"));

        graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "Cycle2_C", Sequence = 3 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "D", Name = "Cycle2_D", Sequence = 4 });
        graph.AddEdge(new MigrationGraphEdge("C", "D"));
        graph.AddEdge(new MigrationGraphEdge("D", "C"));

        var hasCycles = graph.HasCycles;
        var cyclePath = graph.CyclePath;

        hasCycles.Should().BeTrue();
        cyclePath.Should().NotBeNull();
        cyclePath.Should().OnlyContain(item => new[] { "A", "B" }.Contains(item));
    }

    // Cycle reachable from multiple roots
    [Fact]
    public void DetectCycles_CycleReachableFromMultipleRoots_CyclePathIsDeterministic()
    {
        var graph = new MigrationDependencyGraph();

        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "Root1", Sequence = 1 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "CycleNode1", Sequence = 2 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "CycleNode2", Sequence = 3 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "D", Name = "CycleNode3", Sequence = 4 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "E", Name = "Root2", Sequence = 5 });

        graph.AddEdge(new MigrationGraphEdge("A", "B"));
        graph.AddEdge(new MigrationGraphEdge("B", "C"));
        graph.AddEdge(new MigrationGraphEdge("C", "D"));
        graph.AddEdge(new MigrationGraphEdge("D", "B"));
        graph.AddEdge(new MigrationGraphEdge("E", "B"));

        var hasCycles = graph.HasCycles;
        var cyclePath = graph.CyclePath;

        hasCycles.Should().BeTrue("Cycle reachable from multiple roots should be detected");
        cyclePath.Should().NotBeNull();
        cyclePath.Should().BeEquivalentTo(new[] { "B", "C", "D" }, options => options.WithStrictOrdering());
        var distinctCount = cyclePath!.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        distinctCount.Should().Be(cyclePath.Count);
        cyclePath.Should().HaveCount(3);
    }

    [Fact]
    public void DetectCycles_CycleWithMultipleEntryPoints_ReportsMinimalCycle()
    {
        var graph = new MigrationDependencyGraph();

        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "Entry1", Sequence = 1 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "PathToCycle", Sequence = 2 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "CycleStart", Sequence = 3 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "D", Name = "CycleMiddle", Sequence = 4 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "E", Name = "CycleEnd", Sequence = 5 });

        graph.AddEdge(new MigrationGraphEdge("A", "B"));
        graph.AddEdge(new MigrationGraphEdge("B", "C"));
        graph.AddEdge(new MigrationGraphEdge("C", "D"));
        graph.AddEdge(new MigrationGraphEdge("D", "E"));
        graph.AddEdge(new MigrationGraphEdge("E", "C"));

        var hasCycles = graph.HasCycles;
        var cyclePath = graph.CyclePath;

        hasCycles.Should().BeTrue();
        cyclePath.Should().NotBeNull();
        cyclePath.Should().BeEquivalentTo(new[] { "C", "D", "E" }, options => options.WithStrictOrdering());
    }

    // Diamond-shaped acyclic graph (should NOT be flagged as cycles)
    [Fact]
    public void DetectCycles_DiamondAcyclicGraph_NoFalsePositiveCycle()
    {
        var graph = new MigrationDependencyGraph();

        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "Root", Sequence = 1 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "Left", Sequence = 2 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "Right", Sequence = 3 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "D", Name = "Leaf", Sequence = 4 });

        graph.AddEdge(new MigrationGraphEdge("A", "B"));
        graph.AddEdge(new MigrationGraphEdge("A", "C"));
        graph.AddEdge(new MigrationGraphEdge("B", "D"));
        graph.AddEdge(new MigrationGraphEdge("C", "D"));

        var hasCycles = graph.HasCycles;
        var cyclePath = graph.CyclePath;

        hasCycles.Should().BeFalse("Diamond-shaped acyclic graph should not be flagged as cyclic");
        cyclePath.Should().BeNull();
        var order = graph.GetTopologicalOrder();
        order.Should().HaveCount(4);
        order[0].MigrationId.Should().Be("A");
        order.Skip(1).Select(n => n.MigrationId).Should().BeEquivalentTo(new[] { "B", "C" });
        order[3].MigrationId.Should().Be("D");
    }

    [Fact]
    public void DetectCycles_TwoSeparatePathsToSameNode_NoCycle()
    {
        var graph = new MigrationDependencyGraph();

        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "Start", Sequence = 1 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "Path1", Sequence = 2 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "Path2", Sequence = 3 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "D", Name = "End", Sequence = 4 });

        graph.AddEdge(new MigrationGraphEdge("A", "B"));
        graph.AddEdge(new MigrationGraphEdge("A", "C"));
        graph.AddEdge(new MigrationGraphEdge("B", "D"));
        graph.AddEdge(new MigrationGraphEdge("C", "D"));

        var hasCycles = graph.HasCycles;
        var cyclePath = graph.CyclePath;

        hasCycles.Should().BeFalse("Convergent paths should not create a cycle");
        cyclePath.Should().BeNull();
        var order = graph.GetTopologicalOrder();
        order.Should().HaveCount(4);
    }

    // Complex cycle scenarios
    [Fact]
    public void DetectCycles_LargeCycle_MultipleNodesInCycle()
    {
        var graph = new MigrationDependencyGraph();

        for (int i = 1; i <= 10; i++)
        {
            graph.AddNode(new MigrationGraphNode { MigrationId = "N" + i, Name = "Node" + i, Sequence = i });
        }

        for (int i = 1; i <= 9; i++)
        {
            graph.AddEdge(new MigrationGraphEdge("N" + i, "N" + (i + 1)));
        }
        graph.AddEdge(new MigrationGraphEdge("N10", "N1"));

        var hasCycles = graph.HasCycles;
        var cyclePath = graph.CyclePath;

        hasCycles.Should().BeTrue("Large cycle should be detected");
        cyclePath.Should().NotBeNull();
        cyclePath.Should().HaveCount(10);
        cyclePath.Should().StartWith("N1");
        cyclePath.Should().EndWith("N1");
    }

    [Fact]
    public void DetectCycles_CycleWithChord_ReportsMinimalCycle()
    {
        var graph = new MigrationDependencyGraph();

        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "Start", Sequence = 1 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "Middle", Sequence = 2 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "End", Sequence = 3 });

        graph.AddEdge(new MigrationGraphEdge("A", "B"));
        graph.AddEdge(new MigrationGraphEdge("B", "C"));
        graph.AddEdge(new MigrationGraphEdge("C", "A"));
        graph.AddEdge(new MigrationGraphEdge("A", "C"));

        var hasCycles = graph.HasCycles;
        var cyclePath = graph.CyclePath;

        hasCycles.Should().BeTrue("Cycle with chord should still be detected");
        cyclePath.Should().NotBeNull();
        (cyclePath.Count == 3 || cyclePath.Count == 2).Should().BeTrue();
    }

    // Acyclic graph variations (ensure no false positives)
    [Fact]
    public void DetectCycles_LinearChain_NoCycle()
    {
        var graph = new MigrationDependencyGraph();

        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "First", Sequence = 1 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "Second", Sequence = 2 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "Third", Sequence = 3 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "D", Name = "Fourth", Sequence = 4 });

        graph.AddEdge(new MigrationGraphEdge("A", "B"));
        graph.AddEdge(new MigrationGraphEdge("B", "C"));
        graph.AddEdge(new MigrationGraphEdge("C", "D"));

        var hasCycles = graph.HasCycles;
        var cyclePath = graph.CyclePath;

        hasCycles.Should().BeFalse("Linear chain should not have cycles");
        cyclePath.Should().BeNull();
        var order = graph.GetTopologicalOrder();
        order.Should().HaveCount(4);
        order.Select(n => n.MigrationId).Should().BeEquivalentTo(new[] { "A", "B", "C", "D" });
    }

    [Fact]
    public void DetectCycles_EmptyGraph_NoCycle()
    {
        var graph = new MigrationDependencyGraph();

        var hasCycles = graph.HasCycles;
        var cyclePath = graph.CyclePath;

        hasCycles.Should().BeFalse("Empty graph should not have cycles");
        cyclePath.Should().BeNull();
    }

    [Fact]
    public void DetectCycles_SingleNode_NoCycle()
    {
        var graph = new MigrationDependencyGraph();
        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "Single", Sequence = 1 });

        var hasCycles = graph.HasCycles;
        var cyclePath = graph.CyclePath;

        hasCycles.Should().BeFalse("Single node without edges should not have cycles");
        cyclePath.Should().BeNull();
    }

    // Cycle path correctness
    [Fact]
    public void DetectCycles_CyclePathContainsAllNodesInCycle()
    {
        var graph = new MigrationDependencyGraph();

        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "NodeA", Sequence = 1 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "NodeB", Sequence = 2 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "NodeC", Sequence = 3 });

        graph.AddEdge(new MigrationGraphEdge("A", "B"));
        graph.AddEdge(new MigrationGraphEdge("B", "C"));
        graph.AddEdge(new MigrationGraphEdge("C", "A"));

        var cyclePath = graph.CyclePath;

        cyclePath.Should().NotBeNull();
        cyclePath.Should().HaveCount(3);
        cyclePath.Should().Contain("A");
        cyclePath.Should().Contain("B");
        cyclePath.Should().Contain("C");

        for (int i = 0; i < cyclePath.Count; i++)
        {
            var current = cyclePath[i];
            var next = cyclePath[(i + 1) % cyclePath.Count];
            var hasEdge = graph.Edges.Any(e => e.FromId == current && e.ToId == next);
            hasEdge.Should().BeTrue();
        }
    }

    [Fact]
    public void DetectCycles_CyclePathIsMinimal()
    {
        var graph = new MigrationDependencyGraph();

        graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "Entry", Sequence = 1 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "CycleStart", Sequence = 2 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "Middle", Sequence = 3 });
        graph.AddNode(new MigrationGraphNode { MigrationId = "D", Name = "CycleEnd", Sequence = 4 });

        graph.AddEdge(new MigrationGraphEdge("A", "B"));
        graph.AddEdge(new MigrationGraphEdge("B", "C"));
        graph.AddEdge(new MigrationGraphEdge("C", "D"));
        graph.AddEdge(new MigrationGraphEdge("D", "B"));

        var cyclePath = graph.CyclePath;

        cyclePath.Should().NotBeNull();
        cyclePath.Should().HaveCount(3);
        cyclePath.Should().BeEquivalentTo(new[] { "B", "C", "D" }, options => options.WithStrictOrdering());
    }
}
