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
            var transformer = new Transformer();
            transformer.Core.CoreLegRadius_mm = 700.0; // core radius
            transformer.Core.NumLegs = 3; // number of core legs
            transformer.Core.NumWoundLegs = 3; // number of legs with windings
            transformer.Core.WindowWidth_mm = 715.0; // width of the core window
            transformer.Core.WindowHeight_mm = 2230.0; // height of the core window

            // Create windings
            var TV_wdg = new Winding
            {
                Segments = new List<WindingSegment>()
                {
                    new WindingSegment
                    {
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
                                    new SpacerPatternElement { Count = 9, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 9, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 9, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 9, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 9, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 9, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 9, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 9, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 9, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 9, SpacerHeight_mm = 4.2 }
                                }
                            }
                        }
                    }
                }
            };
            
            transformer.Windings.Add(TV_wdg);

            var LV_wdg = new Winding
            {
                Segments = new List<WindingSegment>()
                {
                    new WindingSegment
                    {
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
                            NumConductorsInParallel = 2,
                            NumTurns = 144,
                            SpacerPattern = new RadialSpacerPattern
                            {
                                SpacerWidth_mm = 30.0,
                                NumSpacers_Circumference = 32,
                                Elements = new List<SpacerPatternElement>
                                {
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 }
                                }
                            }
                        }
                    }
                }
            };
            transformer.Windings.Add(LV_wdg);

            var HV_wdg = new Winding
            {
                Segments = new List<WindingSegment>()
                {
                    new WindingSegment
                    {
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
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 },
                                    new SpacerPatternElement { Count = 1, SpacerHeight_mm = 5.3 },
                                    new SpacerPatternElement { Count = 11, SpacerHeight_mm = 4.2 }
                                }
                            }
                        }
                    }
                }
            };
            transformer.Windings.Add(HV_wdg);

            var RW_wdg = new Winding
            {
                
            };
            var RW_seg1 = new WindingSegment
            {
                Geometry = new MultiStartWindingGeometry
                {
                    
                }
            };
            RW_wdg.Segments.Add(RW_seg1);
            transformer.Windings.Add(RW_wdg);

            return transformer;
        }
    }
}
