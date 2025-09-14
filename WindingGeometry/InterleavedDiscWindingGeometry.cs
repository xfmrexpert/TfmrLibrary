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

        protected override Dictionary<LogicalConductorIndex, PhysicalConductorIndex> BuildTurnMap()
        {
            var LogicalToPhysicalTurnMap = new Dictionary<LogicalConductorIndex, PhysicalConductorIndex>();
            int num_disc_pairs = NumDiscs / 2;
            int pair_start_turn = 0;
            int disc = -1;
            for (int pair = 0; pair < num_disc_pairs; pair++)
            {
                var interleaving = Interleaving.Count > pair ? Interleaving[pair] : InterleavingType.None;
                for (int disc_in_pair = 0; disc_in_pair < 2; disc_in_pair++)
                {
                    disc++;
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
                            layer = TurnsPerDisc * NumParallelConductors - rad_pos;
                        else // Second disc in pair, by convention wound from inside to outside
                            layer = rad_pos;
                        var physIndex = new PhysicalConductorIndex(disc, layer);

                        if (interleaving == InterleavingType.None)
                        {
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
                }
            }
            return LogicalToPhysicalTurnMap;
            // pair_start_turn = 0;
            // int strand;
            // int physical_turn = -1;
            // // Walk through the discs
            // for (int disc = 0; disc < NumDiscs; disc++)
            // {
            //     // Figure out which disc pair we're in so we can get the interleaving type
            //     int disc_pair = disc / 2;
            //     int disc_in_par = disc % 2;
            //     InterleavingType interleaving = Interleaving.Count > disc_pair ? Interleaving[disc_pair] : InterleavingType.None;
            //     // Walk through the radial positions in the disc (inside to outside)
            //     for (int rad_pos = 0; rad_pos < TurnsPerDisc * NumParallelConductors; rad_pos++)
            //     {
            //         if (disc_in_par == 0) // First disc in pair, by convention wound from outside to inside
            //         {
            //             if (interleaving == InterleavingType.None)
            //             {
            //                 strand = rad_pos % NumParallelConductors;
            //                 int turn_in_disc = rad_pos / NumParallelConductors;
            //                 LogicalToPhysicalTurnMap[new LogicalConductorIndex(pair_start_turn + TurnsPerDisc - turn_in_disc, NumParallelConductors - strand)] = new PhysicalConductorIndex(disc, rad_pos);
            //             }
            //             else if (interleaving == InterleavingType.Partial) // Turns are interleaved, strands are not
            //             {
            //                 strand = rad_pos % NumParallelConductors;
            //                 int turn_in_disc = rad_pos / NumParallelConductors;
            //                 LogicalToPhysicalTurnMap[new LogicalConductorIndex(pair_start_turn + TurnsPerDisc - turn_in_disc, NumParallelConductors - strand)] = new PhysicalConductorIndex(disc, rad_pos);
            //             }
            //             else if (interleaving == InterleavingType.Full)
            //             {
            //                 strand = rad_pos % NumParallelConductors;
            //                 int turn_in_disc = rad_pos / NumParallelConductors;
            //                 LogicalToPhysicalTurnMap[new LogicalConductorIndex(pair_start_turn + TurnsPerDisc - turn_in_disc, NumParallelConductors - strand)] = new PhysicalConductorIndex(disc, rad_pos);
            //             }
            //         }
            //         else
            //         {
            //             if (interleaving == InterleavingType.None)
            //             {
            //                 strand = rad_pos % NumParallelConductors;
            //                 int turn_in_disc = rad_pos / NumParallelConductors;
            //                 LogicalToPhysicalTurnMap[new LogicalConductorIndex(pair_start_turn + turn_in_disc, strand)] = new PhysicalConductorIndex(disc, rad_pos);
            //             }
            //             else if (interleaving == InterleavingType.Partial) // Turns are interleaved, strands are not
            //             {
            //                 strand = rad_pos % NumParallelConductors;
            //                 int turn_in_disc = rad_pos / NumParallelConductors;
            //                 LogicalToPhysicalTurnMap[new LogicalConductorIndex(pair_start_turn + TurnsPerDisc - turn_in_disc, NumParallelConductors - strand)] = new PhysicalConductorIndex(disc, rad_pos);
            //             }
            //             else if (interleaving == InterleavingType.Full)
            //             {
            //                 strand = rad_pos % NumParallelConductors;
            //                 int turn_in_disc = rad_pos / NumParallelConductors;
            //                 LogicalToPhysicalTurnMap[new LogicalConductorIndex(pair_start_turn + TurnsPerDisc - turn_in_disc, NumParallelConductors - strand)] = new PhysicalConductorIndex(disc, rad_pos);
            //             }
            //         }

            //     }



            // for (int lTurn = 0; lTurn < 2 * TurnsPerDisc; lTurn++)
            // {
            //     for (int strand = 1; strand < NumParallelConductors; strand++)
            //     {
            //         switch (interleaving)
            //         {
            //             case InterleavingType.None:
            //                 // No interleaving
            //                 PhysToLogicalTurnMap[lTurn] = lTurn;
            //                 break;
            //             case InterleavingType.Partial:
            //             case InterleavingType.Full:
            //                 // Partial or full interleaving
            //                 if (lTurn < TurnsPerDisc)
            //                 {
            //                     // First half of the turns in the pair
            //                     physical_turn++;
            //                     PhysToLogicalTurnMap[physical_turn] = disc1 * TurnsPerDisc + lTurn;
            //                 }
            //                 else
            //                 {
            //                     // Second half of the turns in the pair
            //                     physical_turn++;
            //                     PhysToLogicalTurnMap[physical_turn] = disc2 * TurnsPerDisc + (lTurn - TurnsPerDisc);
            //                 }
            //                 break;
            //         }
            //     }

            // }
            //}
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
