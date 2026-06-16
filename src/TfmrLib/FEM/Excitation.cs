using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib.FEM
{
    public enum Quantity
    {
        Voltage = 0,
        Current = 1
    }

    public class Excitation
    {
        public Terminal Terminal { get; set; }
        public double Value { get; set; }
        public bool Floating { get; set; } = false;
    }
}
