using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeometryLib;
using MathNet.Numerics.Providers.LinearAlgebra;

namespace TfmrLib
{
    public class Transformer
    {
        public Core Core { get; set; } = new Core();
        
        private readonly ObservableCollection<Winding> _windings = new();
        public IList<Winding> Windings => _windings;

        public double eps_oil = 1.0; //2.2;
        public double ins_loss_factor = 0.03;
        public double r_core;
        public double bdry_radius = 1.0; //radius of outer boundary of finite element model

        public int phyAir;
        public int phyExtBdry;
        public int phyAxis;
        public int phyInf;

        public Transformer()
        {
            // Subscribe to collection changes to automatically set ParentTransformer
            _windings.CollectionChanged += (sender, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (Winding winding in e.NewItems)
                    {
                        winding.ParentTransformer = this;
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

        public void AddWinding(Winding winding)
        {
            _windings.Add(winding);
        }

        public int NumConductors
        {
            get
            {
                return Windings.Sum(wdg => wdg.NumConductors);
            }
        }

        public Geometry GenerateGeometry()
        {
            var geometry = new Geometry();

            // Left boundary (axis if core radius is 0)
            var pt_origin = geometry.AddPoint(r_core, 0, 0.1);
            var pt_axis_top = geometry.AddPoint(r_core, bdry_radius, 0.1);
            var pt_axis_top_inf = geometry.AddPoint(r_core, 1.1 * bdry_radius, 0.1);
            var pt_axis_bottom = geometry.AddPoint(r_core, -bdry_radius, 0.1);
            var pt_axis_bottom_inf = geometry.AddPoint(r_core, -1.1 * bdry_radius, 0.1);
            var axis = geometry.AddLine(pt_axis_bottom, pt_axis_top);
            var axis_top_inf = geometry.AddLine(pt_axis_top, pt_axis_top_inf);
            var axis_bottom_inf = geometry.AddLine(pt_axis_bottom_inf, pt_axis_bottom);
            phyAxis = axis.AddTag();
            var right_bdry = geometry.AddArc(pt_axis_top, pt_axis_bottom, bdry_radius, -Math.PI);
            var right_bdry_inf = geometry.AddArc(pt_axis_top_inf, pt_axis_bottom_inf, 1.1 * bdry_radius, -Math.PI);
            var outer_bdry = geometry.AddLineLoop(axis, right_bdry);
            var outer_bdry_inf = geometry.AddLineLoop(axis_bottom_inf, right_bdry, axis_top_inf, right_bdry_inf);
            phyExtBdry = outer_bdry.AddTag();

            List<GeomLineLoop> conductorins_bdrys = new List<GeomLineLoop>();
            
            foreach (Winding wdg in Windings)
            {
                conductorins_bdrys.AddRange(wdg.GenerateGeometry(ref geometry));
            }

            var interior_surface = geometry.AddSurface(outer_bdry, conductorins_bdrys.ToArray());
            phyAir = interior_surface.AddTag();

            var inf_surface = geometry.AddSurface(outer_bdry_inf);
            phyInf = inf_surface.AddTag();

            return geometry;
        }
    }
}
