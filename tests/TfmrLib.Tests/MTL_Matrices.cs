using System.Numerics;
using TfmrLib;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra;

namespace TfmrLib.Tests;

public class MTL_Matrices
{
    private static Transformer TestTfmr()
    {
        double WindowHt = Conversions.in_to_mm(120.0);

        var tfmr = new Transformer()
        {
            Core = new Core
            {
                CoreLegRadius_mm = Conversions.in_to_mm(0.0),
                NumLegs = 1,
                NumWoundLegs = 1,
                WindowWidth_mm = WindowHt,
                WindowHeight_mm = WindowHt
            },
            Windings =
            {
                new Winding
                {
                    Label = "Disc Winding",
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
                                    InsulationThickness_mm = Conversions.in_to_mm(0.018),
                                    rho_c = 0
                                },
                                NumDiscs = 4,
                                TurnsPerDisc = 4,
                                NumTurns = 16,
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
                                DistanceAboveBottomYoke_mm = WindowHt / 2
                            }
                        }
                    }
                },
                new Winding
                {
                    Label = "Interleaved Disc Winding",
                    Segments =
                    {
                        new WindingSegment
                        {
                            Label = "Segment 1",
                            Geometry = new InterleavedDiscWindingGeometry
                            {
                                Interleaving = [.. Enumerable.Repeat(new InterleavingSchedule.InterleavedGroup(1, InterleavingSchedule.InterleavingType.FullStearns), 3)],
                                ConductorType = new RectConductor
                                {
                                    StrandHeight_mm = Conversions.in_to_mm(0.3),
                                    StrandWidth_mm = Conversions.in_to_mm(0.085),
                                    CornerRadius_mm = Conversions.in_to_mm(0.032),
                                    InsulationThickness_mm = Conversions.in_to_mm(0.018),
                                    rho_c = 0
                                },
                                NumDiscs = 4,
                                TurnsPerDisc = 4,
                                NumTurns = 16,
                                SpacerPattern = new RadialSpacerPattern
                                {
                                    SpacerWidth_mm = 20.0,
                                    NumSpacers_Circumference = 16,
                                    Elements = new List<SpacerPatternElement>
                                    {
                                        new() { Count = 5, SpacerHeight_mm = Conversions.in_to_mm(0.188) }
                                    }
                                },
                                InnerRadius_mm = Conversions.in_to_mm(25.25),
                                DistanceAboveBottomYoke_mm = WindowHt / 2
                            }
                        }
                    }
                }
            }
        };
        return tfmr;
    }

    private static void PrintMatrix(Matrix<double> matrix)
    {
        int rows = matrix.RowCount;
        int cols = matrix.ColumnCount;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                Console.Write($"{matrix[i, j]: 0;-0} ");
            }
            Console.WriteLine();
        }
    }

    [Fact]
    public void HA_Test()
    {
        var tfmr = TestTfmr();
        var HA_disc = MTLTerminalMatrixFactory.CalcHAForSeg(tfmr.Windings[0].Segments[0]);
        var HA_interleaved = MTLTerminalMatrixFactory.CalcHAForSeg(tfmr.Windings[1].Segments[0]);
        Console.WriteLine("Disc Winding:");
        PrintMatrix(HA_disc);
        Console.WriteLine("Interleaved Disc Winding:");
        PrintMatrix(HA_interleaved);
    }

}
