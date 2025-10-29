using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
    public enum StartNodeLocation
    {
        Top,
        Bottom
    }
    
    public class WindingSegment : IConnectedEntity
    {
        public int Id { get; internal set; }
        public string Label { get; set; } // e.g. "HV Upper", "HV Lower"
        public Winding ParentWinding { get; internal set; }

        protected Node _a, _b;

        public Node StartNode => _a;
        public Node EndNode   => _b;

        public IReadOnlyList<Node> Ports => _ports;
        private Node[] _ports => new[] { _a, _b };

        private WindingGeometry _geometry;
        public WindingGeometry Geometry
        {
            get => _geometry;
            set
            {
                _geometry = value;
                if (value != null)
                    value.ParentSegment = this;
            }
        }

        public StartNodeLocation StartLocation { get; set; } = StartNodeLocation.Top;

        internal void Initialize(Winding parent, Graph graph, int id)
        {
            ParentWinding = parent;
            Id = id;

            // Create the nodes for this segment
            _a = graph.CreateNode($"W{parent.Id}_S{id}_Start");
            _b = graph.CreateNode($"W{parent.Id}_S{id}_End");

            // CRITICAL: Register this segment with the graph so it knows what's connected.
            graph.Register(this);

            if (Geometry != null)
            {
                Geometry.ParentSegment = this;
            }
        }

        public void RepointPort(Node oldNode, Node newNode)
        {
            if (ReferenceEquals(StartNode, oldNode))
            {
                _a = newNode;
            }
            if (ReferenceEquals(EndNode, oldNode))
            {
                _b = newNode;
            }
        }
    }
}
