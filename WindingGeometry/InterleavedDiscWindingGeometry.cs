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
            if (Interleaving.Count < _numDiscs / 2)
            {
                Interleaving.AddRange(Enumerable.Repeat(InterleavingType.None, _numDiscs / 2 - Interleaving.Count));
            }
            else if (Interleaving.Count > _numDiscs / 2)
            {
                Interleaving.RemoveRange(_numDiscs / 2, Interleaving.Count - _numDiscs / 2);
            }
        }

        protected override void BuildTurnMap()
        {
            int disc_pairs = NumDiscs / 2;
            int physical_turn = -1;
            for (int pair = 0; pair < disc_pairs; pair++)
            {
                int disc1 = pair * 2;
                int disc2 = disc1 + 1;

                InterleavingType interleaving = Interleaving.Count > pair ? Interleaving[pair] : InterleavingType.None;

                for (int lTurn = 0; lTurn < 2*TurnsPerDisc; lTurn++)
                {
                    for (int strand = 1; strand < NumParallelConductors; strand++)
                    {
                        switch (interleaving)
                        {
                            case InterleavingType.None:
                                // No interleaving
                                //PhysToLogicalTurnMap[lTurn] = lTurn;
                                break;
                            case InterleavingType.Partial:
                            case InterleavingType.Full:
                                // Partial or full interleaving
                                if (lTurn < TurnsPerDisc)
                                {
                                    // First half of the turns in the pair
                                    physical_turn++;
                                    //PhysToLogicalTurnMap[physical_turn] = disc1 * TurnsPerDisc + lTurn;
                                }
                                else
                                {
                                    // Second half of the turns in the pair
                                    physical_turn++;
                                    //PhysToLogicalTurnMap[physical_turn] = disc2 * TurnsPerDisc + (lTurn - TurnsPerDisc);
                                }
                                break;
                        }
                    }
                    
                }
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
