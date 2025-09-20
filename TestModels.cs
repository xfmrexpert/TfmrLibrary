using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
    public static class TestModels
    {
        public static Transformer TestTransformer()
        {
            var transformer = new Transformer
            {
                Core = new Core
                {
                    CoreLegRadius_mm = Conversions.in_to_mm(12.1),
                    NumLegs = 1,
                    NumWoundLegs = 1,
                    WindowWidth_mm = Conversions.in_to_mm(40.0),
                    WindowHeight_mm = Conversions.in_to_mm(40.0)
                },
                Windings =
                {
                    new Winding
                    {
                        Label = "Winding 1",
                        Segments =
                        {
                            new WindingSegment
                            {
                                Label = "Segment 1",
                                Geometry = new DiscWindingGeometry
                                {
                                    ConductorType = new RectConductor
                                    {
                                        StrandHeight_mm = Conversions.in_to_mm(0.3),
                                        StrandWidth_mm = Conversions.in_to_mm(0.085),
                                        CornerRadius_mm = Conversions.in_to_mm(0.032),
                                        InsulationThickness_mm = Conversions.in_to_mm(0.018)
                                    },
                                    NumDiscs = 6,
                                    TurnsPerDisc = 10,
                                    NumTurns = 40,
                                    SpacerPattern = new RadialSpacerPattern
                                    {
                                        SpacerWidth_mm = 20.0,
                                        NumSpacers_Circumference = 16,
                                        Elements = new List<SpacerPatternElement>
                                        {
                                            new() { Count = 5, SpacerHeight_mm = Conversions.in_to_mm(0.188) }
                                        }
                                    },
                                    InnerRadius_mm = Conversions.in_to_mm(15.25),
                                    DistanceAboveBottomYoke_mm = Conversions.in_to_mm(15.0)
                                }
                            }
                        }
                    },
                    new Winding
                    {
                        Label = "Winding 2",
                        Segments =
                        {
                            new WindingSegment
                            {
                                Label = "Segment 1",
                                Geometry = new InterleavedDiscWindingGeometry
                                {
                                    ConductorType = new RectConductor
                                    {
                                        StrandHeight_mm = Conversions.in_to_mm(0.3),
                                        StrandWidth_mm = Conversions.in_to_mm(0.085),
                                        CornerRadius_mm = Conversions.in_to_mm(0.032),
                                        InsulationThickness_mm = Conversions.in_to_mm(0.018)
                                    },
                                    NumDiscs = 6,
                                    TurnsPerDisc = 10,
                                    NumTurns = 60,
                                    NumParallelConductors = 2,
                                    SpacerPattern = new RadialSpacerPattern
                                    {
                                        SpacerWidth_mm = 20.0,
                                        NumSpacers_Circumference = 16,
                                        Elements = new List<SpacerPatternElement>
                                        {
                                            new() { Count = 5, SpacerHeight_mm = Conversions.in_to_mm(0.188) }
                                        }
                                    },
                                    Interleaving = new() { InterleavedDiscWindingGeometry.InterleavingType.Full, InterleavedDiscWindingGeometry.InterleavingType.Partial, InterleavedDiscWindingGeometry.InterleavingType.None },
                                    InnerRadius_mm = Conversions.in_to_mm(20.25),
                                    DistanceAboveBottomYoke_mm = Conversions.in_to_mm(15.0)
                                }
                            }
                        }
                    }
                }
            };
            return transformer;
        }

        public static Transformer ModelWinding()
        {
            var transformer = new Transformer
            {
                Core = new Core
                {
                    CoreLegRadius_mm = Conversions.in_to_mm(12.1),
                    NumLegs = 1,
                    NumWoundLegs = 1,
                    WindowWidth_mm = Conversions.in_to_mm(40.0),
                    WindowHeight_mm = Conversions.in_to_mm(40.0)
                },
                Windings =
                {
                    new Winding
                    {
                        Label = "Winding 1",
                        Segments =
                        {
                            new WindingSegment
                            {
                                Label = "Segment 1",
                                Geometry = new DiscWindingGeometry
                                {
                                    ConductorType = new RectConductor
                                    {
                                        StrandHeight_mm = Conversions.in_to_mm(0.3),
                                        StrandWidth_mm = Conversions.in_to_mm(0.085),
                                        CornerRadius_mm = Conversions.in_to_mm(0.032),
                                        InsulationThickness_mm = Conversions.in_to_mm(0.018)
                                    },
                                    NumDiscs = 14,
                                    TurnsPerDisc = 20,
                                    NumTurns = 280,
                                    SpacerPattern = new RadialSpacerPattern
                                    {
                                        SpacerWidth_mm = 20.0,
                                        NumSpacers_Circumference = 16,
                                        Elements = new List<SpacerPatternElement>
                                        {
                                            new() { Count = 13, SpacerHeight_mm = Conversions.in_to_mm(0.188) }
                                        }
                                    },
                                    InnerRadius_mm = Conversions.in_to_mm(15.25),
                                    DistanceAboveBottomYoke_mm = Conversions.in_to_mm(15.0)
                                }
                            }
                        }
                    }
                }
            };
            return transformer;
        }

        public static Transformer TB904_ThreePhase()
        {
            var transformer = new Transformer
            {
                Core = new Core
                {
                    CoreLegRadius_mm = 700.0 / 2,
                    NumLegs = 3,
                    NumWoundLegs = 3,
                    WindowWidth_mm = 715.0,
                    WindowHeight_mm = 2230.0
                },
                Windings =
                {
                    // TV Winding
                    new Winding
                    {
                        Label = "TV",
                        Segments =
                        {
                            new WindingSegment
                            {
                                Label = "TV Main",
                                Geometry = new HelicalWindingGeometry
                                {
                                    ConductorType = new CTCConductor
                                    {
                                        NumStrands = 15,
                                        StrandHeight_mm = 6.9,
                                        StrandWidth_mm = 1.6,
                                        StrandInsulationThickness_mm = 0.15 / 2,
                                        InterleavingPaperThickness_mm = 0.105,
                                        InsulationThickness_mm = 0.61 / 2
                                    },
                                    NumTurns = 100,
                                    SpacerPattern = new RadialSpacerPattern
                                    {
                                        SpacerWidth_mm = 40.0,
                                        NumSpacers_Circumference = 28,
                                        Elements = new List<SpacerPatternElement>
                                        {
                                            new() { Count = 9, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 }
                                        }
                                    },
                                    InnerRadius_mm = 765.0/2,
                                    DistanceAboveBottomYoke_mm = 110.0
                                }
                            }
                        }
                    },

                    // LV Winding
                    new Winding
                    {
                        Label = "LV",
                        Segments =
                        {
                            new WindingSegment
                            {
                                Label = "LV Main",
                                Geometry = new HelicalWindingGeometry
                                {
                                    ConductorType = new CTCConductor
                                    {
                                        NumStrands = 45,
                                        StrandHeight_mm = 4.4,
                                        StrandWidth_mm = 1.5,
                                        StrandInsulationThickness_mm = 0.15 / 2,
                                        InterleavingPaperThickness_mm = 0.105,
                                        InsulationThickness_mm = 0.61 / 2
                                    },
                                    NumParallelConductors = 2,
                                    NumTurns = 144,
                                    SpacerPattern = new RadialSpacerPattern
                                    {
                                        SpacerWidth_mm = 30.0,
                                        NumSpacers_Circumference = 32,
                                        Elements = new List<SpacerPatternElement>
                                        {
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 }
                                        }
                                    },
                                    InnerRadius_mm = 1131.0/2,
                                    DistanceAboveBottomYoke_mm = 50.0
                                }
                            }
                        }
                    },

                    // HV Winding
                    new Winding
                    {
                        Label = "HV",
                        Segments =
                        {
                            new WindingSegment
                            {
                                Label = "HV Main",
                                Geometry = new DiscWindingGeometry
                                {
                                    ConductorType = new CTCConductor
                                    {
                                        NumStrands = 19,
                                        StrandHeight_mm = 6.6,
                                        StrandWidth_mm = 1.5,
                                        StrandInsulationThickness_mm = 0.15 / 2,
                                        InterleavingPaperThickness_mm = 0.105,
                                        InsulationThickness_mm = 1.37 / 2
                                    },
                                    NumTurns = 486,
                                    NumDiscs = 100,
                                    TurnsPerDisc = 5,
                                    SpacerPattern = new RadialSpacerPattern
                                    {
                                        SpacerWidth_mm = 30.0,
                                        NumSpacers_Circumference = 32,
                                        Elements = new List<SpacerPatternElement>
                                        {
                                            new() { Count = 1, SpacerHeight_mm = 8.6 },
                                            new() { Count = 15, SpacerHeight_mm = 6.4 },
                                            new() { Count = 4, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 6, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 8, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 6, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 6, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 6, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 6, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 6, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 6, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 4, SpacerHeight_mm = 4.2 },
                                            new() { Count = 15, SpacerHeight_mm = 6.4 },
                                            new() { Count = 1, SpacerHeight_mm = 8.6 }
                                        }
                                    },
                                    InnerRadius_mm = 871.0/2,
                                    DistanceAboveBottomYoke_mm = 80.0
                                }
                            }
                        }
                    },
                    // RV Winding
                    new Winding
                    {
                        Label = "RV",
                        Segments =
                        {
                            new WindingSegment
                            {
                                Label = "RV Main",
                                Geometry = new MultiStartWindingGeometry
                                {
                                    ConductorType = new RectConductor
                                    {
                                        StrandHeight_mm = 5.0,
                                        StrandWidth_mm = 12.0,
                                        InsulationThickness_mm = 1.22/2,
                                    },
                                    NumberOfStarts=10,
                                    NumberOfTurnsPerStart = 6,
                                    NumParallelConductors = 2,
                                    ParallelOrientation = Orientation.Axial,
                                    SpacerPattern = new RadialSpacerPattern
                                    {
                                        SpacerWidth_mm = 35.0,
                                        NumSpacers_Circumference = 32,
                                        Elements = new List<SpacerPatternElement>
                                        {
                                            new() { Count = 119, SpacerHeight_mm = 6.4 }
                                        }
                                    },
                                    InnerRadius_mm = 1349.0/2,
                                    DistanceAboveBottomYoke_mm = 215.0
                                }
                            }
                        }
                    },
                }
            };

            return transformer;
        }

        public static Transformer TB904_SinglePhase()
        {
            var transformer = new Transformer
            {
                Core = new Core
                {
                    CoreLegRadius_mm = 720.0/2,
                    NumLegs = 3,
                    NumWoundLegs = 1,
                    WindowWidth_mm = 530.0,
                    WindowHeight_mm = 2190.0
                },
                Windings =
                {
                    // TV Winding
                    new Winding
                    {
                        Label = "TV",
                        Segments =
                        {
                            new WindingSegment
                            {
                                Label = "TV Main",
                                Geometry = new HelicalWindingGeometry
                                {
                                    NumParallelConductors = 2,
                                    ParallelOrientation = Orientation.Axial,
                                    ConductorType = new CTCConductor
                                    {
                                        NumStrands = 29,
                                        StrandHeight_mm = 3.5,
                                        StrandWidth_mm = 1.5,
                                        StrandInsulationThickness_mm = 0.15 / 2,
                                        InterleavingPaperThickness_mm = 0.105,
                                        InsulationThickness_mm = 0.45 / 2
                                    },
                                    NumTurns = 83,
                                    SpacerPattern = new RadialSpacerPattern
                                    {
                                        SpacerWidth_mm = 30.0,
                                        NumSpacers_Circumference = 28,
                                        Elements = new List<SpacerPatternElement>
                                        {
                                            new() { Count = 13, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 11, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 }
                                        }
                                    },
                                    InnerRadius_mm = 756.0/2,
                                    DistanceAboveBottomYoke_mm = 60.0
                                }
                            }
                        }
                    },

                    // LV Winding
                    new Winding
                    {
                        Label = "LV",
                        Segments =
                        {
                            new WindingSegment
                            {
                                Label = "LV Main",
                                Geometry = new DiscWindingGeometry
                                {
                                    ConductorType = new CTCConductor
                                    {
                                        NumStrands = 21,
                                        StrandHeight_mm = 5.0,
                                        StrandWidth_mm = 1.5,
                                        StrandInsulationThickness_mm = 0.15 / 2,
                                        InterleavingPaperThickness_mm = 0.105,
                                        InsulationThickness_mm = 0.91 / 2
                                    },
                                    NumParallelConductors = 2,
                                    ParallelOrientation = Orientation.Radial,
                                    NumTurns = 240,
                                    NumDiscs = 128,
                                    TurnsPerDisc = 2,
                                    SpacerPattern = new RadialSpacerPattern
                                    {
                                        SpacerWidth_mm = 30.0,
                                        NumSpacers_Circumference = 28,
                                        Elements = new List<SpacerPatternElement>
                                        {
                                            new() { Count = 10, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 10, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 10, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 10, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 9, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 10, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 10, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 10, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 10, SpacerHeight_mm = 4.2 }
                                        }
                                    },
                                    InnerRadius_mm = 938.0/2,
                                    DistanceAboveBottomYoke_mm = 60.0
                                }
                            }
                        }
                    },

                    // HV Winding
                    new Winding
                    {
                        Label = "HV",
                        Segments =
                        {
                            new WindingSegment
                            {
                                Label = "HV Upper",
                                Geometry = new InterleavedDiscWindingGeometry
                                {
                                    ConductorType = new RectConductor
                                    {
                                        StrandHeight_mm = 9.9,
                                        StrandWidth_mm = 2.7,
                                        InsulationThickness_mm = 1.52 / 2
                                    },
                                    NumTurns = 800,
                                    NumDiscs = 62,
                                    TurnsPerDisc = 13,
                                    SpacerPattern = new RadialSpacerPattern
                                    {
                                        SpacerWidth_mm = 35.0,
                                        NumSpacers_Circumference = 32,
                                        Elements = new List<SpacerPatternElement>
                                        {
                                            new CompositeSpacerPatternElement { Count = 4, SubElements=[new () { Count = 1, SpacerHeight_mm = 4.2 }, new() { Count = 1, SpacerHeight_mm = 6.4 }] },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            //new() { Count = 2, SpacerHeight_mm = 10.6 },
                                            new CompositeSpacerPatternElement { Count = 2, SubElements=[new () { Count = 1, SpacerHeight_mm = 6.4 }, new() { Count = 1, SpacerHeight_mm = 4.2 }] },
                                            new() { Count = 4, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 6, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 8, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 8, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 4, SpacerHeight_mm = 4.2 },
                                            //new() { Count = 7, SpacerHeight_mm = 10.6 }
                                            new CompositeSpacerPatternElement { Count = 7, SubElements=[new () { Count = 1, SpacerHeight_mm = 6.4 }, new() { Count = 1, SpacerHeight_mm = 4.2 }] },
                                        }
                                    },
                                    InnerRadius_mm = 1242.0/2,
                                    DistanceAboveBottomYoke_mm = 1043.2
                                }
                            },
                            new WindingSegment
                            {
                                Label = "HV Lower",
                                Geometry = new InterleavedDiscWindingGeometry
                                {
                                    ConductorType = new RectConductor
                                    {
                                        StrandHeight_mm = 9.9,
                                        StrandWidth_mm = 2.7,
                                        InsulationThickness_mm = 1.52 / 2
                                    },
                                    NumTurns = 800,
                                    NumDiscs = 62,
                                    TurnsPerDisc = 13,
                                    SpacerPattern = new RadialSpacerPattern
                                    {
                                        SpacerWidth_mm = 35.0,
                                        NumSpacers_Circumference = 32,
                                        Elements = new List<SpacerPatternElement>
                                        {
                                            //new() { Count = 7, SpacerHeight_mm = 10.6 },
                                            new CompositeSpacerPatternElement { Count = 7, SubElements=[new () { Count = 1, SpacerHeight_mm = 6.4 }, new() { Count = 1, SpacerHeight_mm = 4.2 }] },
                                            new() { Count = 4, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 8, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 8, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 6, SpacerHeight_mm = 4.2 },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            new() { Count = 4, SpacerHeight_mm = 4.2 },
                                            //new() { Count = 2, SpacerHeight_mm = 10.6 },
                                            new CompositeSpacerPatternElement { Count = 2, SubElements=[new () { Count = 1, SpacerHeight_mm = 6.4 }, new() { Count = 1, SpacerHeight_mm = 4.2 }] },
                                            new() { Count = 1, SpacerHeight_mm = 5.3 },
                                            //new() { Count = 4, SpacerHeight_mm = 10.6 }
                                            new CompositeSpacerPatternElement { Count = 4, SubElements=[new () { Count = 1, SpacerHeight_mm = 4.2 }, new() { Count = 1, SpacerHeight_mm = 6.4 }] },
                                        }
                                    },
                                    InnerRadius_mm = 1242.0/2,
                                    DistanceAboveBottomYoke_mm = 60.0
                                }
                            }
                        }
                    },
                    // RV Winding
                    new Winding
                    {
                        Label = "RV",
                        Segments =
                        {
                            new WindingSegment
                            {
                                Label = "RV Upper",
                                Geometry = new InterleavedDiscWindingGeometry
                                {
                                    ConductorType = new RectConductor
                                    {
                                        StrandHeight_mm = 14.2,
                                        StrandWidth_mm = 2.2,
                                        InsulationThickness_mm = 1.52/2,
                                    },
                                    NumTurns = 80,
                                    NumDiscs = 20,
                                    TurnsPerDisc = 4,
                                    NumParallelConductors = 2,
                                    ParallelOrientation = Orientation.Radial,
                                    SpacerPattern = new RadialSpacerPattern
                                    {
                                        SpacerWidth_mm = 40.0,
                                        NumSpacers_Circumference = 32,
                                        Elements = new List<SpacerPatternElement>
                                        {
                                            new() { Count = 19, SpacerHeight_mm = 5.3 }
                                        }
                                    },
                                    InnerRadius_mm = 1612.0/2,
                                    DistanceAboveBottomYoke_mm = 1340.0
                                }
                            },
                            new WindingSegment
                            {
                                Label = "RV Lower",
                                Geometry = new InterleavedDiscWindingGeometry
                                {
                                    ConductorType = new RectConductor
                                    {
                                        StrandHeight_mm = 14.2,
                                        StrandWidth_mm = 2.2,
                                        InsulationThickness_mm = 1.52/2,
                                    },
                                    NumTurns = 80,
                                    NumDiscs = 20,
                                    TurnsPerDisc = 4,
                                    NumParallelConductors = 2,
                                    ParallelOrientation = Orientation.Radial,
                                    SpacerPattern = new RadialSpacerPattern
                                    {
                                        SpacerWidth_mm = 40.0,
                                        NumSpacers_Circumference = 32,
                                        Elements = new List<SpacerPatternElement>
                                        {
                                            new() { Count = 19, SpacerHeight_mm = 5.3 }
                                        }
                                    },
                                    InnerRadius_mm = 1612.0/2,
                                    DistanceAboveBottomYoke_mm = 333.0
                                }
                            }
                        }
                    },
                }
            };

            return transformer;
        }
    }
}