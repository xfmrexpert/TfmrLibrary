using GeometryLib;
using MathNet.Numerics.LinearAlgebra;

namespace TfmrLib
{
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

        protected override void ComputeConductorLocations()
        {
            _conductorLocations = new List<ConductorLocationAxi>();

            for (int layer = 0; layer < NumberOfLayers; layer++)
            {
                double radial_position = InnerRadius_mm + layer * (ConductorType.TotalWidth_mm + InterLayerInsulation_mm) + ConductorType.TotalWidth_mm / 2;
                for (int turn_in_layer = 0; turn_in_layer < TurnsPerLayer; turn_in_layer++)
                {
                    for (int strand = 0; strand < NumParallelConductors; strand++)
                    {
                        double axial_position = DistanceAboveBottomYoke_mm + (turn_in_layer * NumParallelConductors + strand) * ConductorType.TotalHeight_mm + ConductorType.TotalHeight_mm / 2;
                        double turn_length = 2.0 * Math.PI * radial_position;
                        _conductorLocations.Add(new ConductorLocationAxi(radial_position, axial_position, turn_length));
                    }
                }
            }
        }

        public override GeomLineLoop[] GenerateGeometry(ref Geometry geometry)
        {
            var wdg_rect = geometry.AddRectangle(InnerRadius_mm + WindingRadialBuild_mm / 2, WindingHeight_mm / 2 + DistanceAboveBottomYoke_mm - Core.WindowHeight_mm / 2, WindingHeight_mm, WindingRadialBuild_mm);
            var rtn_loops = new GeomLineLoop[1];
            rtn_loops[0] = wdg_rect;
            return rtn_loops;
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
