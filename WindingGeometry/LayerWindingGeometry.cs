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

    
}
