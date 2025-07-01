using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
    public class RadialSpacerPattern
    {
        public double SpacerWidth_mm { get; set; } // Width of the spacer in mm
        public int NumSpacers_Circumference { get; set; } // Number of spacers around the circumference
        public double AxialCompressionFactor { get; set; } = 0.954; // Factor to account for axial compression of the spacers
        public List<SpacerPatternElement> Elements { get; set; } = new();
        public string Description => string.Join(" + ", Elements.Select(e => e.ToString()));

        public double Height_mm
        {
            get
            {
                // Calculate the total height of the spacer pattern based on the elements
                return Elements.Sum(e => e.Count * e.SpacerHeight_mm) * AxialCompressionFactor;
            }
        }
    }

    public class SpacerPatternElement
    {
        public int Count { get; set; }              // How many times this height is repeated
        public double SpacerHeight_mm { get; set; }  // Spacer thickness in mm

        public override string ToString() => $"{Count} × {SpacerHeight_mm} mm";
    }

}
