using System.Collections.Generic;

namespace CircuitLib
{
    public class Resistor : Branch
    {
        public double Resistance_Ohms { get; set; }

        public Resistor(int id, Node _start, Node _end, double R_Ohms) : base(id, _start, _end)
        {
            Resistance_Ohms = R_Ohms;
        }
    }
}