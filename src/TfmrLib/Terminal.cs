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
        public Node InternalNode { get; set; }
        public Connection ConnectionType { get; set; }
        
        public double Voltage_kV { get; set; }
        public double Rating_MVA { get; set; }
    }
    
}
