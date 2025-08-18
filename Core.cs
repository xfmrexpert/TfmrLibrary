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

        public GeomLineLoop GenerateGeometry(ref Geometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            // FIXME: We need to tag the axis and the outer boundary

            return geometry.AddRectangle(CoreLegRadius_mm, 0, WindowHeight_mm, WindowWidth_mm);
        }
    }
}
