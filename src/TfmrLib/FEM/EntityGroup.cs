using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib.FEM
{
    public class EntityGroup : INamed
    {
        public string Name { get; init; }
        /// <summary>gmsh/MFEM dimension: 0=point, 1=curve, 2=surface, 3=volume.</summary>
        public int Dimension { get; set; }
        /// <summary>Physical-group tags (a.k.a. MFEM attributes) on the mesh.</summary>
        public List<int> AttributeIds { get; set; } = new();
    }
}
