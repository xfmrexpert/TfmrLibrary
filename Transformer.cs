using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeometryLib;
using MathNet.Numerics.LinearAlgebra;

namespace TfmrLib
{
    public class Transformer
    {
        private Core _core;
        public Core Core
        {
            get => _core;
            set
            {
                _core = value;
                if (value != null)
                    _core.ParentTransformer = this;
            }
        }

        public TagManager TagManager { get; }   // Scoped to this transformer

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

        public Transformer(TagManager? tagManager = null)
        {
            Core = new Core(this);
            TagManager = tagManager ?? new TagManager();

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

        public Geometry GenerateGeometry()
        {
            var geometry = new Geometry();
            TagManager.ClearTags();
            // Left boundary (axis if core radius is 0)
            //var pt_origin = geometry.AddPoint(r_core, 0, 0.1);
            //var pt_axis_top = geometry.AddPoint(r_core, bdry_radius, 0.1);
            //var pt_axis_top_inf = geometry.AddPoint(r_core, 1.1 * bdry_radius, 0.1);
            //var pt_axis_bottom = geometry.AddPoint(r_core, -bdry_radius, 0.1);
            //var pt_axis_bottom_inf = geometry.AddPoint(r_core, -1.1 * bdry_radius, 0.1);
            //var axis = geometry.AddLine(pt_axis_bottom, pt_axis_top);
            //var axis_top_inf = geometry.AddLine(pt_axis_top, pt_axis_top_inf);
            //var axis_bottom_inf = geometry.AddLine(pt_axis_bottom_inf, pt_axis_bottom);
            //phyAxis = axis.AddTag();
            //var right_bdry = geometry.AddArc(pt_axis_top, pt_axis_bottom, bdry_radius, -Math.PI);
            //var right_bdry_inf = geometry.AddArc(pt_axis_top_inf, pt_axis_bottom_inf, 1.1 * bdry_radius, -Math.PI);
            //var outer_bdry = geometry.AddLineLoop(axis, right_bdry);
            //var outer_bdry_inf = geometry.AddLineLoop(axis_bottom_inf, right_bdry, axis_top_inf, right_bdry_inf);
            //phyExtBdry = outer_bdry.AddTag();
            var outer_bdry = Core.GenerateGeometry(ref geometry);

            List<GeomLineLoop> conductorins_bdrys = new List<GeomLineLoop>();
            
            foreach (Winding wdg in Windings)
            {
                conductorins_bdrys.AddRange(wdg.GenerateGeometry(ref geometry));
            }

            var interior_surface = geometry.AddSurface(outer_bdry, conductorins_bdrys.ToArray());
            TagManager.TagEntityByString(interior_surface, "InteriorDomain");

            //var inf_surface = geometry.AddSurface(outer_bdry_inf);
            //phyInf = inf_surface.AddTag();

            return geometry;
        }
    }
}
