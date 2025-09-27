using GeometryLib;
using MathNet.Numerics.LinearAlgebra;

namespace TfmrLib
{
    public class HelicalWindingGeometry : WindingGeometry
    {
        public RadialSpacerPattern SpacerPattern { get; set; }

        public virtual int NumMechTurns => NumTurns + 1; // Total mechanical turns in the winding

        public override double WindingHeight_mm
        {
            get
            {
                if (ParallelOrientation == Orientation.Radial)
                    return NumMechTurns * ConductorType.TotalHeight_mm + SpacerPattern.Height_mm;
                else
                    return NumParallelConductors * NumMechTurns * ConductorType.TotalHeight_mm + SpacerPattern.Height_mm;
            }
        }

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

        protected override void ComputeConductorLocations()
        {
            _conductorLocations = new List<ConductorLocationAxi>();

            int numSpacers = NumMechTurns - 1; // Spacers between each turn

            using var gapEnum = SpacerPattern.GetGapEnumerator(numSpacers, SpacerPatternExhaustedBehavior.Throw);
            bool hasNextGap = false;

            int wdg_direction;
            double z_mid;
            if (ParentSegment.StartLocation == StartNodeLocation.Top)
            {
                z_mid = DistanceAboveBottomYoke_mm + WindingHeight_mm - ConductorType.TotalHeight_mm / 2; //Turn 0
                wdg_direction = -1; // Winding goes from top to bottom
            }
            else
            {
                z_mid = DistanceAboveBottomYoke_mm + ConductorType.TotalHeight_mm / 2; // Turn 0
                wdg_direction = 1; // Winding goes from bottom to top
            }

            for (int turn = 0; turn < NumMechTurns; turn++)
            {
                double gap = 0.0;
                if (turn > 0)
                {
                    hasNextGap = gapEnum.MoveNext();
                    if (!hasNextGap)
                        throw new InvalidOperationException($"Spacer pattern ended prematurely at turn {turn}.");
                    gap = gapEnum.Current;
                }

                if (ParallelOrientation == Orientation.Radial)
                {
                    // Calculate the z-coordinate based on the turn accounting for spacer pattern
                    // Sum the heights of all previous turns and spacers
                    if (turn > 0)
                    {
                        z_mid += wdg_direction * (ConductorType.TotalHeight_mm + gap);
                    }

                    for (int strand = 0; strand < NumParallelConductors; strand++)
                    {
                        double r_mid = InnerRadius_mm + strand * ConductorType.TotalWidth_mm + ConductorType.TotalWidth_mm / 2;
                        double turn_length = 2.0 * Math.PI * r_mid;
                        _conductorLocations.Add(new ConductorLocationAxi(r_mid, z_mid, turn_length));
                    }
                }
                else if (ParallelOrientation == Orientation.Axial)
                {
                    for (int strand = 0; strand < NumParallelConductors; strand++)
                    {
                        // For axial orientation, we need to calculate the z-coordinate based on the turn number
                        // and the number of parallel conductors.
                        if (turn > 0 || strand > 0)
                        {
                            z_mid += wdg_direction * (ConductorType.TotalHeight_mm + gap);
                        }
                        // For axial orientation, we need to calculate the radial position based on the turn number
                        // and the number of parallel conductors.
                        double r_mid = InnerRadius_mm + ConductorType.TotalWidth_mm / 2;
                        _conductorLocations.Add(new ConductorLocationAxi(r_mid, z_mid, 2.0 * Math.PI * r_mid));
                    }
                }
            }
        }

        public override GeomLineLoop[] GenerateGeometry(ref Geometry geometry)
        {
            bool include_ins = true;

            // Setup conductor and insulation boundaries
            var conductorins_bdrys = new GeomLineLoop[NumMechTurns * NumParallelConductors];

            GeomLineLoop? conductor_bdry;
            GeomLineLoop? insulation_bdry;

            int cond_idx = 0;

            int wdg_direction;
            double z_mid;
            if (ParentSegment.StartLocation == StartNodeLocation.Top)
            {
                z_mid = DistanceAboveBottomYoke_mm + WindingHeight_mm - ConductorType.TotalHeight_mm / 2; //Turn 0
                wdg_direction = -1; // Winding goes from top to bottom
            }
            else
            {
                z_mid = DistanceAboveBottomYoke_mm + ConductorType.TotalHeight_mm / 2; // Turn 0
                wdg_direction = 1; // Winding goes from bottom to top
            }

            int numSpacers = NumMechTurns - 1; // Spacers between each turn

            using var gapEnum = SpacerPattern.GetGapEnumerator(numSpacers, SpacerPatternExhaustedBehavior.Throw);
            bool hasNextGap = false;

            for (int turn = 0; turn < NumMechTurns; turn++)
            {
                double gap = 0.0;
                if (turn > 0)
                {
                    hasNextGap = gapEnum.MoveNext();
                    if (!hasNextGap)
                        throw new InvalidOperationException($"Spacer pattern ended prematurely at turn {turn}.");
                    gap = gapEnum.Current;
                }

                if (ParallelOrientation == Orientation.Radial)
                {
                    // Calculate the z-coordinate based on the turn accounting for spacer pattern
                    // Sum the heights of all previous turns and spacers
                    if (turn > 0)
                    {
                        z_mid += wdg_direction * (ConductorType.TotalHeight_mm + gap);
                    }

                    for (int strand = 0; strand < NumParallelConductors; strand++)
                    {
                        var loc = new LocationKey(ParentWinding.Id, ParentSegment.Id, turn, strand);
                        double r_mid = InnerRadius_mm + strand * ConductorType.TotalWidth_mm + ConductorType.TotalWidth_mm / 2;
                        (conductor_bdry, insulation_bdry) = ConductorType.CreateGeometry(ref geometry, r_mid, z_mid - Core.WindowHeight_mm / 2);
                        Tags.TagEntityByLocation(conductor_bdry, loc, TagType.ConductorBoundary);
                        if (insulation_bdry != null)
                        {
                            var insulation_surface = geometry.AddSurface(insulation_bdry, conductor_bdry);
                        }

                        //TODO: The above call to ConductorType.CreateGeometry will create both the conductor and the insulation boundaries
                        // We probably don't want this if we aren't modelling the insulaion explicitly
                        if (include_ins)
                        {
                            var insulation_surface = geometry.AddSurface(insulation_bdry, conductor_bdry);
                            Tags.TagEntityByLocation(insulation_surface, loc, TagType.InsulationSurface);
                            conductorins_bdrys[cond_idx] = insulation_bdry;
                        }
                        else
                        {
                            conductorins_bdrys[cond_idx] = conductor_bdry;
                        }

                        var conductor_surface = geometry.AddSurface(conductor_bdry);
                        Tags.TagEntityByLocation(conductor_surface, loc, TagType.ConductorSurface);
                        cond_idx++;
                    }
                }
                else if (ParallelOrientation == Orientation.Axial)
                {
                    for (int strand = 0; strand < NumParallelConductors; strand++)
                    {
                        var loc = new LocationKey(ParentWinding.Id, ParentSegment.Id, turn, strand);

                        // For axial orientation, we need to calculate the z-coordinate based on the turn number
                        // and the number of parallel conductors.
                        if (cond_idx > 0)
                        {
                            z_mid += wdg_direction * (ConductorType.TotalHeight_mm + gap);
                        }

                        // For axial orientation, we need to calculate the radial position based on the turn number
                        // and the number of parallel conductors.
                        double r_mid = InnerRadius_mm + ConductorType.TotalWidth_mm / 2;
                        (conductor_bdry, insulation_bdry) = ConductorType.CreateGeometry(ref geometry, r_mid, z_mid - Core.WindowHeight_mm / 2);
                        Tags.TagEntityByLocation(conductor_bdry, loc, TagType.ConductorSurface);
                        if (insulation_bdry != null)
                        {
                            var insulation_surface = geometry.AddSurface(insulation_bdry, conductor_bdry);
                            Tags.TagEntityByLocation(insulation_surface, loc, TagType.InsulationSurface);
                            conductorins_bdrys[cond_idx] = insulation_bdry;
                        }
                        else
                        {
                            conductorins_bdrys[cond_idx] = insulation_bdry;
                        }

                        var conductor_surface = geometry.AddSurface(conductor_bdry);
                        Tags.TagEntityByLocation(conductor_surface, loc, TagType.ConductorSurface);
                        cond_idx++;

                    }
                }
            }

            return conductorins_bdrys;
        }

        public override Matrix<double> Calc_Rmatrix(double f = 60)
        {
            throw new NotImplementedException();
        }

        public override Vector<double> GetTurnLengths()
        {
            int num_cond = NumParallelConductors * NumTurns;
            var L_vector = Vector<double>.Build.Dense(num_cond);
            for (int t = 0; t < num_cond; t++)
            {
                L_vector[t] = 2.0 * Math.PI * InnerRadius_mm / 1000.0; // in meters
            }
            return L_vector;
        }
    }

    
}
