using GeometryLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
    public class Core
    {
        public int NumLegs { get; set; }
        public int NumWoundLegs { get; set; }
        public double CoreLegRadius_mm { get; set; }
        public double WindowWidth_mm { get; set; }
        public double WindowHeight_mm { get; set; }

        public Transformer ParentTransformer { get; set; }

        public Core(Transformer? parentTransformer = null)
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

            // FIXME: We need to tag the axis and the outer boundary
            var LL = geometry.AddPoint(0, 0);
            var UL = geometry.AddPoint(0, WindowHeight_mm);
            var UR = geometry.AddPoint(CoreLegRadius_mm + WindowWidth_mm / 2, WindowHeight_mm);
            var LR = geometry.AddPoint(CoreLegRadius_mm + WindowWidth_mm / 2, 0);
            var axis = geometry.AddLine(LL, UL);
            Tags.TagEntityByString(axis, "CoreLeg");
            var top_yoke = geometry.AddLine(UL, UR);
            Tags.TagEntityByString(top_yoke, "TopYoke");
            var right_edge = geometry.AddLine(UR, LR);
            Tags.TagEntityByString(right_edge, "RightEdge");
            var bottom_yoke = geometry.AddLine(LR, LL);
            Tags.TagEntityByString(bottom_yoke, "BottomYoke");
            var outer_bdry = geometry.AddLineLoop(new[] { axis, top_yoke, right_edge, bottom_yoke });
            return outer_bdry;
        }
    }
}
