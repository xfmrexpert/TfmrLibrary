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
        public int phyAir2;
        public int phyExtBdry;
        public int phyAxis;
        public int phyInf;
        public int phyCore;

        public Geometry GenerateGeometry(bool RestartNumbersPerDimension = true)
        {
            var geometry = new Geometry();
            geometry.RestartNumberingPerDimension = RestartNumbersPerDimension;

            // Setup axis and external boundaries
            phyAxis = geometry.NextLineTag; // 1;
            phyExtBdry = geometry.NextLineTag; // 2;
            phyCore = geometry.NextSurfaceTag;
            phyAir = geometry.NextSurfaceTag;
            phyAir2 = geometry.NextSurfaceTag;
            phyInf = geometry.NextSurfaceTag;

            double core_thickness = 0.1982/1000;

            // Left boundary (axis if core radius is 0)
            var pt_origin = geometry.AddPoint(0, 0, 0.1);
            var pt_axis_top = geometry.AddPoint(0, bdry_radius, 0.1);
            var pt_axis_top_inf = geometry.AddPoint(0, 1.1 * bdry_radius, 0.1);
            var pt_axis_bottom = geometry.AddPoint(0, -bdry_radius, 0.1);
            var pt_axis_bottom_inf = geometry.AddPoint(0, -1.1 * bdry_radius, 0.1);
            var axis = geometry.AddLine(pt_axis_bottom, pt_axis_top);
            var axis_top_inf = geometry.AddLine(pt_axis_top, pt_axis_top_inf);
            var axis_bottom_inf = geometry.AddLine(pt_axis_bottom_inf, pt_axis_bottom);
            axis.AttribID = phyAxis;

            var pt_core_bottom_left = geometry.AddPoint(r_core, -bdry_radius, 0.1);
            var pt_core_top_left = geometry.AddPoint(r_core, bdry_radius, 0.1);
            var pt_core_bottom_right = geometry.AddPoint(r_core + core_thickness, -bdry_radius, 0.1);
            var pt_core_top_right = geometry.AddPoint(r_core + core_thickness, bdry_radius, 0.1);
            var pt_core_bottom_right_inf = geometry.AddPoint(r_core + core_thickness, -1.1 * bdry_radius, 0.1);
            var pt_core_top_right_inf = geometry.AddPoint(r_core + core_thickness, 1.1 * bdry_radius, 0.1);

            var core_left = geometry.AddLine(pt_core_bottom_left, pt_core_top_left);
            var core_top = geometry.AddLine(pt_core_top_left, pt_core_top_right);
            var core_right = geometry.AddLine(pt_core_top_right, pt_core_bottom_right);
            var core_bottom = geometry.AddLine(pt_core_bottom_right, pt_core_bottom_left);

            var right_bdry = geometry.AddArc(pt_core_top_right, pt_core_bottom_right, bdry_radius, -Math.PI);
            var right_bdry_inf = geometry.AddArc(pt_core_top_right_inf, pt_core_bottom_right_inf, 1.1 * bdry_radius, -Math.PI);
            var bdry_top = geometry.AddLine(pt_axis_top, pt_core_top_left);
            var bdry_bottom = geometry.AddLine(pt_core_bottom_left, pt_axis_bottom);
            var bdry_inf_top = geometry.AddLine(pt_axis_top_inf, pt_core_top_right_inf);
            var bdry_inf_bottom = geometry.AddLine(pt_core_bottom_right_inf, pt_axis_bottom_inf);

            var core_bdry = geometry.AddLineLoop(core_left, core_top, core_right, core_bottom);

            var axis_core_bdry = geometry.AddLineLoop(axis, bdry_top, core_left, bdry_bottom);

            var outer_bdry = geometry.AddLineLoop(axis, bdry_top, core_top, right_bdry, core_bottom, bdry_bottom);
            var outer_bdry_inf = geometry.AddLineLoop(axis_bottom_inf, bdry_bottom, core_bottom, right_bdry, core_top, bdry_top, axis_top_inf, bdry_inf_top, right_bdry_inf, bdry_inf_bottom);
            outer_bdry.AttribID = phyExtBdry;

            List<GeomLineLoop> conductorins_bdrys = new List<GeomLineLoop>();
            
            foreach (Winding wdg in Windings)
            {
                conductorins_bdrys.AddRange(wdg.GenerateGeometry(ref geometry));
            }

            var core_surface = geometry.AddSurface(core_bdry);
            core_surface.AttribID = phyCore;
            conductorins_bdrys.Add(core_bdry);
            var interior_surface = geometry.AddSurface(outer_bdry, conductorins_bdrys.ToArray());
            interior_surface.AttribID = phyAir;

            var axis_core_surface = geometry.AddSurface(axis_core_bdry);
            axis_core_surface.AttribID = phyAir2;

            var inf_surface = geometry.AddSurface(outer_bdry_inf);
            inf_surface.AttribID = phyInf;

            return geometry;
        }

    }
}
