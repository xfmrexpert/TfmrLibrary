using GeometryLib;
using MathNet.Numerics.LinearAlgebra;
using System.Numerics;

namespace TfmrLib
{
    public class DiscWindingGeometry : WindingGeometry
    {
        public virtual int NumDiscs { get; set; }
        public int TurnsPerDisc { get; set; }
        public RadialSpacerPattern SpacerPattern { get; set; }

        protected int[] PositionTurnMap;
        protected int[] PositionStrandMap;

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

        // TODO: THIS IS WRONG
        public override (double r, double z) GetConductorMidpoint(int turn_idx, int strand_idx)
        {
            // n is the local turn number, starting from the top of the winding
            if (turn_idx < 0 || turn_idx >= NumTurns)
            {
                throw new ArgumentOutOfRangeException(nameof(turn_idx), "Turn number must be within the range of total turns.");
            }

            double r, z;

            int disc = (int)Math.Floor((double)turn_idx / TurnsPerDisc);
            int turn_in_disc = turn_idx % TurnsPerDisc;
            //Console.WriteLine($"turn: {n} disc: {disc} turn in disc: {turn}");

            // Calculate the radial position (r) of the midpoint of the nth turn
            // We assume discs always start from the outside.  Therefore, even numbered discs
            // are wound out to in and odd numbered discs from in to out.
            if (disc % 2 == 0)
            {
                //out to in
                r = InnerRadius_mm + (TurnsPerDisc - turn_in_disc) * NumParallelConductors * ConductorType.TotalWidth_mm - strand_idx * ConductorType.TotalWidth_mm - ConductorType.TotalWidth_mm/2;
            }
            else
            {
                //in to out
                r = InnerRadius_mm + turn_in_disc * NumParallelConductors * ConductorType.TotalWidth_mm + strand_idx * ConductorType.TotalWidth_mm + ConductorType.TotalWidth_mm / 2;
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
            z -= ConductorType.TotalHeight_mm / 2;
            
            //Console.WriteLine($"turn: {n} disc: {disc} turn in disc: {turn} r: {r} z:{z}");
            return (r, z);
        }

        protected virtual void BuildTurnMap()
        {
            PositionTurnMap = new int[NumTurns * NumParallelConductors];
            PositionStrandMap = new int[NumTurns * NumParallelConductors];
            int logicalTurn = 0;
            for (int disc = 0; disc < NumDiscs; disc++)
            {
                for (int turn_in_disc = 0; turn_in_disc < TurnsPerDisc; turn_in_disc++)
                {
                    PositionStrandMap[logicalTurn] = 0;
                    for (int strand = 0; strand < NumParallelConductors; strand++)
                    {
                        logicalTurn += 1;
                        //PhysToLogicalTurnMap[logicalTurn] = logicalTurn;
                    }
                }
            }
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
            phyTurnsCond = new int[conductorins_bdrys.Length];
            phyTurnsCondBdry = new int[conductorins_bdrys.Length];
            if (include_ins) phyTurnsIns = new int[conductorins_bdrys.Length];

            int cond_idx = 0;
            double z_mid = DistanceAboveBottomYoke_mm + WindingHeight_mm - ConductorType.TotalHeight_mm / 2;

            // Logical turn is a shit name.  What we mean here is the turn as it appears in the physical winding numbered from
            // out -> in -> out and from bottom -> top.  
            int logicalTurn = 0;

            // We need NumDiscs - 1 inter-disc gaps.
            using var gapEnum = SpacerPattern.GetGapEnumerator(NumDiscs - 1, SpacerPatternExhaustedBehavior.Throw);
            bool hasNextGap = false;

            for (int disc = 0; disc < NumDiscs; disc++)
            {
                if (disc > 0)
                {
                    hasNextGap = gapEnum.MoveNext();
                    if (!hasNextGap)
                        throw new InvalidOperationException($"Spacer pattern ended prematurely at disc {disc}.");
                    double gap = gapEnum.Current;
                    z_mid -= ConductorType.TotalHeight_mm + gap;
                }

                // Maybe we dispense with turn/stand here and go to a physical disc/layer where layer is each iteration
                // of adjacent turns/strands. We then have a physical->logical mapping that takes disc # and layer # and 
                // spits out the logical turn and strand #s. In effect, this may be no different than physical turn and strand #s
                // to logical turn and strand #s. Layer # would be turn_in_disc * NumParallelConductors + strand.
                for (int turn_in_disc = 0; turn_in_disc < TurnsPerDisc; turn_in_disc++)
                {
                    logicalTurn += 1;
                    for (int strand = 0; strand < NumParallelConductors; strand++)
                    {
                        double r_mid;
                        if (disc % 2 == 0)
                        {
                            // Even disc: winding from outside to inside
                            // turn_in_disc counts down from TurnsPerDisc - 1 to 0
                            // strand counts up from 0 to NumParallelConductors - 1
                            r_mid = InnerRadius_mm + (TurnsPerDisc - turn_in_disc) * NumParallelConductors * ConductorType.TotalWidth_mm - strand * ConductorType.TotalWidth_mm - ConductorType.TotalWidth_mm / 2;
                        }
                        else
                        {
                            // Odd disc: winding from inside to outside
                            // turn_in_disc counts up from 0 to TurnsPerDisc - 1
                            // strand counts up from 0 to NumParallelConductors - 1
                            r_mid = InnerRadius_mm + turn_in_disc * NumParallelConductors * ConductorType.TotalWidth_mm + strand * ConductorType.TotalWidth_mm + ConductorType.TotalWidth_mm / 2;
                        }

                        var (conductor_bdry, insulation_bdry) = ConductorType.CreateGeometry(ref geometry, r_mid, z_mid - z_offset);

                        var loc = new LocationKey(ParentWinding.Id, ParentSegment.Id, logicalTurn, strand);

                        phyTurnsCondBdry[cond_idx] = Tags.TagEntityByLocation(conductor_bdry, loc, TagType.ConductorBoundary);
                        int insTag = Tags.TagEntityByLocation(insulation_bdry, loc, TagType.InsulationBoundary);

                        //TODO: The above call to ConductorType.CreateGeometry will create both the conductor and the insulation boundaries
                        // We probably don't want this if we aren't modelling the insulaion explicitly
                        if (include_ins)
                        {
                            var insulation_surface = geometry.AddSurface(insulation_bdry, conductor_bdry);
                            phyTurnsIns[cond_idx] = Tags.TagEntityByLocation(insulation_surface, loc, TagType.InsulationSurface);
                            conductorins_bdrys[cond_idx] = insulation_bdry;
                        }
                        else
                        {
                            conductorins_bdrys[cond_idx] = conductor_bdry;
                        }

                        var conductor_surface = geometry.AddSurface(conductor_bdry);
                        phyTurnsCond[cond_idx] = Tags.TagEntityByLocation(conductor_surface, loc, TagType.ConductorSurface);
                        cond_idx++;
                    }
                }
            }

            return conductorins_bdrys;
        }

        public override Matrix<double> Calc_Rmatrix(double f = 60)
        {
            int num_cond = NumParallelConductors * NumTurns;

            var R = Matrix<double>.Build.Dense(num_cond, num_cond);

            for (int t = 0; t < num_cond; t++)
            {
                R[t, t] = ConductorType.DCResistance;
            }

            double sigma_c = 1 / ConductorType.rho_c;
            Complex eta = Complex.Sqrt(2d * Math.PI * f * Constants.mu_0 * sigma_c * Complex.ImaginaryOne) * ConductorType.BareWidth_mm / 2d;
            double R_skin = (1 / (sigma_c * ConductorType.BareHeight_mm * ConductorType.BareWidth_mm) * eta * Complex.Cosh(eta) / Complex.Sinh(eta)).Real;
            //Console.WriteLine($"R_skin: {R_skin}");
            //R_skin = 0;
            var R_f = R + Matrix<double>.Build.DenseIdentity(num_cond, num_cond) * R_skin;
            return R_f;
        }
    }

    
}
