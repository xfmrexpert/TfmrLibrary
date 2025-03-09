using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TDAP;

namespace TfmrLib
{
    public class Transformer
    {
        public List<Winding> Windings { get; set; } = [];

        public double eps_oil = 1.0; //2.2;
        public double r_core;
        public double bdry_radius = 1.0; //radius of outer boundary of finite element model

        public int phyAir;
        public int phyExtBdry;
        public int phyAxis;
        public int phyInf;

        public Geometry GenerateGeometry(bool RestartNumbersPerDimension = true)
        {
            var geometry = new Geometry();

            // Setup axis and external boundaries
            phyAxis = geometry.NextLineTag; // 1;
            phyExtBdry = geometry.NextLineTag; // 2;
            phyAir = geometry.NextSurfaceTag;
            phyInf = geometry.NextSurfaceTag;

            // Left boundary (axis if core radius is 0)
            var pt_origin = geometry.AddPoint(r_core, 0, 0.1);
            var pt_axis_top = geometry.AddPoint(r_core, bdry_radius, 0.1);
            var pt_axis_top_inf = geometry.AddPoint(r_core, 1.1 * bdry_radius, 0.1);
            var pt_axis_bottom = geometry.AddPoint(r_core, -bdry_radius, 0.1);
            var pt_axis_bottom_inf = geometry.AddPoint(r_core, -1.1 * bdry_radius, 0.1);
            var axis = geometry.AddLine(pt_axis_bottom, pt_axis_top);
            var axis_top_inf = geometry.AddLine(pt_axis_top, pt_axis_top_inf);
            var axis_bottom_inf = geometry.AddLine(pt_axis_bottom_inf, pt_axis_bottom);
            axis.AttribID = phyAxis;

            var right_bdry = geometry.AddArc(pt_axis_top, pt_axis_bottom, bdry_radius, -Math.PI);
            var right_bdry_inf = geometry.AddArc(pt_axis_top_inf, pt_axis_bottom_inf, 1.1 * bdry_radius, -Math.PI);
            var outer_bdry = geometry.AddLineLoop(axis, right_bdry);
            var outer_bdry_inf = geometry.AddLineLoop(axis_bottom_inf, right_bdry, axis_top_inf, right_bdry_inf);
            outer_bdry.AttribID = phyExtBdry;

            List<GeomLineLoop> conductorins_bdrys = new List<GeomLineLoop>();
            int startTag = 0; 
            foreach (Winding wdg in Windings)
            {
                wdg.GenerateGeometry(ref geometry, startTag, RestartNumbersPerDimension);
            }

            var interior_surface = geometry.AddSurface(outer_bdry, conductorins_bdrys.ToArray());
            interior_surface.AttribID = phyAir;

            var inf_surface = geometry.AddSurface(outer_bdry_inf);
            inf_surface.AttribID = phyInf;

            return geometry;
        }

    }
}
