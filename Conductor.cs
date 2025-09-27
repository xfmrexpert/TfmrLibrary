using GeometryLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
    public abstract class Conductor
    {
        public abstract double BareWidth_mm { get; }
        public abstract double BareHeight_mm { get; }
        public abstract double TotalWidth_mm { get; }
        public abstract double TotalHeight_mm { get; }
        public double AxialCompressionFactor { get; set; } = 0.9;
        public abstract double ConductingArea_sqmm { get; }
        public double rho_c { get; set; }

        public abstract (GeomLineLoop, GeomLineLoop) CreateGeometry(ref Geometry geo, double r_mid_mm, double z_mid_mm);

        public double DCResistance { get => ConductingArea_sqmm * rho_c; }
    }

    public class RectConductor : Conductor
    {
        public double StrandWidth_mm { get; set; }
        public double StrandHeight_mm { get; set; }
        public double CornerRadius_mm { get; set; }
        public double InsulationThickness_mm { get; set; }

        public override double BareWidth_mm => StrandWidth_mm;

        public override double BareHeight_mm => StrandHeight_mm;

        public override double TotalWidth_mm => StrandWidth_mm + 2 * InsulationThickness_mm;

        public override double TotalHeight_mm => StrandHeight_mm + 2 * InsulationThickness_mm * AxialCompressionFactor;

        public override double ConductingArea_sqmm => StrandWidth_mm * StrandHeight_mm - ((4 - Math.PI) * CornerRadius_mm * CornerRadius_mm);

        public override (GeomLineLoop, GeomLineLoop) CreateGeometry(ref Geometry geo, double r_mid, double z_mid)
        {
            var conductor_bdry = geo.AddRoundedRectangle(r_mid / 1000, z_mid / 1000, BareHeight_mm / 1000, BareWidth_mm / 1000, CornerRadius_mm / 1000, 0.0004);
            var insulation_bdry = geo.AddRoundedRectangle(r_mid / 1000, z_mid / 1000, (BareHeight_mm + 2 * InsulationThickness_mm) / 1000, (BareWidth_mm + 2 * InsulationThickness_mm) / 1000, (CornerRadius_mm + InsulationThickness_mm) / 1000, 0.0004);
            return (conductor_bdry, insulation_bdry);
        }
    }

    public class CTCConductor : Conductor
    {
        public int NumStrands { get; set; } // Number of strands in the conductor
        public double StrandWidth_mm { get; set; } 
        public double StrandHeight_mm { get; set; }
        public double StrandCornerRadius_mm { get; set; }
        public double StrandInsulationThickness_mm { get; set; } // Thickness of insulation between strands
        public double InterleavingPaperThickness_mm { get; set; } // Thickness of interleaving paper between layers
        public double InsulationThickness_mm { get; set; } // Thickness of insulation around the conductor
        public double Resistivity_Ohm_m { get; set; } // Resistivity of the conductor material in Ohm-meters

        public override double BareWidth_mm
        {
            get
            {
                return (NumStrands + 1) / 2 * (StrandWidth_mm + 2 * StrandInsulationThickness_mm) + 0.1;
            }
        }

        public override double BareHeight_mm
        {
            get
            {
                return (2 * (StrandHeight_mm + 2 * StrandInsulationThickness_mm) + InterleavingPaperThickness_mm + 0.2) * AxialCompressionFactor;
            }
        }

        public override double TotalWidth_mm
        {
            get
            {
                return (NumStrands + 1) / 2 * (StrandWidth_mm + 2 * StrandInsulationThickness_mm) + 2 * InsulationThickness_mm + 0.2;
            }
        }

        public override double TotalHeight_mm
        {
            get
            {
                return 2 * (StrandHeight_mm + 2 * StrandInsulationThickness_mm) + (InterleavingPaperThickness_mm + 2 * InsulationThickness_mm) * AxialCompressionFactor + 0.1;
            }
        }

        public override double ConductingArea_sqmm
        {
            get
            {
                return NumStrands * (StrandWidth_mm * StrandHeight_mm - ((4 - Math.PI) * StrandCornerRadius_mm * StrandCornerRadius_mm));
            }
        }

        public override (GeomLineLoop, GeomLineLoop) CreateGeometry(ref Geometry geo, double r_mid, double z_mid)
        {
            var conductor_bdry = geo.AddRoundedRectangle(r_mid / 1000, z_mid / 1000, BareHeight_mm / 1000, BareWidth_mm / 1000, StrandCornerRadius_mm / 1000, 0.0004);
            var insulation_bdry = geo.AddRoundedRectangle(r_mid / 1000, z_mid / 1000, (BareHeight_mm + 2 * InsulationThickness_mm) / 1000, (BareWidth_mm + 2 * InsulationThickness_mm) / 1000, (StrandCornerRadius_mm + InsulationThickness_mm) / 1000, 0.0004);
            return (conductor_bdry, insulation_bdry);
        }
    }
}
