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

        public List<ConnectionNode> Nodes { get; set; } = [];

        public void AddNode(ConnectionNode node)
        {
            Nodes.Append(node);
        }

        private readonly ObservableCollection<Winding> _windings = new();
        public IList<Winding> Windings => _windings;

        public double eps_oil = 1.0; //2.2;
        public double ins_loss_factor = 0.03;

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
            
            var outer_bdry = Core.GenerateGeometry(ref geometry);

            List<GeomLineLoop> conductorins_bdrys = new List<GeomLineLoop>();
            
            foreach (Winding wdg in Windings)
            {
                conductorins_bdrys.AddRange(wdg.GenerateGeometry(ref geometry));
            }

            var interior_surface = geometry.AddSurface(outer_bdry, conductorins_bdrys.ToArray());
            TagManager.TagEntityByString(interior_surface, "InteriorDomain");

            return geometry;
        }
    }
}
