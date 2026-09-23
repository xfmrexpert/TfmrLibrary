using GeometryLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
    public interface ICore
    {
        Transformer? ParentTransformer { get; set; }
        public double WindowWidth_mm { get; set; }
        public double WindowHeight_mm { get; set; }
        GeomLineLoop GenerateGeometry(ref Geometry geometry);
    }
    
    public class CoreFacsimile : ICore
    {
        public Transformer? ParentTransformer { get; set; }

        protected TagManager Tags =>
        ParentTransformer?.TagManager
        ?? throw new InvalidOperationException("TagManager not available (Transformer not set).");


        public double OuterRadius_mm { get; set; }
        public double Thickness_mm { get; set; }
        public double Conductivity { get; set; }
        public bool ClosedLoop { get; set; } = true;
        public double WindowWidth_mm { get; set; }
        public double WindowHeight_mm { get; set; }

        public CoreFacsimile() { }

        public CoreFacsimile(Transformer parentTransformer)
        {
            ParentTransformer = parentTransformer;
        }

        public GeomLineLoop GenerateGeometry(ref Geometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            var window_LL = geometry.AddPoint(OuterRadius_mm / 1000.0, -WindowHeight_mm / 1000.0 / 2.0, 0.01);
            var window_UL = geometry.AddPoint(OuterRadius_mm / 1000.0, WindowHeight_mm / 1000.0 / 2.0, 0.01);
            var window_UR = geometry.AddPoint((OuterRadius_mm + WindowWidth_mm) / 1000.0, WindowHeight_mm / 1000.0 / 2.0, 0.01);
            var window_LR = geometry.AddPoint((OuterRadius_mm + WindowWidth_mm) / 1000.0, -WindowHeight_mm / 1000.0 / 2.0, 0.01);
            var core_LL = geometry.AddPoint((OuterRadius_mm - Thickness_mm) / 1000.0, -WindowHeight_mm / 1000.0 / 2.0, 0.01);
            var core_UL = geometry.AddPoint((OuterRadius_mm - Thickness_mm) / 1000.0, WindowHeight_mm / 1000.0 / 2.0, 0.01);
            var core_UR = window_UL;
            var core_LR = window_LL;
            var axis_lower = geometry.AddPoint(0.0,  -WindowHeight_mm / 1000.0 / 2.0, 0.01);
            var axis_upper = geometry.AddPoint(0.0,  WindowHeight_mm / 1000.0 / 2.0, 0.01);
            var axis = geometry.AddLine(axis_lower, axis_upper);
            Tags.TagEntityByString(axis, "Axis");
            var air_top = geometry.AddLine(axis_upper, core_UL);
            var air_bottom = geometry.AddLine(core_LL, axis_lower);
            var core_outer = geometry.AddLine(core_LR, core_UR);
            Tags.TagEntityByString(core_outer, "CoreLeg_Outer");
            var core_top = geometry.AddLine(core_UL, core_UR);
            Tags.TagEntityByString(core_top, "CoreLeg_Top");
            var core_inner = geometry.AddLine(core_LL, core_UL);
            Tags.TagEntityByString(core_inner, "CoreLeg_Inner");
            var core_bottom = geometry.AddLine(core_LR, core_LL);
            Tags.TagEntityByString(core_top, "CoreLeg_Bottom");
            var top_yoke = geometry.AddLine(window_UL, window_UR);
            Tags.TagEntityByString(top_yoke, "TopYoke");
            var right_edge = geometry.AddLine(window_UR, window_LR);
            Tags.TagEntityByString(right_edge, "RightEdge");
            var bottom_yoke = geometry.AddLine(window_LR, window_LL);
            Tags.TagEntityByString(bottom_yoke, "BottomYoke");
            var window_bdry = geometry.AddLineLoop(new[] { core_outer, top_yoke, right_edge, bottom_yoke });
            var core_bdry = geometry.AddLineLoop(new[] { core_outer, core_bottom, core_inner, core_top });
            var air_bdry = geometry.AddLineLoop(new [] { axis, air_top, core_inner, air_bottom });
            return window_bdry; // TODO: Figure out what to return here!!!
        }
    }

    public class Core : ICore
    {
        public Transformer? ParentTransformer { get; set; }

        public int NumLegs { get; set; }
        public int NumWoundLegs { get; set; }
        public double CoreLegRadius_mm { get; set; }
        public double WindowWidth_mm { get; set; }
        public double WindowHeight_mm { get; set; }

        public Core() { }

        public Core(Transformer parentTransformer)
        {
            ParentTransformer = parentTransformer;
        }

        protected TagManager Tags =>
        ParentTransformer?.TagManager
        ?? throw new InvalidOperationException("TagManager not available (Transformer not set).");

        public GeomLineLoop GenerateGeometry(ref Geometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            var LL = geometry.AddPoint(CoreLegRadius_mm / 1000, -WindowHeight_mm / 1000 / 2, 0.01);
            var UL = geometry.AddPoint(CoreLegRadius_mm / 1000, WindowHeight_mm / 1000 / 2, 0.01);
            var UR = geometry.AddPoint((CoreLegRadius_mm + WindowWidth_mm) / 1000, WindowHeight_mm / 1000 / 2, 0.01);
            var LR = geometry.AddPoint((CoreLegRadius_mm + WindowWidth_mm) / 1000, -WindowHeight_mm / 1000 / 2, 0.01);
            var core_leg = geometry.AddLine(LL, UL);
            Tags.TagEntityByString(core_leg, "CoreLeg");
            var top_yoke = geometry.AddLine(UL, UR);
            Tags.TagEntityByString(top_yoke, "TopYoke");
            var right_edge = geometry.AddLine(UR, LR);
            Tags.TagEntityByString(right_edge, "RightEdge");
            var bottom_yoke = geometry.AddLine(LR, LL);
            Tags.TagEntityByString(bottom_yoke, "BottomYoke");
            var outer_bdry = geometry.AddLineLoop(new[] { core_leg, top_yoke, right_edge, bottom_yoke });
            return outer_bdry;
        }
    }
}
