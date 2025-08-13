using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using LinAlg = MathNet.Numerics.LinearAlgebra;
using GeometryLib;

namespace TfmrLib
{
    public class Winding
    {
        public string Name { get; set; } // HV, LV, RW, etc.
        
        public Transformer ParentTransformer { get; set; }

        private readonly ObservableCollection<WindingSegment> _segments = new();
        public IList<WindingSegment> Segments => _segments;

        public double eps_paper; //3.5;
        public double rho_c; //ohm-m;

        public double dist_wdg_tank_right;
        public double dist_wdg_tank_top;
        public double dist_wdg_tank_bottom;
        
        public double Rs;
        public double Rl;
        public double Ls;
        public double Ll;
        public double ResistanceFudgeFactor = 1.0;

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
            Rs = 0.0;
            Rl = 0.0;
            Ls = 0.0;
            Ll = 0.0;

            // Subscribe to collection changes to automatically set ParentWinding
            _segments.CollectionChanged += (sender, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (WindingSegment segment in e.NewItems)
                    {
                        segment.ParentWinding = this;
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
            _segments.Add(segment);
        }
    }
}
