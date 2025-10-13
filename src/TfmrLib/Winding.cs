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
        public int Id { get; set; }
        public string Label { get; set; } // HV, LV, RW, etc.

        public ConnectionNode StartNode { get; set; }
        public ConnectionNode EndNode { get; set; }
        
        public Transformer ParentTransformer { get; set; }

        private readonly ObservableCollection<WindingSegment> _segments = new();
        public IList<WindingSegment> Segments => _segments;

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

        public GeomLineLoop[] GenerateGeometry(ref Geometry geometry)
        {
            GeomLineLoop[] rtnLoop = new GeomLineLoop[0];
            foreach (var seg in Segments)
            {
                if (seg.Geometry != null)
                    rtnLoop = rtnLoop.Concat(seg.Geometry.GenerateGeometry(ref geometry)).ToArray();
            }
            return rtnLoop;
        }

        public void AddSegment(WindingSegment segment)
        {
            segment.ParentWinding = this;
            _segments.Add(segment);
        }

        public void AddSeriesSegments(int n = 1)
        {
            for (int i = 0; i < n; i++)
            {
                var seg = new WindingSegment();
                seg.StartNode = StartNode;
                seg.EndNode = EndNode;
                AddSegment(seg);
            }
        }
        
        public void AddParallelSegments(int n = 1)
        {
            for (int i = 0; i < n; i++)
            {
                var seg = new WindingSegment();
                seg.StartNode = StartNode;
                if (i < n - 1)
                {
                    var int_node = new ConnectionNode();
                }
                else
                {
                    seg.EndNode = EndNode;
                }
                AddSegment(seg);
            }
        }
    }
}
