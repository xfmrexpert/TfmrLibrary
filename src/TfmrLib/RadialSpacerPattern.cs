using System;
using System.Collections.Generic;
using System.Linq;

namespace TfmrLib
{
    public class RadialSpacerPattern
    {
        public double SpacerWidth_mm { get; set; }
        public int NumSpacers_Circumference { get; set; }
        public double AxialCompressionFactor { get; set; } = 0.954;
        public List<SpacerPatternElement> Elements { get; set; } = new();
    }

    public class SpacerPatternElement
    {
        public int Count { get; set; }
        public double SpacerHeight_mm { get; set; }
    }

    public class CompositeSpacerPatternElement : SpacerPatternElement
    {
        public List<SpacerPatternElement> SubElements { get; set; } = new();
    }
}
