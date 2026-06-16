using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib.FEM
{
    public class Terminal : INamed
    {
        public string Name { get; init; }
        public EntityGroup EntityGroup { get; set; }
        public Quantity ExcitationType { get; set; }
    }
}
