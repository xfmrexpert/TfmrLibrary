using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib.FEM
{
    public class Scenario : INamed
    {
        public string Name { get; init; }
        public List<Excitation> Excitations { get; set; }
    }
}
