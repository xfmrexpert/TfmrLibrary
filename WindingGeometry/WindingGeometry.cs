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

    public record ConductorLocationAxi(double RadialPosition_mm, double AxialPosition_mm, double TurnLength_mm);

    // Some note on winding conductor indexing.  We have (I think) two general concepts: 1) where is 
    // the conductor in the winding cross-section and 2) where is the conductor within the logical layout of 
    // connected turns within the winding.  We'll use the term "strand" to refer to a single, discrete
    // conductor.  A "turn" is a single complete loop of all parallel strands.  The physical location of a strand
    // is defined by the disc number and the layer number.

    public abstract class WindingGeometry
    {
        // Entity relationships
        public WindingSegment ParentSegment { get; set; }
        public Winding ParentWinding => ParentSegment?.ParentWinding;
        public Transformer ParentTransformer => ParentWinding?.ParentTransformer;
        public Core Core => ParentTransformer?.Core;

        protected TagManager Tags =>
        ParentTransformer?.TagManager
        ?? throw new InvalidOperationException("TagManager not available (Transformer not set).");

        // Internal turn mapping structures
        protected Dictionary<int, LogicalConductorIndex> ConductorIndexToLogical;
        protected Dictionary<LogicalConductorIndex, int> LogicalToConductorIndex => ConductorIndexToLogical.ToDictionary(kv => kv.Value, kv => kv.Key);

        protected List<ConductorLocationAxi> _conductorLocations = null;
        protected List<ConductorLocationAxi> ConductorLocations
        {
            get
            {
                if (_conductorLocations == null) ComputeConductorLocations();
                return _conductorLocations;
            }
        }

        protected void InvalidateConductorLocations()
        {
            _conductorLocations = null;
        }

        // Public properties
        public WindingType Type { get; protected set; }
        public virtual int NumTurns { get; set; } // Total number of complete turns in the winding (overriden in multistart)
        public Conductor ConductorType { get; set; }
        public int NumParallelConductors { get; set; } = 1; // Number of conductors in parallel
        public Orientation ParallelOrientation { get; set; } = Orientation.Radial; // Orientation of parallel conductors
        public List<ConductorTransposition> ExternalTranspositions { get; set; } = []; // Type of conductor transposition
        public double InnerRadius_mm { get; set; } // Inner radius of the winding in mm
        public double DistanceAboveBottomYoke_mm { get; set; } // Distance from the bottom yoke to the bottom of the winding

        // Computed properties
        public abstract double WindingHeight_mm { get; }
        public abstract double WindingRadialBuild_mm { get; }

        // Constructors
        public WindingGeometry()
        {
            // Default constructor
        }

        public WindingGeometry(WindingSegment parentSegment)
        {
            ParentSegment = parentSegment;
        }

        protected abstract void ComputeConductorLocations();

        public abstract GeomLineLoop[] GenerateGeometry(ref Geometry geometry);

        public abstract Matrix<double> Calc_Rmatrix(double f = 60);

        public abstract Vector<double> GetTurnLengths();

    }
    
}
