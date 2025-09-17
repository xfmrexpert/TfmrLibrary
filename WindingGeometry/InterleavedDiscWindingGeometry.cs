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
            LogicalToPhysicalTurnMap = new Dictionary<LogicalConductorIndex, PhysicalConductorIndex>();
            int num_disc_pairs = NumDiscs / 2;
            int pair_start_turn = 0;
            int disc = -1;
            for (int pair = 0; pair < num_disc_pairs; pair++)
            {
                System.Diagnostics.Debug.WriteLine($"Building turn map for disc pair {pair}");
                var interleaving = Interleaving.Count > pair ? Interleaving[pair] : InterleavingType.None;
                for (int disc_in_pair = 0; disc_in_pair < 2; disc_in_pair++)
                {
                    disc++;
                    System.Diagnostics.Debug.WriteLine($"  Disc {disc} (disc_in_pair={disc_in_pair}), interleaving={interleaving}");
                    bool isInterleavedTurn = false;
                    int turn_in_disc = 0;
                    int strand = 0;
                    for (int rad_pos = 0; rad_pos < TurnsPerDisc * NumParallelConductors; rad_pos++)
                    {
                        // This may confuse things a bit, but we're going to count the rad_pos here in the direction
                        // the disc is wound.  So for the first disc in the pair, it's wound from outside to inside, and the second disc
                        // is wound from inside to outside. When we assign the physical index, we'll account for this.
                        int layer;
                        if (disc_in_pair == 0) // First disc in pair, by convention wound from outside to inside
                            layer = TurnsPerDisc * NumParallelConductors - rad_pos - 1;
                        else // Second disc in pair, by convention wound from inside to outside
                            layer = rad_pos;
                        var physIndex = new PhysicalConductorIndex(disc, layer);

                        if (interleaving == InterleavingType.None)
                        {
                            System.Diagnostics.Debug.WriteLine($"    turn={pair_start_turn + turn_in_disc}, strand={strand}, rad_pos={rad_pos}, turn_in_disc={turn_in_disc}, strand={strand}, physIndex=({physIndex.Disc},{physIndex.Layer})");
                            LogicalToPhysicalTurnMap[new LogicalConductorIndex(pair_start_turn + turn_in_disc, strand)] = physIndex;
                            // Increment turn and strand appropriately
                            if (strand == NumParallelConductors - 1)
                            {
                                strand = 0;
                                turn_in_disc++;
                            }
                            else
                            {
                                strand++;
                            }
                        }
                        else if (interleaving == InterleavingType.Partial) // Turns are interleaved, strands are not
                        {
                            if (isInterleavedTurn)
                            {
                                LogicalToPhysicalTurnMap[new LogicalConductorIndex(pair_start_turn + turn_in_disc + TurnsPerDisc, strand)] = physIndex;
                            }
                            else
                            {
                                LogicalToPhysicalTurnMap[new LogicalConductorIndex(pair_start_turn + turn_in_disc, strand)] = physIndex;
                            }
                            if (strand == NumParallelConductors - 1)
                            {
                                strand = 0;
                                turn_in_disc++;
                                isInterleavedTurn = !isInterleavedTurn;
                            }
                            else
                            {
                                strand++;
                            }
                        }
                        else if (interleaving == InterleavingType.Full)
                        {
                            if (isInterleavedTurn)
                            {
                                LogicalToPhysicalTurnMap[new LogicalConductorIndex(pair_start_turn + turn_in_disc + TurnsPerDisc, strand)] = physIndex;
                            }
                            else
                            {
                                LogicalToPhysicalTurnMap[new LogicalConductorIndex(pair_start_turn + turn_in_disc, strand)] = physIndex;
                            }
                            if (isInterleavedTurn)
                            {
                                if (strand == NumParallelConductors - 1)
                                {
                                    strand = 0;
                                    turn_in_disc++;
                                    isInterleavedTurn = false;
                                }
                                else
                                {
                                    isInterleavedTurn = false;
                                    strand++;
                                }
                            }
                            else
                            {
                                isInterleavedTurn = true;
                            }
                        }

                    }
                    pair_start_turn += TurnsPerDisc;
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
