using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeometryLib;

namespace TfmrLib
{
    public interface ICore
    {
        Transformer? ParentTransformer { get; set; }
        public double WindowWidth_mm { get; set; }
        public double WindowHeight_mm { get; set; }
    }

    public class CoreFacsimile : ICore
    {
        public Transformer? ParentTransformer { get; set; }

        // protected TagManager Tags =>
        // ParentTransformer?.TagManager
        // ?? throw new InvalidOperationException("TagManager not available (Transformer not set).");


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

        // protected TagManager Tags =>
        // ParentTransformer?.TagManager
        // ?? throw new InvalidOperationException("TagManager not available (Transformer not set).");


    }
}
