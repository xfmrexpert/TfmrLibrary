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

    public class Terminal : ConnectionNode
    {
        public Connection ConnectionType { get; set; }
        
        public double Voltage_kV { get; set; }
        public double Rating_MVA { get; set; }
    }
    
}
