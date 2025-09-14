using GeometryLib;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinAlg = MathNet.Numerics.LinearAlgebra;

namespace TfmrLib
{
    public enum WindingType
    {
        Disc,
        Layer,
        Helical,
        MultiStart,
        InterleavedDisc
    }

    public enum Orientation
    {
        Radial, // Parallel conductors are oriented radially
        Axial   // Parallel conductors are oriented axially
    }

    public record PhysicalConductorIndex(int Disc, int Layer); // Disc is vertical position (0 is top), Layer is radial position (0 is innermost)
    public record LogicalConductorIndex(int Turn, int Strand); // Turn is the turn number (0 is first (top) turn), Strand is the parallel conductor number (0 is first strand)

    // Some note on winding conductor indexing.  We have (I think) two general concepts: 1) where is 
    // the conductor in the winding cross-section and 2) where is the conductor within the logical layout of 
    // connected turns within the winding.  We'll use the term "strand" to refer to a single, discrete
    // conductor.  A "turn" is a single complete loop of all parallel strands.  The physical location of a strand
    // is defined by the disc number and the layer number.

    public abstract class WindingGeometry
    {
        // Add these properties
        public WindingSegment ParentSegment { get; set; }
        public Winding ParentWinding => ParentSegment?.ParentWinding;
        public Transformer ParentTransformer => ParentWinding?.ParentTransformer;
        public Core Core => ParentTransformer?.Core;

        protected TagManager Tags =>
        ParentTransformer?.TagManager
        ?? throw new InvalidOperationException("TagManager not available (Transformer not set).");

        public WindingType Type { get; protected set; }
        public virtual int NumTurns { get; set; } // Total number of turns in the winding
        public Conductor ConductorType { get; set; }
        public int NumParallelConductors { get; set; } = 1; // Number of conductors in parallel
        public Orientation ParallelOrientation { get; set; } = Orientation.Radial; // Orientation of parallel conductors
        public List<ConductorTransposition> ExternalTranspositions { get; set; } = []; // Type of conductor transposition
        public double InnerRadius_mm { get; set; } // Inner radius of the winding in mm

        public double DistanceAboveBottomYoke_mm { get; set; } = 4.0; // Distance from the bottom yoke to the bottom of the winding

        // These are used to tag geometry entities with contextual information so we can identify them later.
        // We need to think about how we want to handle aggregate tags for entire windings and the transformer as a whole.
        // It may also be beneficial to think about doing this more generically.
        public int[] phyTurnsCondBdry;
        public int[] phyTurnsCond;
        public int[] phyTurnsIns;

        public abstract double WindingHeight_mm { get; }

        public abstract double WindingRadialBuild_mm { get; }

        public WindingGeometry()
        {
            // Default constructor
        }

        public WindingGeometry(WindingSegment parentSegment)
        {
            ParentSegment = parentSegment;
        }

        public abstract (double r, double z) GetConductorMidpoint(int turn_idx, int strand_idx);

        public abstract GeomLineLoop[] GenerateGeometry(ref Geometry geometry);

        public abstract Matrix<double> Calc_Rmatrix(double f = 60);

    }
    
}
