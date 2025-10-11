using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
    public enum StartNodeLocation
    {
        Top,
        Bottom
    }
    
    public class WindingSegment
    {
        public int Id { get; set; }
        public string Label { get; set; } // e.g. "HV Upper", "HV Lower"
        public Winding ParentWinding { get; set; }

        private WindingGeometry _geometry;
        public WindingGeometry Geometry
        {
            get => _geometry;
            set
            {
                _geometry = value;
                if (value != null)
                    value.ParentSegment = this;
            }
        }

        public ConnectionNode StartNode { get; set; } // By convention, the start node is +V
        public ConnectionNode EndNode { get; set; } // By convention, the end node is -V

        public StartNodeLocation StartLocation { get; set; } = StartNodeLocation.Top;

        public WindingSegment()
        {
            // Default constructor
        }

        public WindingSegment(Winding wdg)
        {
            ParentWinding = wdg;
        }

    }
}
