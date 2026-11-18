using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEMLib
{
    public class EntityGroup : INamed
    {
        public string Name { get; init; }
        public int Dimension { get; set; }
        public List<int> AttributeIds { get; set; } = new();
    }
}
