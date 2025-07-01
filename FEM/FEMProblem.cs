using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib.FEM
{
    public class FEMProblem
    {
        public List<Region> Regions { get; set; } = new List<Region>();
        public List<BoundaryCondition> BoundaryConditions { get; set; } = new List<BoundaryCondition>();
        public List<Excitation> Excitations { get; set; } = new List<Excitation>();
    }
}
