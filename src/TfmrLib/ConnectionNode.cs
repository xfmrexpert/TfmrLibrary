using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
    public class ConnectionNode
    {
        public string NodeId { get; set; }     // e.g. "H1", "X0", "R5"

        public List<WindingSegment> IncidentSegments { get; internal set; } = [];

        public void ConnectTo(ConnectionNode to)
        {
            foreach (var seg in IncidentSegments)
            {
                if (ReferenceEquals(seg.StartNode, this)) seg.StartNode = to;
                if (ReferenceEquals(seg.EndNode, this)) seg.EndNode = to;
                if (!to.IncidentSegments.Contains(seg))
                {
                    to.IncidentSegments.Add(seg);
                }
            }
        }
    }
}
