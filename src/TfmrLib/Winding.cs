using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using LinAlg = MathNet.Numerics.LinearAlgebra;
using GeometryLib;
using System.ComponentModel;

namespace TfmrLib
{
    public class Winding
    {
        public int Id { get; internal set; }
        public string Label { get; set; } // HV, LV, RW, etc.
        
        public Transformer ParentTransformer { get; internal set; }

        private readonly ObservableCollection<WindingSegment> _segments = new();
        public IList<WindingSegment> Segments => _segments;

        public List<InternalConnection> InternalConnections { get; } = new();
        public List<Terminal> Terminals { get; } = new();

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

        internal void Initialize(Transformer parent, Graph graph, int id)
        {
            ParentTransformer = parent;
            Id = id;

            for (int i = 0; i < Segments.Count; i++)
            {
                Segments[i].Initialize(this, graph, i);
            }

            foreach (var conn in InternalConnections)
            {
                conn.Apply(this, graph);
            }

            foreach (var term in Terminals)
            {
                term.Initialize(this);
            }
        }

        public GeomLineLoop[] GenerateGeometry(ref Geometry geometry)
        {
            var rtnLoop = new List<GeomLineLoop>();
            foreach (var seg in Segments)
            {
                if (seg.Geometry != null)
                    rtnLoop.AddRange(seg.Geometry.GenerateGeometry(ref geometry));
            }
            return rtnLoop.ToArray();
        }

        public void AddSegment(WindingSegment segment)
        {
            segment.ParentWinding = this;
            _segments.Add(segment);
        }

    }
}
