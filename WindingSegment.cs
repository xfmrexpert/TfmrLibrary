using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
    public class WindingSegment
    {
        public string Label { get; set; } // e.g. "HV Upper", "HV Lower"
        public WindingGeometry Geometry { get; set; }
        public ConnectionNode StartNode { get; set; }
        public ConnectionNode EndNode { get; set; }
    }
}
