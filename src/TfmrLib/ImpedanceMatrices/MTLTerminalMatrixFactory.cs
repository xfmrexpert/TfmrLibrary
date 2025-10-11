using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using LinAlg = MathNet.Numerics.LinearAlgebra;
using System.Numerics;

namespace TfmrLib
{
    using Matrix_d = LinAlg.Matrix<double>;
    using Matrix_c = LinAlg.Matrix<Complex>;
    using Vector_d = LinAlg.Vector<double>;
    using Vector_c = LinAlg.Vector<Complex>;

    public static class MTLTerminalMatrixFactory
    {
        static private MatrixBuilder<double> M_d = Matrix_d.Build;
        static private MatrixBuilder<Complex> M_c = Matrix_c.Build;
        static private VectorBuilder<double> V_d = Vector_d.Build;
        static private VectorBuilder<Complex> V_c = Vector_c.Build;

        // I believe the way I interpret HA and HB is that they form a series of constraints that, for example,
        // set the voltage at the end of one turn to the voltage at the start of the next turn. 
        //
        // I think that the first and last rows correspond to terminal conditions (connected impedance, for example)
        // The rows in between should relate to 1) voltage constraints tieing the voltage at the end of one turn to
        // the voltage at the start of the next and 2) current constraints constraining the current out of one turn end
        // to be the opposite of the current into the next turn (or in other words setting the sum of currents at the 
        // interturn nodes to be 0). 
        //
        // In a regular winding segment, we'll have a node at the segment start, a node at the segment end, and one or
        // more parallel paths of connected turns.  We assume that the parallel conductors making up a turn are only 
        // connected electrically at the start and end of the segment

        public static Matrix_d CalcHA(WindingSegment seg)
        {
            // Matrix_d HA11 = M_d.DenseIdentity(wdg.NumConductors); // Identity matrix that corresponds to V(0)
            // Matrix_d HA21 = M_d.Dense(wdg.NumConductors, wdg.NumConductors); // Zero matrix
            // Matrix_d HA12 = M_d.Dense(wdg.NumConductors, wdg.NumConductors); // Matrix that should relate to V(2PI)
            // for (int t = 0; t < (wdg.NumConductors - 1); t++)
            // {
            //     HA12[t + 1, t] = -1.0;
            // }
            // Matrix_d HA22 = M_d.Dense(wdg.NumConductors, wdg.NumConductors);
            // HA22[wdg.NumConductors - 1, wdg.NumConductors - 1] = 1.0;
            // Matrix_d HA1 = HA11.Append(HA12);
            // Matrix_d HA2 = HA21.Append(HA22);
            // return HA1.Stack(HA2);
            var seg_geo = seg.Geometry;
            Matrix_d HA11 = M_d.Dense(seg_geo.NumConductors, seg_geo.NumConductors); // Start with all 0s
            Matrix_d HA12 = M_d.Dense(seg_geo.NumConductors, seg_geo.NumConductors);
            int row = 1;
            for (int turn = 1; turn < seg_geo.NumTurns; turn++) // Start at end of first turn
            {
                for (int strand = 0; strand < seg_geo.NumParallelConductors; strand++)
                {
                    // For each strand, we want to tie the end of [turn] to the start of [turn+1]
                    var cdr_idx = seg_geo.GetConductorIndex(turn, strand);
                    var cdr_idx_next = seg_geo.GetConductorIndex(turn + 1, strand);
                    HA11[row, cdr_idx] = 1;
                    HA12[row, cdr_idx_next] = -1;
                    row++;
                }
            }
            Matrix_d HA1 = HA11.Append(HA12);
            // FIXME: Build the rest you twat
            return HA1;
        }

        public static Matrix_c CalcHB(Winding wdg, double f)
        {
            Matrix_c HB11 = M_c.Dense(wdg.NumConductors, wdg.NumConductors);
            HB11[0, 0] = wdg.Rs+Complex.ImaginaryOne * 2 * Math.PI * f * wdg.Ls; //Source impedance
            Matrix_c HB12 = M_c.Dense(wdg.NumConductors, wdg.NumConductors);
            Matrix_c HB21 = M_c.Dense(wdg.NumConductors, wdg.NumConductors);
            for (int t = 0; t < (wdg.NumConductors - 1); t++)
            {
                HB21[t, t + 1] = 1.0;
            }
            Matrix_c HB22 = -1.0 * M_c.DenseIdentity(wdg.NumConductors);
            HB22[wdg.NumConductors - 1, wdg.NumConductors - 1] = wdg.Rl + Complex.ImaginaryOne * 2 * Math.PI * f * wdg.Ll; //Impedance to ground
            Matrix_c HB1 = HB11.Append(HB12);
            Matrix_c HB2 = HB21.Append(HB22);
            Matrix_c HB = HB1.Stack(HB2);
            return HB;
        }

    }
}