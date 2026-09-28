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
           
            
        }
    }
    
}
