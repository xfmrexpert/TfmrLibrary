namespace TfmrLib
{
    public class InterleavedDiscWindingGeometry : DiscWindingGeometry
    {
        public enum InterleavingType
        {
            None, // No interleaving
            Partial,
            Full
        }

        // Maintained at exactly NumDiscs length, auto-filled with None.
        public List<InterleavingType> Interleaving { get; } = new();

        private int _numDiscs;
        public override int NumDiscs
        {
            get => _numDiscs;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                if (_numDiscs == value) return;
                _numDiscs = value;
                EnsureInterleavingLength();
            }
        }

        private void EnsureInterleavingLength()
        {
            if (Interleaving.Count < _numDiscs)
            {
                Interleaving.AddRange(Enumerable.Repeat(InterleavingType.None, _numDiscs - Interleaving.Count));
            }
            else if (Interleaving.Count > _numDiscs)
            {
                Interleaving.RemoveRange(_numDiscs, Interleaving.Count - _numDiscs);
            }
        }

        public InterleavedDiscWindingGeometry()
        {
            Type = WindingType.InterleavedDisc;
        }

        public InterleavedDiscWindingGeometry(WindingSegment parentSegment) : base(parentSegment)
        {
            Type = WindingType.InterleavedDisc;
        }
    }

    
}
