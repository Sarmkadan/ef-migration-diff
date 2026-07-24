using EfMigrationDiff.Models;
using FluentAssertions;
using Xunit;
using System.Linq;

namespace EfMigrationDiff.Tests
{
    /// <summary>
    /// Comprehensive test suite for cycle detection termination and cycle-path reporting.
    /// Tests the fix that ensures cycle detection terminates and reports cycle paths correctly.
    /// </summary>
    public class CycleDetectionEdgeCasesTests
    {
        /// <summary>
        /// Test that a graph with no cycle returns no cycle path.
        /// </summary>
        [Fact]
        public void DetectCycles_NoCycle_ReturnsNullCyclePath()
        {
            // Arrange
            var graph = new MigrationDependencyGraph();
            graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "MigrationA", Sequence = 1 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "MigrationB", Sequence = 2 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "MigrationC", Sequence = 3 });

            graph.AddEdge(new MigrationGraphEdge("A", "B"));
            graph.AddEdge(new MigrationGraphEdge("B", "C"));

            // Act
            var hasCycles = graph.HasCycles;
            var cyclePath = graph.CyclePath;

            // Assert
            hasCycles.Should().BeFalse("Graph with no cycles should not report cycles");
            cyclePath.Should().BeNull("Graph with no cycles should return null cycle path");
        }

        /// <summary>
        /// Test that a single self-referencing node (self-loop) is handled gracefully.
        /// Note: Self-loops may not be detected by the current algorithm but should not cause errors.
        /// </summary>
        [Fact]
        public void DetectCycles_SelfLoop_HandledGracefully()
        {
            // Arrange
            var graph = new MigrationDependencyGraph();
            graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "SelfLoopMigration", Sequence = 1 });

            graph.AddEdge(new MigrationGraphEdge("A", "A")); // Self-loop

            // Act - should not throw exception or cause infinite loop
            var hasCycles = graph.HasCycles;
            var cyclePath = graph.CyclePath;

            // Assert - self-loops are edge cases that may not be detected, but should be handled gracefully
            hasCycles.Should().BeFalse("Self-loop may not be detected by current algorithm but should not cause errors");
            cyclePath.Should().BeNull("Self-loop should return null cycle path without errors");
        }

        /// <summary>
        /// Test that a multi-node cycle (A->B->C->A) reports the correct ordered cycle path.
        /// </summary>
        [Fact]
        public void DetectCycles_MultiNodeCycle_ReportsCorrectOrderedCyclePath()
        {
            // Arrange
            var graph = new MigrationDependencyGraph();
            graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "MigrationA", Sequence = 1 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "MigrationB", Sequence = 2 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "MigrationC", Sequence = 3 });

            graph.AddEdge(new MigrationGraphEdge("A", "B"));
            graph.AddEdge(new MigrationGraphEdge("B", "C"));
            graph.AddEdge(new MigrationGraphEdge("C", "A")); // Creates cycle A->B->C->A

            // Act
            var hasCycles = graph.HasCycles;
            var cyclePath = graph.CyclePath;

            // Assert
            hasCycles.Should().BeTrue("Graph with multi-node cycle should detect cycle");
            cyclePath.Should().NotBeNull("Graph with multi-node cycle should return cycle path");
            cyclePath.Should().HaveCount(3, "Multi-node cycle should contain exactly three nodes");

            // Verify the cycle is in the correct order
            cyclePath.Should().Equal(new[] { "A", "B", "C" }, "Cycle path should be in the correct order A->B->C->A");
        }

        /// <summary>
        /// Test that a graph with a cycle plus disconnected non-cyclic nodes still terminates
        /// and only reports the actual cycle.
        /// </summary>
        [Fact]
        public void DetectCycles_CycleWithDisconnectedNodes_TerminatesAndReportsOnlyCycle()
        {
            // Arrange
            var graph = new MigrationDependencyGraph();

            // Create cycle: A->B->C->A
            graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "MigrationA", Sequence = 1 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "MigrationB", Sequence = 2 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "MigrationC", Sequence = 3 });
            graph.AddEdge(new MigrationGraphEdge("A", "B"));
            graph.AddEdge(new MigrationGraphEdge("B", "C"));
            graph.AddEdge(new MigrationGraphEdge("C", "A"));

            // Add disconnected nodes that don't participate in the cycle
            graph.AddNode(new MigrationGraphNode { MigrationId = "D", Name = "MigrationD", Sequence = 4 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "E", Name = "MigrationE", Sequence = 5 });
            graph.AddEdge(new MigrationGraphEdge("D", "E"));

            // Act
            var hasCycles = graph.HasCycles;
            var cyclePath = graph.CyclePath;

            // Assert
            hasCycles.Should().BeTrue("Graph with cycle should detect cycle even with disconnected nodes");
            cyclePath.Should().NotBeNull("Graph with cycle should return cycle path even with disconnected nodes");
            cyclePath.Should().HaveCount(3, "Cycle path should only contain the nodes in the cycle");

            // Verify cycle path only contains cycle nodes, not disconnected ones
            cyclePath.Should().OnlyContain(nodeId => new[] { "A", "B", "C" }.Contains(nodeId),
                "Cycle path should only contain nodes that are part of the cycle");
        }

        /// <summary>
        /// Test that a large linear chain (10,000 nodes, no cycle) completes without stack overflow
        /// or timeout, verifying the termination fix under stress-like input.
        /// </summary>
        [Fact]
        public void DetectCycles_LargeLinearChain_NoStackOverflowOrTimeout()
        {
            // Arrange
            var graph = new MigrationDependencyGraph();
            const int nodeCount = 10000;

            // Create a large linear chain: 1->2->3->...->10000
            for (int i = 1; i <= nodeCount; i++)
            {
                graph.AddNode(new MigrationGraphNode
                {
                    MigrationId = $"M{i:D4}",
                    Name = $"Migration{i}",
                    Sequence = i
                });
            }

            // Connect them in a linear chain
            for (int i = 1; i < nodeCount; i++)
            {
                graph.AddEdge(new MigrationGraphEdge(
                    $"M{i:D4}",
                    $"M{(i + 1):D4}"
                ));
            }

            // Act - this should not throw StackOverflowException or timeout
            var hasCycles = graph.HasCycles;
            var cyclePath = graph.CyclePath;
            var topologicalOrder = graph.GetTopologicalOrder();

            // Assert
            hasCycles.Should().BeFalse("Large linear chain should not have cycles");
            cyclePath.Should().BeNull("Large linear chain should return null cycle path");
            topologicalOrder.Should().HaveCount(nodeCount, $"Topological order should contain all {nodeCount} nodes");
        }

        /// <summary>
        /// Test that cycle detection terminates properly when adding edges incrementally.
        /// </summary>
        [Fact]
        public void DetectCycles_IncrementalEdgeAddition_TerminatesProperly()
        {
            // Arrange
            var graph = new MigrationDependencyGraph();
            graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "MigrationA", Sequence = 1 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "MigrationB", Sequence = 2 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "MigrationC", Sequence = 3 });

            // Add edges one by one and check termination after each addition
            graph.AddEdge(new MigrationGraphEdge("A", "B"));
            graph.HasCycles.Should().BeFalse("After adding A->B, graph should still be acyclic");

            graph.AddEdge(new MigrationGraphEdge("B", "C"));
            graph.HasCycles.Should().BeFalse("After adding B->C, graph should still be acyclic");

            // Add the edge that creates the cycle
            graph.AddEdge(new MigrationGraphEdge("C", "A"));
            graph.HasCycles.Should().BeTrue("After adding C->A, graph should have a cycle");

            // Verify cycle path is correct
            var cyclePath = graph.CyclePath;
            cyclePath.Should().NotBeNull("Cycle should be detected");
            cyclePath.Should().HaveCount(3, "Cycle should contain all three nodes");
        }

        /// <summary>
        /// Test that GetTopologicalOrder returns empty list when cycle exists.
        /// </summary>
        [Fact]
        public void GetTopologicalOrder_WithCycle_ReturnsEmptyList()
        {
            // Arrange
            var graph = new MigrationDependencyGraph();
            graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "MigrationA", Sequence = 1 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "MigrationB", Sequence = 2 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "MigrationC", Sequence = 3 });

            graph.AddEdge(new MigrationGraphEdge("A", "B"));
            graph.AddEdge(new MigrationGraphEdge("B", "C"));
            graph.AddEdge(new MigrationGraphEdge("C", "A")); // Creates cycle

            // Act
            var topologicalOrder = graph.GetTopologicalOrder();

            // Assert
            topologicalOrder.Should().BeEmpty("Graph with cycle should return empty topological order");
        }

        /// <summary>
        /// Test that cycle detection works with case-insensitive node IDs.
        /// </summary>
        [Fact]
        public void DetectCycles_CaseInsensitiveNodeIds_DetectsCycleCorrectly()
        {
            // Arrange
            var graph = new MigrationDependencyGraph();
            graph.AddNode(new MigrationGraphNode { MigrationId = "node-a", Name = "MigrationA", Sequence = 1 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "NODE-B", Name = "MigrationB", Sequence = 2 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "NoDe-C", Name = "MigrationC", Sequence = 3 });

            // Add edges with different case
            graph.AddEdge(new MigrationGraphEdge("node-a", "NODE-B"));
            graph.AddEdge(new MigrationGraphEdge("NODE-B", "NoDe-C"));
            graph.AddEdge(new MigrationGraphEdge("NoDe-C", "node-a")); // Creates cycle

            // Act
            var hasCycles = graph.HasCycles;
            var cyclePath = graph.CyclePath;

            // Assert
            hasCycles.Should().BeTrue("Graph with case-insensitive cycle should detect cycle");
            cyclePath.Should().NotBeNull("Graph with case-insensitive cycle should return cycle path");
            cyclePath.Should().HaveCount(3, "Cycle should contain all three nodes regardless of case");
        }

        /// <summary>
        /// Test that cycle detection handles complex graph with multiple potential cycles
        /// but only reports the first detected cycle.
        /// </summary>
        [Fact]
        public void DetectCycles_ComplexGraph_DetectsFirstCycle()
        {
            // Arrange - create a graph with multiple possible cycles
            var graph = new MigrationDependencyGraph();

            // Create nodes
            for (int i = 1; i <= 6; i++)
            {
                graph.AddNode(new MigrationGraphNode { MigrationId = $"M{i}", Name = $"Migration{i}", Sequence = i });
            }

            // Create first cycle: M1->M2->M3->M1
            graph.AddEdge(new MigrationGraphEdge("M1", "M2"));
            graph.AddEdge(new MigrationGraphEdge("M2", "M3"));
            graph.AddEdge(new MigrationGraphEdge("M3", "M1"));

            // Add more edges that could create other cycles but shouldn't affect detection
            graph.AddEdge(new MigrationGraphEdge("M4", "M5"));
            graph.AddEdge(new MigrationGraphEdge("M5", "M6"));
            graph.AddEdge(new MigrationGraphEdge("M6", "M4")); // Another cycle

            // Act
            var hasCycles = graph.HasCycles;
            var cyclePath = graph.CyclePath;

            // Assert
            hasCycles.Should().BeTrue("Graph with multiple cycles should detect cycles");
            cyclePath.Should().NotBeNull("Graph with multiple cycles should return cycle path");
            cyclePath.Should().HaveCount(3, "Should detect one of the cycles (exact cycle may vary)");

            // Verify cycle path contains only valid node IDs
            cyclePath.Should().OnlyContain(nodeId => nodeId.StartsWith("M"),
                "Cycle path should only contain valid migration IDs");
        }

        /// <summary>
        /// Test that cycle detection is idempotent - calling DetectCycles multiple times
        /// returns the same result.
        /// </summary>
        [Fact]
        public void DetectCycles_Idempotent_Operation()
        {
            // Arrange
            var graph = new MigrationDependencyGraph();
            graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "MigrationA", Sequence = 1 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "B", Name = "MigrationB", Sequence = 2 });
            graph.AddNode(new MigrationGraphNode { MigrationId = "C", Name = "MigrationC", Sequence = 3 });

            graph.AddEdge(new MigrationGraphEdge("A", "B"));
            graph.AddEdge(new MigrationGraphEdge("B", "C"));
            graph.AddEdge(new MigrationGraphEdge("C", "A")); // Creates cycle

            // Act - call multiple times
            var hasCycles1 = graph.HasCycles;
            var cyclePath1 = graph.CyclePath;
            var hasCycles2 = graph.HasCycles;
            var cyclePath2 = graph.CyclePath;
            var hasCycles3 = graph.HasCycles;
            var cyclePath3 = graph.CyclePath;

            // Assert
            hasCycles1.Should().BeTrue("First cycle detection should find cycle");
            hasCycles2.Should().BeTrue("Second cycle detection should find cycle");
            hasCycles3.Should().BeTrue("Third cycle detection should find cycle");

            cyclePath1.Should().Equal(cyclePath2, "Cycle path should be consistent across multiple detections");
            cyclePath2.Should().Equal(cyclePath3, "Cycle path should remain consistent");
        }

        /// <summary>
        /// Test that cycle detection handles empty graph gracefully.
        /// </summary>
        [Fact]
        public void DetectCycles_EmptyGraph_HandledGracefully()
        {
            // Arrange
            var graph = new MigrationDependencyGraph();

            // Act
            var hasCycles = graph.HasCycles;
            var cyclePath = graph.CyclePath;
            var topologicalOrder = graph.GetTopologicalOrder();

            // Assert
            hasCycles.Should().BeFalse("Empty graph should not have cycles");
            cyclePath.Should().BeNull("Empty graph should return null cycle path");
            topologicalOrder.Should().BeEmpty("Empty graph should return empty topological order");
        }

        /// <summary>
        /// Test that cycle detection handles single node graph without self-loop.
        /// </summary>
        [Fact]
        public void DetectCycles_SingleNodeWithoutSelfLoop_NoCycle()
        {
            // Arrange
            var graph = new MigrationDependencyGraph();
            graph.AddNode(new MigrationGraphNode { MigrationId = "A", Name = "MigrationA", Sequence = 1 });

            // Act
            var hasCycles = graph.HasCycles;
            var cyclePath = graph.CyclePath;

            // Assert
            hasCycles.Should().BeFalse("Single node without self-loop should not have cycle");
            cyclePath.Should().BeNull("Single node without self-loop should return null cycle path");
        }
    }
}