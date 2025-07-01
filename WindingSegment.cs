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
        public NodeConnection StartNode { get; set; }
        public NodeConnection EndNode { get; set; }
        public ElectricalConnection ConnectionType { get; set; } = ElectricalConnection.Series;
    }
}
