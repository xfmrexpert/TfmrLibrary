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
        Radial, // Conductors are oriented radially
        Axial   // Conductors are oriented axially
    }

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
        public ConductorTransposition ExternalTransposition { get; set; } = new ConductorTransposition { Type = TranspositionType.None }; // Type of conductor transposition
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

        public LinAlg.Vector<double> Calc_TurnRadii()
        {
            LinAlg.Vector<double> r_c = LinAlg.Vector<double>.Build.Dense(NumTurns * NumParallelConductors);
            for (int turn = 0; turn < NumTurns; turn++)
            {
                for (int strand = 0; strand < NumParallelConductors; strand++)
                {
                    r_c[strand] = GetConductorMidpoint(turn, strand).r;
                }
            }
            return r_c;
        }

        public abstract Matrix<double> Calc_Rmatrix(double f = 60);
        
    }
    
}
