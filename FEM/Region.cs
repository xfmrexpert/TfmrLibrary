using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib.FEM
{
    public partial class Region
    {
        public string Name { get; set; }
        public List<int> Tags { get; set; } = new List<int>();
        public Material? Material { get; set; }

        // Region-specific properties override material properties
        public Dictionary<string, double> Properties { get; } = new();

        public Region SetMaterial(Material m) { Material = m; return this; }
        public Region Property(string prop, double value) { Properties[prop] = value; return this; }

        public bool TryGetProperty(string prop, out double value)
        {
            if (Properties.TryGetValue(prop, out value)) return true;
            if (Material != null && Material.TryGet(prop, out value)) return true;
            return false;
        }
    }
}
