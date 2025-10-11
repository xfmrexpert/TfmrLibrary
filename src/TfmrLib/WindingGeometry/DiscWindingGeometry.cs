using GeometryLib;
using MathNet.Numerics.LinearAlgebra;
using System.Numerics;
using Vector_d = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace TfmrLib
{
    public class DiscWindingGeometry : WindingGeometry
    {
        // Public properties
        public virtual int NumDiscs { get; set; }
        public int TurnsPerDisc { get; set; }
        public RadialSpacerPattern SpacerPattern { get; set; }

        // Constructors
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

        // Computed properties
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

        // Helper methods to compute physical position on-demand
        public (int Disc, int Layer) GetPhysicalPosition(int conductorIndex)
        {
            // Given the winding pattern, compute disc and layer directly
            int turnsPerLayer = TurnsPerDisc * NumParallelConductors;
            int disc = conductorIndex / turnsPerLayer;
            int positionInDisc = conductorIndex % turnsPerLayer;
            
            int layer;
            if (disc % 2 == 0)
            {
                // Even disc: wound outside→inside, reverse the layer order
                layer = turnsPerLayer - 1 - positionInDisc;
            }
            else
            {
                // Odd disc: wound inside→outside
                layer = positionInDisc;
            }
            
            return (disc, layer);
        }

        public LogicalConductorIndex GetLogicalIndex(int conductorIndex)
        {
            if (ConductorIndexToLogical == null)
                BuildConductorMapping();
            
            return ConductorIndexToLogical![conductorIndex];
        }

        protected virtual void BuildConductorMapping()
        {
            int totalConductors = NumDiscs * TurnsPerDisc * NumParallelConductors;
            ConductorIndexToLogical = new Dictionary<int, LogicalConductorIndex>(totalConductors);

            int conductorIndex = 0;
            int logicalTurn = 0;

            for (int disc = 0; disc < NumDiscs; disc++)
            {
                for (int turnInDisc = 0; turnInDisc < TurnsPerDisc; turnInDisc++)
                {
                    for (int strand = 0; strand < NumParallelConductors; strand++)
                    {
                        ConductorIndexToLogical[conductorIndex] = new LogicalConductorIndex(logicalTurn, strand);
                        conductorIndex++;
                    }
                    logicalTurn++;
                }
            }
        }

        protected override void ComputeConductorLocations()
        {
            int totalConductors = NumDiscs * TurnsPerDisc * NumParallelConductors;
            _conductorLocations = new List<ConductorLocationAxi>(totalConductors);
            
            if (ConductorIndexToLogical == null)
                BuildConductorMapping();

            int windingDirection;
            double z_mid;
            if (ParentSegment.StartLocation == StartNodeLocation.Top)
            {
                z_mid = DistanceAboveBottomYoke_mm + WindingHeight_mm - ConductorType.TotalHeight_mm / 2; //Turn 0
                windingDirection = -1; // Winding goes from top to bottom
            }
            else
            {
                z_mid = DistanceAboveBottomYoke_mm + ConductorType.TotalHeight_mm / 2; // Turn 0
                windingDirection = 1; // Winding goes from bottom to top
            }

            // Logical turn is a shit name.  What we mean here is the turn as it appears in the physical winding numbered from
            // out -> in -> out and from bottom -> top.  
            int logicalTurn = 0;

            // We need NumDiscs - 1 inter-disc gaps.
            using var gapEnum = SpacerPattern.GetGapEnumerator(NumDiscs - 1, SpacerPatternExhaustedBehavior.Throw);
            bool hasNextGap = false;

            for (int conductorIndex = 0; conductorIndex < totalConductors; conductorIndex++)
            {
                var (disc, layer) = GetPhysicalPosition(conductorIndex);
                
                // Update z position when moving to new disc
                if (conductorIndex > 0 && disc > GetPhysicalPosition(conductorIndex - 1).Disc)
                {
                    if (gapEnum.MoveNext())
                    {
                        double gap = gapEnum.Current;
                        z_mid += windingDirection * (ConductorType.TotalHeight_mm + gap);
                    }
                }

                // Compute radial position based on layer
                double r_mid = InnerRadius_mm + layer * ConductorType.TotalWidth_mm + ConductorType.TotalWidth_mm / 2;

                // Compute turn length.  This is a bit tricky because the first and last turns in a disc may be partial turns.
                double turn_length;
                if (layer == 0 || layer == TurnsPerDisc * NumParallelConductors - 1)
                {
                    // Start or end turn, need to compute partial turn length
                    // We need to first start with the total number of electrical turns in this winding segment
                    // and then determine how many of those turns are in this disc.
                    int totalElectricalTurns = NumTurns; // This is the total number of electrical turns in the winding segment
                    double turnsInThisDisc = totalElectricalTurns / NumDiscs; // This is the number of electrical turns in this disc
                    double partialTurnFraction = (turnsInThisDisc - Math.Floor(turnsInThisDisc)) / 2; // Fraction of a turn at start or end of disc
                    if (partialTurnFraction == 0) partialTurnFraction = 1.0;
                    turn_length = 2.0 * Math.PI * r_mid * partialTurnFraction;
                }
                else
                {
                    turn_length = 2.0 * Math.PI * r_mid;
                }

                _conductorLocations.Add(new ConductorLocationAxi(r_mid, z_mid, turn_length));
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
            // Loop through conductors in logical order and tag them using the turn map
            int totalConductors = NumDiscs * TurnsPerDisc * NumParallelConductors;

            if (_conductorLocations == null) ComputeConductorLocations();
            if (_conductorLocations.Count != totalConductors)
                throw new InvalidOperationException($"Conductor location count {_conductorLocations.Count} does not match expected number of conductor positions {totalConductors}.");

            // Setup conductor and insulation boundaries
            var conductorins_bdrys = new GeomLineLoop[totalConductors];

            for (int conductorIndex = 0; conductorIndex < totalConductors; conductorIndex++)
            {
                var logical = GetLogicalIndex(conductorIndex);
                var location = _conductorLocations[conductorIndex];
                var locationKey = new LocationKey(ParentWinding.Id, ParentSegment.Id, logical.Turn, logical.Strand);

                var (conductorBoundary, insulationBoundary) = ConductorType.CreateGeometry(ref geometry, location.RadialPosition_mm, location.AxialPosition_mm - z_offset);

                Tags.TagEntityByLocation(conductorBoundary, locationKey, TagType.ConductorBoundary);
                Tags.TagEntityByLocation(insulationBoundary, locationKey, TagType.InsulationBoundary);

                if (include_ins)
                {
                    var insulation_surface = geometry.AddSurface(insulationBoundary, conductorBoundary);
                    Tags.TagEntityByLocation(insulation_surface, locationKey, TagType.InsulationSurface);
                    conductorins_bdrys[conductorIndex] = insulationBoundary;
                }
                else
                {
                    conductorins_bdrys[conductorIndex] = conductorBoundary;
                }

                // 2D region that is conductor interior
                var conductor_surface = geometry.AddSurface(conductorBoundary);
                Tags.TagEntityByLocation(conductor_surface, locationKey, TagType.ConductorSurface);
            }
            
            // Return outer boundary of conductor representations to allow "holes" in the remainder of the geometry
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

        public override Vector_d GetTurnLengths_m()
        {
            int num_cond = NumParallelConductors * NumTurns;
            var L_vector = Vector_d.Build.Dense(num_cond);
            for (int t = 0; t < num_cond; t++)
            {
                L_vector[t] = _conductorLocations[t].TurnLength_mm / 1000.0; // in meters
            }
            return L_vector;
        }
    }

    
}
