using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
    public static class TestModels
    {
        public static Transformer TB904_ThreePhase()
        {
            var transformer = new Transformer
            {
                Core = new Core
                {
                    CoreLegRadius_mm = 700.0,
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
                        Name = "TV",
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
                        Name = "LV",
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
                        Name = "HV",
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
                        Name = "RV",
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
    }
}