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
        public List<Source> Sources { get; set; } = new List<Source>(); // Sources are for things like current density or charge density (right-hand side terms)
        public List<Excitation> Excitations { get; set; } = new List<Excitation>(); // Excitations apply to ports or regions with prescribed values like current or voltage

        public virtual void Solve()
        {
            throw new NotImplementedException("Solve method not implemented for base FEMProblem class");
        }
    }
}
