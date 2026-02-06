using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
    public enum Connection
    {
        Wye,
        Delta,
        ZigZag
    }

    public class Terminal
    {
        public string Label { get; set; }
        
        // Configuration for connecting to the model
        public string? ConnectToSegmentLabel { get; set; }
        public int ConnectToSegmentIndex { get; set; } = -1;
        public bool ConnectToStartNode { get; set; } = true;
        public string? ConnectToTapLabel { get; set; }

        // The resolved node in the graph
        private Node? _internalNode;
        public Node InternalNode 
        { 
            get => _internalNode ?? throw new InvalidOperationException("Terminal has not been initialized/resolved to a Node yet.");
            set => _internalNode = value;
        }

        public Connection ConnectionType { get; set; }
        
        public double Voltage_kV { get; set; }
        public double Rating_MVA { get; set; }

        public void Initialize(Winding winding)
        {
            WindingSegment? segment = null;

            if (!string.IsNullOrEmpty(ConnectToSegmentLabel))
            {
                segment = winding.Segments.FirstOrDefault(s => s.Label == ConnectToSegmentLabel);
                if (segment == null) 
                    throw new Exception($"Terminal '{Label}' references missing segment '{ConnectToSegmentLabel}' in winding '{winding.Label}'.");
            }
            else if (ConnectToSegmentIndex >= 0)
            {
                if (ConnectToSegmentIndex < winding.Segments.Count)
                    segment = winding.Segments[ConnectToSegmentIndex];
                else
                    throw new Exception($"Terminal '{Label}' references invalid segment index {ConnectToSegmentIndex} in winding '{winding.Label}'.");
            }
            else
            {
                // Default behavior: connect to first segment Start if not specified? 
                // Or maybe this terminal is manually assigned later.
                return; 
            }

            if (segment != null)
            {
                if (!string.IsNullOrEmpty(ConnectToTapLabel))
                {
                    var tap = segment.Taps.FirstOrDefault(t => t.Label == ConnectToTapLabel);
                    if (tap != null && tap.Node != null)
                    {
                        InternalNode = tap.Node;
                    }
                    else
                    {
                        throw new Exception($"Terminal '{Label}' references missing or uninitialized tap '{ConnectToTapLabel}' in segment '{segment.Label}'.");
                    }
                }
                else
                {
                    InternalNode = ConnectToStartNode ? segment.StartNode : segment.EndNode;
                }
            }
        }
    }
    
}
