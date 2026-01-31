using System.Collections.Generic;
using TfmrLib;
using Xunit;

namespace TfmrLib.Tests
{
    public class GraphTests
    {
        // specific mock for testing purposes within this file
        private class MockEntity : IConnectedEntity
        {
            private List<Node> _ports = new();
            public IReadOnlyList<Node> Ports => _ports;

            public MockEntity(params Node[] nodes)
            {
                _ports.AddRange(nodes);
            }

            public void RepointPort(Node oldNode, Node newNode)
            {
                for (int i = 0; i < _ports.Count; i++)
                {
                    if (ReferenceEquals(_ports[i], oldNode))
                    {
                        _ports[i] = newNode;
                    }
                }
            }
        }

        [Fact]
        public void CreateNode_ShouldAssignUniqueIds()
        {
            var graph = new Graph();
            var n1 = graph.CreateNode("n1");
            var n2 = graph.CreateNode("n2");

            Assert.NotNull(n1);
            Assert.NotNull(n2);
            Assert.NotEqual(n1.Id, n2.Id);
            Assert.Equal("n1", n1.Tag);
            Assert.Equal("n2", n2.Tag);
        }

        [Fact]
        public void Union_ShouldRepointEntityPorts()
        {
            var graph = new Graph();
            var n1 = graph.CreateNode("n1");
            var n2 = graph.CreateNode("n2");

            var entity = new MockEntity(n1);
            graph.Register(entity);

            // Pre-condition
            Assert.Equal(n1, entity.Ports[0]);

            // Act: Merge n1 into n2
            var survivor = graph.Union(n1, n2);

            // Assert
            Assert.Equal(n2, survivor);
            Assert.Equal(n2, entity.Ports[0]);
        }

        [Fact]
        public void NodeConnectTo_ShouldUnionNodes()
        {
            var graph = new Graph();
            var n1 = graph.CreateNode("n1");
            var n2 = graph.CreateNode("n2");

            var entity = new MockEntity(n1);
            graph.Register(entity);

            // Act
            n1.ConnectTo(n2);

            // Assert
            Assert.Equal(n2, entity.Ports[0]);
        }
        
        [Fact]
        public void Union_SameNode_ShouldDoNothing()
        {
             var graph = new Graph();
             var n1 = graph.CreateNode("n1");
             var entity = new MockEntity(n1);
             graph.Register(entity);
             
             // Act
             var survivor = graph.Union(n1, n1);
             
             // Assert
             Assert.Equal(n1, survivor);
             Assert.Equal(n1, entity.Ports[0]);
        }

        [Fact]
        public void Short_ShouldConnectNodes()
        {
            var graph = new Graph();
            var n1 = graph.CreateNode("A");
            var n2 = graph.CreateNode("B");
            var entity = new MockEntity(n1);
            graph.Register(entity);

            graph.Short(n1, n2);

            Assert.Equal(n2, entity.Ports[0]);
        }
    }

    public class TerminalTests
    {
        [Fact]
        public void Terminal_Properties_RoundTrip()
        {
            var graph = new Graph();
            var node = graph.CreateNode();
            
            var terminal = new Terminal
            {
                Label = "HV1",
                InternalNode = node,
                ConnectionType = Connection.Delta,
                Voltage_kV = 115.0,
                Rating_MVA = 30.0
            };

            Assert.Equal("HV1", terminal.Label);
            Assert.Equal(node, terminal.InternalNode);
            Assert.Equal(Connection.Delta, terminal.ConnectionType);
            Assert.Equal(115.0, terminal.Voltage_kV);
            Assert.Equal(30.0, terminal.Rating_MVA);
        }
    }
}
