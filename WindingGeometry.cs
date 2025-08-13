using GeometryLib;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

    // Some note on winding conductor indexing.  We have (I think) two general concepts: 1) where is 
    // the conductor in the winding cross-section and 2) where is the conductor within the logical layout of 
    // connected turns within the winding.  We'll use the term "strand" to refer to a single, discrete
    // conductor.  A "turn" is a single complete loop of all parallel strands.  The physical location of a strand
    // is defined by the disc number and the layer number.

    public abstract class WindingGeometry
    {
        // Add these properties
        public WindingSegment ParentSegment { get; set; }
        public Winding ParentWinding => ParentSegment?.ParentWinding;
        public Transformer ParentTransformer => ParentWinding?.ParentTransformer;
        public Core Core => ParentTransformer?.Core;

        public WindingType Type { get; protected set; }
        public int NumTurns { get; set; } // Total number of turns in the winding
        public Conductor ConductorType { get; set; }
        public int NumParallelConductors { get; set; } = 1; // Number of conductors in parallel
        public ConductorTransposition ExternalTransposition { get; set; } = new ConductorTransposition { Type = TranspositionType.None }; // Type of conductor transposition
        public double InnerRadius_mm { get; set; } // Inner radius of the winding in mm

        public double DistanceAboveBottomYoke_mm { get; set; } = 4.0; // Distance from the bottom yoke to the bottom of the winding

        // These are used to tag geometry entities with contextual information so we can identify them later.
        // We need to think about how we want to handle aggregate tags for entire windings and the transformer as a whole.
        // It may also be beneficial to think about doing this more generically.
        public int[] phyTurnsCondBdry;
        public int[] phyTurnsCond;
        public int[] phyTurnsIns;

        public abstract double WindingHeight_mm { get; }

        public abstract double WindingRadialBuild_mm { get; }

        public WindingGeometry()
        {
            // Default constructor
        }

        public WindingGeometry(WindingSegment parentSegment)
        {
            ParentSegment = parentSegment;
        }

        public abstract (double r, double z) GetConductorMidpoint(int turn_idx, int strand_idx);

        public abstract GeomLineLoop[] GenerateGeometry(ref Geometry geometry);

        public LinAlg.Vector<double> Calc_TurnRadii()
        {
            LinAlg.Vector<double> r_c = LinAlg.Vector<double>.Build.Dense(NumTurns * NumParallelConductors);
            for (int turn = 0; turn < NumTurns; turn++)
            {
                for (int strand = 0; strand < NumParallelConductors; strand++)
                {
                    r_c[strand] = GetConductorMidpoint(turn, strand).r;
                }
            }
            return r_c;
        }

        public abstract LinAlg.Matrix<double> Calc_Rmatrix(double f = 60);
        
    }

    public class DiscWindingGeometry : WindingGeometry
    {
        public int NumDiscs { get; set; }
        public int TurnsPerDisc { get; set; }
        public RadialSpacerPattern SpacerPattern { get; set; }

        public DiscWindingGeometry()
        {
            Type = WindingType.Disc;
            SpacerPattern = new RadialSpacerPattern();
        }

        public DiscWindingGeometry(WindingSegment parentSegment) : base(parentSegment)
        {
            Type = WindingType.Disc;
            SpacerPattern = new RadialSpacerPattern();
        }

        public override double WindingHeight_mm
        {
            get
            {
                // The winding height is the total height of the conductors plus any spacers.
                return NumDiscs * ConductorType.TotalHeight_mm + SpacerPattern.Height_mm;
            }
        }

        public override double WindingRadialBuild_mm
        {
            get
            {
                // The radial build is the total width of the conductors in parallel.
                return TurnsPerDisc * NumParallelConductors * ConductorType.TotalWidth_mm;
            }
        }

        public override (double r, double z) GetConductorMidpoint(int turn_idx, int strand_idx)
        {
            // n is the local turn number, starting from the top of the winding
            if (turn_idx < 0 || turn_idx >= NumTurns)
            {
                throw new ArgumentOutOfRangeException(nameof(turn_idx), "Turn number must be within the range of total turns.");
            }

            double r, z;

            int disc = (int)Math.Floor((double)turn_idx / TurnsPerDisc);
            int turn_in_disc = (int)(turn_idx % TurnsPerDisc);
            //Console.WriteLine($"turn: {n} disc: {disc} turn in disc: {turn}");

            // Calculate the radial position (r) of the midpoint of the nth turn
            // We assume discs always start from the outside.  Therefore, even numbered discs
            // are wound out to in and odd numbered discs from in to out.
            if (disc % 2 == 0)
            {
                //out to in
                r = InnerRadius_mm + (TurnsPerDisc - turn_in_disc) * NumParallelConductors * (ConductorType.TotalWidth_mm) - strand_idx * ConductorType.TotalWidth_mm - (ConductorType.TotalWidth_mm/2);
            }
            else
            {
                //in to out
                r = InnerRadius_mm + turn_in_disc * NumParallelConductors * (ConductorType.TotalWidth_mm) + strand_idx * ConductorType.TotalWidth_mm + (ConductorType.TotalWidth_mm / 2);
            }

            // Calculate the z-coordinate based on the disc and turn accounting for spacer pattern
            // Sum the heights of all previous discs and spacers
            z = 0.0;

            int patternElement = 0;
            int discInPattern = 0;
            // Loop through discs up to the disc this conductor is in
            for (int i = 0; i < disc; i++)
            {
                SpacerPatternElement element = SpacerPattern.Elements[patternElement];
                if (discInPattern >= element.Count)
                {
                    patternElement++;
                    discInPattern = 0;
                    if (patternElement >= SpacerPattern.Elements.Count)
                    {
                        throw new InvalidOperationException("Not enough spacer elements defined for the number of discs.");
                    }
                    element = SpacerPattern.Elements[patternElement];
                }
                z += ConductorType.TotalHeight_mm + element.SpacerHeight_mm * SpacerPattern.AxialCompressionFactor;
            }
            z -= (ConductorType.TotalHeight_mm / 2);
            
            //Console.WriteLine($"turn: {n} disc: {disc} turn in disc: {turn} r: {r} z:{z}");
            return (r, z);
        }

        public override GeomLineLoop[] GenerateGeometry(ref Geometry geometry)
        {
            bool include_ins = true;

            // It appears as though I established the z_offset to half of the winding height.  Why?  No idea.
            double z_offset = Core.WindowHeight_mm / 2;

            // Note: The number of total turns in a winding may differ from the number of turns in an axisymmetric section
            // because of partial turns (ie. the disc cross-overs may not be axially aligned). 
            
            // Setup conductor and insulation boundaries
            var conductorins_bdrys = new GeomLineLoop[NumDiscs * TurnsPerDisc * NumParallelConductors];

            phyTurnsCond = new int[NumDiscs * TurnsPerDisc * NumParallelConductors];
            phyTurnsCondBdry = new int[NumDiscs * TurnsPerDisc * NumParallelConductors];
            if (include_ins)
            {
                phyTurnsIns = new int[NumDiscs * TurnsPerDisc * NumParallelConductors];
            }

            int cond_idx = 0;
            int patternElement = 0;
            int discInPattern = 0;
            double z_mid = DistanceAboveBottomYoke_mm + ConductorType.TotalHeight_mm / 2;
            for (int disc = 0; disc < NumDiscs; disc++)
            {
                // Calculate the z-coordinate based on the disc and turn accounting for spacer pattern
                // Sum the heights of all previous discs and spacers
                Console.WriteLine($"Disc {disc} of {NumDiscs}");
                if (disc > 0)
                {
                    SpacerPatternElement element = SpacerPattern.Elements[patternElement];
                    if (discInPattern >= element.Count)
                    {
                        patternElement++;
                        discInPattern = 0;
                        if (patternElement >= SpacerPattern.Elements.Count)
                        {
                            throw new InvalidOperationException("Not enough spacer elements defined for the number of discs.");
                        }
                        element = SpacerPattern.Elements[patternElement];
                    }
                    //Console.WriteLine($"Disc in Pattern {discInPattern} of {element.Count}");
                    z_mid += ConductorType.TotalHeight_mm + element.SpacerHeight_mm * SpacerPattern.AxialCompressionFactor;
                    discInPattern++;
                }
                Console.WriteLine($"z_mid: {z_mid}");
                //if (z_mid > (WindingHeight_mm + DistanceAboveBottomYoke_mm))
                //{
                //    throw new InvalidOperationException("Calculated z_mid exceeds the winding height.");
                //}

                for (int turn_in_disc = 0; turn_in_disc < TurnsPerDisc; turn_in_disc++)
                {
                    for (int strand = 0; strand < NumParallelConductors; strand++)
                    {
                        double r_mid;
                        if (disc % 2 == 0)
                        {
                            // Even disc: winding from outside to inside
                            // turn_in_disc counts down from TurnsPerDisc - 1 to 0
                            // strand counts up from 0 to NumParallelConductors - 1
                            r_mid = InnerRadius_mm + (TurnsPerDisc - turn_in_disc) * NumParallelConductors * ConductorType.TotalWidth_mm - strand * ConductorType.TotalWidth_mm - (ConductorType.TotalWidth_mm / 2);
                        }
                        else
                        {
                            // Odd disc: winding from inside to outside
                            // turn_in_disc counts up from 0 to TurnsPerDisc - 1
                            // strand counts up from 0 to NumParallelConductors - 1
                            r_mid = InnerRadius_mm + turn_in_disc * NumParallelConductors * ConductorType.TotalWidth_mm + strand * ConductorType.TotalWidth_mm + (ConductorType.TotalWidth_mm / 2);
                        }


                        var (conductor_bdry, insulation_bdry) = ConductorType.CreateGeometry(ref geometry, r_mid, z_mid - z_offset);

                        phyTurnsCondBdry[cond_idx] = conductor_bdry.AddTag();
                        //TODO: The above call to ConductorType.CreateGeometry will create both the conductor and the insulation boundaries
                        // We probably don't want this if we aren't modelling the insulaion explicitly
                        if (include_ins)
                        {
                            var insulation_surface = geometry.AddSurface(insulation_bdry, conductor_bdry);
                            phyTurnsIns[cond_idx] = insulation_surface.AddTag();
                            conductorins_bdrys[cond_idx] = insulation_bdry;
                        }
                        else
                        {
                            conductorins_bdrys[cond_idx] = conductor_bdry;
                        }

                        var conductor_surface = geometry.AddSurface(conductor_bdry);
                        phyTurnsCond[cond_idx] = conductor_surface.AddTag();
                        cond_idx++;
                    }
                }
            }

            return conductorins_bdrys;
        }

        public override LinAlg.Matrix<double> Calc_Rmatrix(double f = 60)
        {
            int num_cond = NumParallelConductors * NumTurns;

            var R = LinAlg.Matrix<double>.Build.Dense(num_cond, num_cond);

            for (int t = 0; t < num_cond; t++)
            {
                R[t, t] = ConductorType.DCResistance;
            }

            double sigma_c = 1 / ConductorType.rho_c;
            Complex eta = Complex.Sqrt(2d * Math.PI * f * Constants.mu_0 * sigma_c * Complex.ImaginaryOne) * ConductorType.BareWidth_mm / 2d;
            double R_skin = (1 / (sigma_c * ConductorType.BareHeight_mm * ConductorType.BareWidth_mm) * eta * Complex.Cosh(eta) / Complex.Sinh(eta)).Real;
            //Console.WriteLine($"R_skin: {R_skin}");
            //R_skin = 0;
            var R_f = (R + LinAlg.Matrix<double>.Build.DenseIdentity(num_cond, num_cond) * R_skin);
            return R_f;
        }
    }

    public class LayerWindingGeometry : WindingGeometry
    {
        public int NumberOfLayers { get; set; }
        public double TurnsPerLayer { get; set; }
        public double InterLayerInsulation_mm { get; set; }
        public double RadialBuild_mm { get; set; }

        public override double WindingHeight_mm => TurnsPerLayer * NumParallelConductors * ConductorType.TotalHeight_mm;

        public override double WindingRadialBuild_mm => NumberOfLayers * ConductorType.TotalWidth_mm + (NumberOfLayers - 1) * InterLayerInsulation_mm;

        public LayerWindingGeometry()
        {
            Type = WindingType.Layer;
        }

        public LayerWindingGeometry(WindingSegment parentSegment) : base(parentSegment)
        {
            Type = WindingType.Layer;
        }

        public override GeomLineLoop[] GenerateGeometry(ref Geometry geometry)
        {
            var wdg_rect = geometry.AddRectangle(InnerRadius_mm + WindingRadialBuild_mm /2, WindingHeight_mm / 2 + DistanceAboveBottomYoke_mm - Core.WindowHeight_mm / 2, WindingHeight_mm, WindingRadialBuild_mm);
            var rtn_loops = new GeomLineLoop[1];
            rtn_loops[0] = wdg_rect;
            return rtn_loops;
        }

        public override (double r, double z) GetConductorMidpoint(int turn_idx, int strand_idx)
        {
            throw new NotImplementedException();
        }

        public override Matrix<double> Calc_Rmatrix(double f = 60)
        {
            throw new NotImplementedException();
        }
    }

    public class HelicalWindingGeometry : WindingGeometry
    {
        public double RadialBuild_mm { get; set; }
        public RadialSpacerPattern SpacerPattern { get; set; }

        public override double WindingHeight_mm => (NumTurns + 1) * ConductorType.TotalHeight_mm + SpacerPattern.Height_mm;

        public override double WindingRadialBuild_mm => ConductorType.TotalWidth_mm * NumParallelConductors;

        public HelicalWindingGeometry()
        {
            Type = WindingType.Helical;
            SpacerPattern = new RadialSpacerPattern();
        }

        public HelicalWindingGeometry(WindingSegment parentSegment) : base(parentSegment)
        {
            Type = WindingType.Helical;
        }

        public override GeomLineLoop[] GenerateGeometry(ref Geometry geometry)
        {
            var wdg_rect = geometry.AddRectangle(InnerRadius_mm + WindingRadialBuild_mm / 2, WindingHeight_mm / 2 + DistanceAboveBottomYoke_mm - Core.WindowHeight_mm / 2, WindingHeight_mm, WindingRadialBuild_mm);
            var rtn_loops = new GeomLineLoop[1];
            rtn_loops[0] = wdg_rect;
            return rtn_loops;
        }

        public override (double r, double z) GetConductorMidpoint(int turn_idx, int strand_idx)
        {
            throw new NotImplementedException();
        }

        public override Matrix<double> Calc_Rmatrix(double f = 60)
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

        public MultiStartWindingGeometry(WindingSegment parentSegment) : base(parentSegment)
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

        public InterleavedDiscWindingGeometry(WindingSegment parentSegment) : base(parentSegment)
        {
            Type = WindingType.InterleavedDisc;
        }
    }
}
