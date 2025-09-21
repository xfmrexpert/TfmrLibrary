using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib.FEM
{
    public class FEMProblem
    {
        public List<Material> Materials { get; set; } = new List<Material>();
        public List<Region> Regions { get; set; } = new List<Region>();
        public List<BoundaryCondition> BoundaryConditions { get; set; } = new List<BoundaryCondition>();
        public List<Excitation> Excitations { get; set; } = new List<Excitation>();

        public virtual void Solve()
        {
            throw new NotImplementedException("Solve method not implemented for base FEMProblem class");
        }
    }
}
