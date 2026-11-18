using System;
using System.Collections.Generic;
using GeometryLib;
using MathNet.Numerics.LinearAlgebra;

namespace TfmrLib
{
    public class HelicalSegmentGeometry : SegmentGeometry
    {
        public RadialSpacerPattern SpacerPattern { get; set; }

        public virtual int NumMechTurns => NumTurns + 1; // Total mechanical turns in the winding

        public override double SegmentHeight_mm
        {
            get
            {
                if (ParallelOrientation == Orientation.Radial)
                    return NumMechTurns * ConductorType.TotalHeight_mm + SpacerPattern.Height_mm;
                else
                    return NumParallelConductors * NumMechTurns * ConductorType.TotalHeight_mm + SpacerPattern.Height_mm;
            }
        }

        public override double SegmentRadialBuild_mm => ConductorType.TotalWidth_mm * NumParallelConductors;

        public HelicalSegmentGeometry()
        {
            Type = SegmentType.Helical;
            SpacerPattern = new RadialSpacerPattern();
        }

        public HelicalSegmentGeometry(WindingSegment parentSegment) : base(parentSegment)
        {
            Type = SegmentType.Helical;
        }

        protected override void ComputeConductorLocations()
        {
            _conductorLocations = new List<ConductorLocationAxi>();

            int numSpacers = NumMechTurns - 1; // Spacers between each turn

            using var spacerEnum = SpacerPattern.GetEnumerator();
            bool hasNextSpacer = false;

            int wdg_direction;
            double z_mid;
            if (ParentSegment.StartLocation == StartNodeLocation.Top)
            {
                z_mid = DistanceAboveBottomYoke_mm + SegmentHeight_mm - ConductorType.TotalHeight_mm / 2; //Turn 0
                wdg_direction = -1; // Winding segment goes from top to bottom
            }
            else
            {
                z_mid = DistanceAboveBottomYoke_mm + ConductorType.TotalHeight_mm / 2; // Turn 0
                wdg_direction = 1; // Winding segment goes from bottom to top
            }

            for (int turn = 0; turn < NumMechTurns; turn++)
            {
                double gap = 0.0;
                if (turn > 0)
                {
                    hasNextSpacer = spacerEnum.MoveNext();
                    if (!hasNextSpacer)
                        throw new InvalidOperationException($"Spacer pattern ended prematurely at turn {turn}.");
                    gap = spacerEnum.Current.SpacerHeight_mm;
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



        public override Matrix<double> Calc_Rmatrix(double f = 60)
        {
            throw new NotImplementedException();
        }

        public override Vector<double> GetTurnLengths_m()
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
