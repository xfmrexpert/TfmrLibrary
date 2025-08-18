using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
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

        public ConnectionNode StartNode { get; set; }
        public ConnectionNode EndNode { get; set; }

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
