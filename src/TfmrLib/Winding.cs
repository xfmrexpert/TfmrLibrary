using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using GeometryLib;
using LinAlg = MathNet.Numerics.LinearAlgebra;

namespace TfmrLib
{
    public class Winding
    {
        public int Id { get; internal set; }
        public string Label { get; set; } // HV, LV, RW, etc.

        public Transformer ParentTransformer { get; internal set; }

        private readonly ObservableCollection<WindingSegment> _segments = new();
        public IList<WindingSegment> Segments => _segments;

        public List<Terminal> Terminals { get; } = new();

        // Read-only computed property
        public int NumTurns
        {
            get
            {
                int n = 0;
                foreach (var seg in Segments)
                {
                    n += seg.Geometry?.NumTurns ?? 0;
                }
                return n;
            }
        }

        // Read-only computed property
        public int NumConductors
        {
            get
            {
                int n = 0;
                foreach (var seg in Segments)
                {
                    if (seg.Geometry != null)
                        n += seg.Geometry.NumParallelConductors * seg.Geometry.NumTurns;
                }
                return n;
            }
        }

        public Winding()
        {
            // Subscribe to collection changes to automatically set ParentWinding
            _segments.CollectionChanged += (sender, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (WindingSegment segment in e.NewItems)
                    {
                        segment.ParentWinding = this;
                        segment.Id = _segments.IndexOf(segment);
                        if (segment.Geometry != null)
                            segment.Geometry.ParentSegment = segment;
                    }
                }
            };
        }

        internal void Initialize(Transformer parent, int id)
        {
            ParentTransformer = parent;
            Id = id;

            for (int i = 0; i < Segments.Count; i++)
            {
                Segments[i].Initialize(this, parent.Graph, i);
            }

            foreach (var term in Terminals)
            {
                term.Initialize(this);
            }
        }

        public void AddSegment(WindingSegment segment)
        {
            segment.ParentWinding = this;
            _segments.Add(segment);
        }

    }
}
