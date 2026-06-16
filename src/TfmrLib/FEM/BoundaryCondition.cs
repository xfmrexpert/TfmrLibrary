using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib.FEM
{
    public class BoundaryCondition : INamed
    {
        public string Name { get; init; }
        public string EntityGroupName { get; set; } = string.Empty;
    }

    public class NeumannBoundaryCondition : BoundaryCondition
    {
        public double Flux { get; set; }
    }

    public class DirichletBoundaryCondition : BoundaryCondition
    {
        public double Potential { get; set; }
    }
}
