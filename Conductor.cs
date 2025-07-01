using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
    public abstract class Conductor
    {
        public abstract double ConductorWidth_mm { get; }
        public abstract double ConductorHeight_mm { get; }
    }

    public class CTCConductor : Conductor
    {
        public int NumStrands { get; set; } // Number of strands in the conductor
        public double StrandWidth_mm { get; set; } 
        public double StrandHeight_mm { get; set; }
        public double StrandInsulationThickness_mm { get; set; } // Thickness of insulation between strands
        public double InterleavingPaperThickness_mm { get; set; } // Thickness of interleaving paper between layers
        public double InsulationThickness_mm { get; set; } // Thickness of insulation around the conductor
        public double Resistivity_Ohm_m { get; set; } // Resistivity of the conductor material in Ohm-meters
        public double AxialCompressionFactor { get; set; } = 0.9;

        public override double ConductorWidth_mm
        {
            get
            {
                return (NumStrands + 1) / 2 * (StrandWidth_mm + 2 * StrandInsulationThickness_mm) + 2 * InsulationThickness_mm + 0.1;
            }
        }

        public override double ConductorHeight_mm
        {
            get
            {
                return (2 * (StrandHeight_mm + 2 * StrandInsulationThickness_mm) + InterleavingPaperThickness_mm + 2 * InsulationThickness_mm + 0.2) * AxialCompressionFactor;
            }
        }
    }
}
