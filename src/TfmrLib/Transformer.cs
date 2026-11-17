using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CircuitLib;
using GeometryLib;
using MathNet.Numerics.LinearAlgebra;

namespace TfmrLib
{
    public class Transformer
    {
        private ICore _core;
        public ICore Core
        {
            get => _core;
            set
            {
                _core = value;
                if (value != null)
                    _core.ParentTransformer = this;
            }
        }

        //public TagManager TagManager { get; } = new();  // Scoped to this transformer

        public Graph Graph { get; } = new();

        public List<Terminal> Terminals { get; } = new();

        private readonly ObservableCollection<Winding> _windings = new();
        public IList<Winding> Windings => _windings;

        public IMaterial InsulatingFluid { get; set; } = Oil.Instance;

        public double ins_loss_factor = 0.03;

        public Transformer()
        {
            _core = new Core(this)
            {
                ParentTransformer = this
            };

            _windings.CollectionChanged += (sender, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (Winding winding in e.NewItems)
                    {
                        winding.ParentTransformer = this;
                        winding.Id = Windings.IndexOf(winding);
                        foreach (var segment in winding.Segments)
                        {
                            segment.ParentWinding = winding;
                            if (segment.Geometry != null)
                                segment.Geometry.ParentSegment = segment;
                        }
                    }
                }
            };
        }

        public Transformer Initialize()
        {
            if (Core != null)
            {
                Core.ParentTransformer = this;
            }

            for (int i = 0; i < Windings.Count; i++)
            {
                Windings[i].Initialize(this, i);
            }
            return this;
        }

        public Vector<double> GetTurnLengths_m()
        {
            int total_cond = Windings.Sum(wdg => wdg.NumConductors);
            var length_vector = Vector<double>.Build.Dense(total_cond);
            int idx = 0;
            foreach (var wdg in Windings)
            {
                foreach (var segment in wdg.Segments)
                {
                    var seg_length_vector = segment.Geometry.GetTurnLengths_m();
                    for (int i = 0; i < seg_length_vector.Count; i++)
                    {
                        length_vector[idx] = seg_length_vector[i];
                        idx++;
                    }
                }
            }
            return length_vector;
        }

        public void AddWinding(Winding winding)
        {
            winding.ParentTransformer = this;
            _windings.Add(winding);
        }

        public int NumConductors
        {
            get
            {
                return Windings.Sum(wdg => wdg.NumConductors);
            }
        }

        public Graph GenerateExtendedGraph()
        {
            Graph ext_graph = new();
            // This assumes each terminal is not connected to another terminal.
            // We'll probably have to revisit this
            foreach (var term in Terminals)
            {
                var startNode = term.InternalNode;
                foreach (var branch in Graph.GetBranchesForNode(startNode))
                {
                    // We need a way to connect the branch to the underlying winding details
                    // Should branches be WindingSegments? How would that effect WindingSegments
                    // with tap?
                }
            }

            return ext_graph;
        }
    }
}
