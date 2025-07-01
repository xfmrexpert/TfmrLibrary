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
}
