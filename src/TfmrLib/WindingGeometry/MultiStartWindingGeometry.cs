namespace TfmrLib
{
    public class MultiStartWindingGeometry : HelicalWindingGeometry
    {
        public int NumberOfStarts { get; set; }

        public int NumberOfTurnsPerStart { get; set; }

        public override int NumTurns => NumberOfStarts * NumberOfTurnsPerStart;

        public override int NumMechTurns => NumberOfStarts * (NumberOfTurnsPerStart + 1); // Total mechanical turns in the winding

        public MultiStartWindingGeometry()
        {
            Type = WindingType.MultiStart;
        }

        public MultiStartWindingGeometry(WindingSegment parentSegment) : base(parentSegment)
        {
            Type = WindingType.MultiStart;
        }
    }

    
}
