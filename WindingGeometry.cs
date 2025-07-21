using GeometryLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinAlg = MathNet.Numerics.LinearAlgebra;

namespace TfmrLib
{
    public enum WindingType
    {
        Disc,
        Layer,
        Helical,
        MultiStart,
        InterleavedDisc
    }

    public abstract class WindingGeometry
    {
        public WindingType Type { get; protected set; }
        public int NumTurns { get; set; } // Total number of turns in the winding
        public Conductor ConductorType { get; set; }
        public int NumParallelConductors { get; set; } = 1; // Number of conductors in parallel
        public ConductorTransposition ExternalTransposition { get; set; } = new ConductorTransposition { Type = TranspositionType.None }; // Type of conductor transposition
        public double InnerRadius_mm { get; set; } // Inner radius of the winding in mm

        public double dist_wdg_tank_bottom { get; set; } = 4.0; // Distance from the winding to the tank bottom in mm

        // These are used to tag geometry entities with contextual information so we can identify them later.
        // We need to think about how we want to handle aggregate tags for entire windings and the transformer as a whole.
        // It may also be beneficial to think about doing this more generically.
        public int[] phyTurnsCondBdry;
        public int[] phyTurnsCond;
        public int[] phyTurnsIns;

        public abstract (double r, double z) GetTurnMidpoint(int n);

        public abstract GeomLineLoop[] GenerateGeometry(ref Geometry geometry);

        public LinAlg.Vector<double> Calc_TurnRadii()
        {
            LinAlg.Vector<double> r_t = LinAlg.Vector<double>.Build.Dense(NumTurns);
            for (int turn = 0; turn < NumTurns; turn++)
            {
                r_t[turn] = GetTurnMidpoint(turn).r;
            }
            return r_t;
        }
    }

    public class DiscWindingGeometry : WindingGeometry
    {
        public int NumDiscs { get; set; }
        public double TurnsPerDisc { get; set; }
        public RadialSpacerPattern SpacerPattern { get; set; }

        public DiscWindingGeometry()
        {
            Type = WindingType.Disc;
        }

        public override (double r, double z) GetTurnMidpoint(int n)
        {
            // n is the local turn number, starting from the top of the winding
            if (n < 0 || n >= NumTurns)
            {
                throw new ArgumentOutOfRangeException(nameof(n), "Turn number must be within the range of total turns.");
            }

            double r, z;

            int disc = (int)Math.Floor((double)n / TurnsPerDisc);
            int turn = (int)(n % TurnsPerDisc);
            //Console.WriteLine($"turn: {n} disc: {disc} turn in disc: {turn}");

            // Calculate the radial position (r) of the midpoint of the nth turn
            if (disc % 2 == 0)
            {
                //out to in
                r = InnerRadius_mm + (TurnsPerDisc - turn) * (ConductorType.TotalConductorWidth_mm) - (ConductorType.TotalConductorWidth_mm/2);
            }
            else
            {
                //in to out
                r = InnerRadius_mm + turn * (ConductorType.TotalConductorWidth_mm) + (ConductorType.TotalConductorWidth_mm / 2);
            }

            // Calculate the z-coordinate based on the disc and turn accounting for spacer pattern
            // Sum the heights of all previous discs and spacers
            z = 0.0;
            for (int i = 0; i < disc; i++)
            {
                SpacerPatternElement element = SpacerPattern.Elements[i];
                z += ConductorType.TotalConductorHeight_mm + element.Count * element.SpacerHeight_mm;
            }
            z -= (ConductorType.TotalConductorHeight_mm / 2);
            
            //Console.WriteLine($"turn: {n} disc: {disc} turn in disc: {turn} r: {r} z:{z}");
            return (r, z);
        }

        public override GeomLineLoop[] GenerateGeometry(ref Geometry geometry)
        {
            bool include_ins = true;

            double z_offset = (num_discs * (h_cond + 2 * t_ins) + (num_discs - 1) * h_spacer + dist_wdg_tank_bottom + dist_wdg_tank_top) / 2;

            // Setup conductor and insulation boundaries
            var conductorins_bdrys = new GeomLineLoop[NumTurns];

            phyTurnsCond = new int[NumTurns];
            phyTurnsCondBdry = new int[NumTurns];
            if (include_ins)
            {
                phyTurnsIns = new int[NumTurns];
            }

            for (int i = 0; i < NumTurns; i++)
            {
                (double r_mid, double z_mid) = GetTurnMidpoint(i);
                z_mid = z_mid - z_offset;
                var (conductor_bdry, insulation_bdry) = ConductorType.CreateGeometry(ref geometry, r_mid, z_mid);
                
                phyTurnsCondBdry[i] = conductor_bdry.AddTag();
                //TODO: The above call to ConductorType.CreateGeometry will create both the conductor and the insulation boundaries
                // We probably don't want this if we aren't modelling the insulaion explicitly
                if (include_ins)
                {
                    var insulation_surface = geometry.AddSurface(insulation_bdry, conductor_bdry);
                    phyTurnsIns[i] = insulation_surface.AddTag();
                    conductorins_bdrys[i] = insulation_bdry;
                }
                else
                {
                    conductorins_bdrys[i] = conductor_bdry;
                }

                var conductor_surface = geometry.AddSurface(conductor_bdry);
                phyTurnsCond[i] = conductor_surface.AddTag();
            }

            return conductorins_bdrys;
        }
    }

    public class LayerWindingGeometry : WindingGeometry
    {
        public int NumberOfLayers { get; set; }
        public double TurnsPerLayer { get; set; }
        public double InterLayerInsulation_mm { get; set; }
        public double RadialBuild_mm { get; set; }

        public LayerWindingGeometry()
        {
            Type = WindingType.Layer;
        }

        public override (double r, double z) GetTurnMidpoint(int n)
        {
            throw new NotImplementedException();
        }

        public override GeomLineLoop[] GenerateGeometry(ref Geometry geometry)
        {
            throw new NotImplementedException();
        }
    }

    public class HelicalWindingGeometry : WindingGeometry
    {
        public double AxialHeight_mm { 
            get { return (NumTurns + 1) * ConductorType.TotalHeight_mm + SpacerPattern.Height_mm; } 
        }
        public double RadialWidth_mm
        {
            get { return ConductorType.TotalWidth_mm * NumParallelConductors; }
        }
        public double RadialBuild_mm { get; set; }
        public RadialSpacerPattern SpacerPattern { get; set; }

        public HelicalWindingGeometry()
        {
            Type = WindingType.Helical;
        }

        public override (double r, double z) GetTurnMidpoint(int n)
        {
            throw new NotImplementedException();
        }

        public override GeomLineLoop[] GenerateGeometry(ref Geometry geometry)
        {
            throw new NotImplementedException();
        }
    }

    public class MultiStartWindingGeometry : HelicalWindingGeometry
    {
        public int NumberOfStarts { get; set; }

        public MultiStartWindingGeometry()
        {
            Type = WindingType.MultiStart;
        }
    }

    public class InterleavedDiscWindingGeometry : DiscWindingGeometry
    {
        public bool InterleaveParallelConductors { get; set; } = false;

        public InterleavedDiscWindingGeometry()
        {
            Type = WindingType.InterleavedDisc;
        }
    }
}
