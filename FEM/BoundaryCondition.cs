using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib.FEM
{
    public class BoundaryCondition
    {
        public string Name { get; set; }
        public List<int> Tags { get; set; }
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
