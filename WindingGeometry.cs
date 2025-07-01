using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public abstract class WindingGeometry
    {
        public WindingType Type { get; protected set; }
        public int NumTurns { get; set; } // Total number of turns in the winding
        public Conductor ConductorType { get; set; }
        public int NumConductorsInParallel { get; set; } = 1; // Number of conductors in parallel
        public double InnerRadius_mm { get; set; } // Inner radius of the winding in mm
    }

    public class DiscWindingGeometry : WindingGeometry
    {
        public int NumberOfDiscs { get; set; }
        public double TurnsPerDisc { get; set; }
        public RadialSpacerPattern SpacerPattern { get; set; }

        public DiscWindingGeometry()
        {
            Type = WindingType.Disc;
        }
    }

    public class LayerWindingGeometry : WindingGeometry
    {
        public int NumberOfLayers { get; set; }
        public double TurnsPerLayer { get; set; }
        public double InterLayerInsulation_mm { get; set; }
        public double RadialBuild_mm { get; set; }

        public LayerWindingGeometry()
        {
            Type = WindingType.Layer;
        }
    }

    public class HelicalWindingGeometry : WindingGeometry
    {
        public double AxialHeight_mm { 
            get { return (NumTurns + 1) * ConductorType.ConductorHeight_mm + SpacerPattern.Height_mm; } 
        }
        public double RadialWidth_mm
        {
            get { return ConductorType.ConductorWidth_mm * NumConductorsInParallel; }
        }
        public double RadialBuild_mm { get; set; }
        public RadialSpacerPattern SpacerPattern { get; set; }

        public HelicalWindingGeometry()
        {
            Type = WindingType.Helical;
        }
    }

    public class MultiStartWindingGeometry : WindingGeometry
    {
        public int NumberOfStarts { get; set; }
        public double Pitch_mm { get; set; }
        public double AxialHeight_mm { get; set; }
        public double RadialBuild_mm { get; set; }

        public MultiStartWindingGeometry()
        {
            Type = WindingType.MultiStart;
        }
    }

    public class InterleavedDiscWindingGeometry : DiscWindingGeometry
    {
        public int NumberOfInterleavesPerDisc { get; set; }

        public InterleavedDiscWindingGeometry()
        {
            throw new NotImplementedException("Interleaved disc windings are not yet implemented.");
            Type = WindingType.InterleavedDisc;
        }
    }
}
