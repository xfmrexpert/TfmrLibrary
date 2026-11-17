using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CircuitLib;

namespace TfmrLib
{
    public enum StartNodeLocation
    {
        Top,
        Bottom
    }

    public class WindingSegment
    {
        public int Id { get; internal set; }
        public string Label { get; set; } // e.g. "HV Upper", "HV Lower"
        public Winding ParentWinding { get; internal set; }

        // Taps configuration
        public List<TapDefinition> Taps { get; } = new();

        // Branches generated from Taps
        public List<Branch> Branches { get; } = new();

        // Convenience properties
        public Node StartNode => Branches.Any() ? Branches[0].StartNode : throw new InvalidOperationException("Segment not initialized");
        public Node EndNode => Branches.Any() ? Branches.Last().EndNode : throw new InvalidOperationException("Segment not initialized");

        public IReadOnlyList<Node> Nodes
        {
            get
            {
                if (Branches.Count == 0) return new List<Node>();
                var list = new List<Node> { Branches[0].StartNode };
                foreach (var b in Branches) list.Add(b.EndNode);
                return list;
            }
        }

        private SegmentGeometry _geometry;
        public SegmentGeometry Geometry
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

            if (Geometry is null)
            {
                throw new ArgumentNullException("Geometry");
            }
            else
            {
                Geometry.ParentSegment = this;
            }

            int totalTurns = Geometry?.NumTurns ?? 0;

            Branches.Clear();
            Node startNode = null;
            List<Node> tapNodes = [];
            Node endNode = null;

            for (int p = 0; p < Geometry.NumParallelConductors; p++)
            {
                // Create the shared start node only for the first parallel branch, then reuse it
                if (p == 0)
                {
                    startNode = graph.CreateNode($"Wdg{parent.Id}_Seg{id}_Start");
                }

                List<Node> nodes = [];
                nodes.Add(startNode);

                var cuts = new List<int> { 0 };

                if (Taps.Count > 0)
                {
                    // Sort taps by turn number
                    // Note this will quietly drop taps with TurnNumber >= NumTurns
                    var sortedTaps = Taps.OrderBy(t => t.TurnNumber).Where(t => t.TurnNumber > 0 && t.TurnNumber < totalTurns).ToList();

                    // Define "cuts" (discrete sections between terminal nodes, e.g. start/end or tap) - 0, taps..., total
                    cuts.AddRange(sortedTaps.Select(t => t.TurnNumber));

                    // Tap Nodes
                    for (int i = 0; i < sortedTaps.Count; i++)
                    {
                        var tap = sortedTaps[i];
                        if (p == 0)
                        {
                            var n = graph.CreateNode($"Wdg{parent.Id}_Seg{id}_Tap_{tap.Label}");
                            tapNodes.Add(n);
                        }
                        tap.Node = tapNodes[i];
                        nodes.Add(tap.Node);
                    }
                }

                // Create the shared end node only for the first parallel branch, then reuse it
                if (p == 0)
                {
                    endNode = graph.CreateNode($"Wdg{parent.Id}_Seg{id}_End");
                }

                nodes.Add(endNode);

                cuts.Add(totalTurns);


                // Create Branches
                for (int i = 0; i < cuts.Count - 1; i++)
                {
                    int startTurn = cuts[i];
                    int endTurn = cuts[i + 1];
                    int numBranchTurns = endTurn - startTurn;

                    var branch = new Branch(
                        i,
                        nodes[i],
                        nodes[i + 1]
                    );

                    Branches.Add(branch);
                    graph.Register(branch);
                }
            }
        }
    }
}
