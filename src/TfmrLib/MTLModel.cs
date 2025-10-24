using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MatrixExponential;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TfmrLib;
using LinAlg = MathNet.Numerics.LinearAlgebra;

namespace TfmrLib
{
    using Matrix_d = LinAlg.Matrix<double>;
    using Matrix_c = LinAlg.Matrix<Complex>;
    using Vector_d = LinAlg.Vector<double>;
    using Vector_c = LinAlg.Vector<Complex>;

    public class MTLModel : FreqResponseModel
    {
        private int total_cdrs = 0;
        private int total_turns = 0; 
        private Matrix_d HA;
        private Matrix_d Gamma;

        private Matrix_d C;

        public MTLModel(Transformer tfmr, IRLCMatrixCalculator matrixCalculator) : base(tfmr, matrixCalculator) { }
        public MTLModel(Transformer tfmr, IRLCMatrixCalculator matrixCalculator, double minFreq, double maxFreq, int numSteps) : base(tfmr, matrixCalculator, minFreq, maxFreq, numSteps) { }

        // The following follows the axisymmetric MTL calulation outline in the Fattal paper
        // HA is lower left-hand quadrant of left-hand side matrix
        // HB is lower right-hand quadrant of the LHS matric
        // HA and HB are "terminal" constraints dictated by winding type (terminal here meaning the
        // terminations of each winding turn when viewed as parallel transmission lines)
        // public Matrix_d CalcHAforWdg(Winding wdg)   
        // {
        //     Matrix_d HA11 = M_d.DenseIdentity(wdg.NumConductors);
        //     Matrix_d HA21 = M_d.Dense(wdg.NumConductors, wdg.NumConductors);
        //     Matrix_d HA12 = M_d.Dense(wdg.NumConductors, wdg.NumConductors);
        //     for (int t = 0; t < (wdg.NumConductors - 1); t++)
        //     {
        //         HA12[t + 1, t] = -1.0;
        //     }
        //     Matrix_d HA22 = M_d.Dense(wdg.NumConductors, wdg.NumConductors);
        //     HA22[wdg.NumConductors - 1, wdg.NumConductors - 1] = 1.0;
        //     Matrix_d HA1 = HA11.Append(HA12);
        //     Matrix_d HA2 = HA21.Append(HA22);
        //     return HA1.Stack(HA2);
        // }

        // public Matrix_d CalcHA()
        // {
        //     Matrix_d HA = M_d.Dense(2*total_turns, 2*total_turns);
        //     int start = 0;
        //     foreach (Winding wdg in Tfmr.Windings)
        //     {
        //         if (wdg.NumConductors > 0)
        //         {
        //             Matrix_d HA_wdg = CalcHAforWdg(wdg);
        //             HA.SetSubMatrix(start, start, HA_wdg);
        //             start += wdg.NumConductors;
        //         }
        //     }
        //     return HA;
        // }

        // public Matrix_c CalcHBforWdg(Winding wdg, double f)
        // {
        //     Matrix_c HB11 = M_c.Dense(wdg.NumConductors, wdg.NumConductors);
        //     HB11[0, 0] = wdg.Rs+Complex.ImaginaryOne * 2 * Math.PI * f * wdg.Ls; //Source impedance
        //     Matrix_c HB12 = M_c.Dense(wdg.NumConductors, wdg.NumConductors);
        //     Matrix_c HB21 = M_c.Dense(wdg.NumConductors, wdg.NumConductors);
        //     for (int t = 0; t < (wdg.NumConductors - 1); t++)
        //     {
        //         HB21[t, t + 1] = 1.0;
        //     }
        //     Matrix_c HB22 = -1.0 * M_c.DenseIdentity(wdg.NumConductors);
        //     HB22[wdg.NumConductors - 1, wdg.NumConductors - 1] = wdg.Rl + Complex.ImaginaryOne * 2 * Math.PI * f * wdg.Ll; //Impedance to ground
        //     Matrix_c HB1 = HB11.Append(HB12);
        //     Matrix_c HB2 = HB21.Append(HB22);
        //     Matrix_c HB = HB1.Stack(HB2);
        //     return HB;
        // }

        // public Matrix_c CalcHB(double f)
        // {
        //     Matrix_c HB = M_c.Dense(2 * total_turns, 2 * total_turns);
        //     int start = 0;
        //     foreach (Winding wdg in Tfmr.Windings)
        //     {
        //         if (wdg.NumTurns > 0)
        //         {
        //             Matrix_c HB_wdg = CalcHBforWdg(wdg, f);
        //             HB.SetSubMatrix(start, start, HB_wdg);
        //             start += wdg.NumConductors;
        //         }
        //     }
        //     return HB;
        // }

        // Return value should be complex vector of voltage response at each turn
        public override (Complex Z_term, Vector_c V_EndOfTurn) CalcResponseAtFreq(double f)
        {
            Matrix_c HB = MTLTerminalMatrixFactory.CalcHB(Tfmr, f);

            Matrix_c B2 = HA.ToComplex().Append(HB);

            Matrix_d L = MatrixCalculator.Calc_Lmatrix(Tfmr, f);
            Matrix_d R_f = MatrixCalculator.Calc_Rmatrix(Tfmr, f);
            //L.DisplayMatrixAsTable();
            //R_f.DisplayMatrixAsTable();
            // A = [           0              -Gamma*(R+j*2*pi*f*L)]
            //     [ -Gamma*(G+j*2*pi*f*C)                0        ]
            Matrix_c A11 = M_c.Dense(total_turns, total_turns);
            Matrix_c A12 = -Gamma.ToComplex() * (R_f.ToComplex() + Complex.ImaginaryOne * 2d * Math.PI * f * L.ToComplex());
            Matrix_c A21 = -Gamma.ToComplex() * ((Math.Tan(Tfmr.ins_loss_factor) * 2d * Math.PI * f * C).ToComplex() + Complex.ImaginaryOne * 2d * Math.PI * f * C.ToComplex());
            Matrix_c A22 = M_c.Dense(total_turns, total_turns);
            //Matrix_c A1 = M_c.Dense(Wdg.num_turns, Wdg.num_turns).Append(A12);
            //Matrix_c A2 = A21.Append(M_c.Dense(Wdg.num_turns, Wdg.num_turns));
            //Gamma.DisplayMatrixAsTable();
            Matrix_c A = M_c.DenseOfMatrixArray(new Matrix_c[,] { { A11, A12 }, { A21, A22 } });
            //A.DisplayMatrixAsTable();
            Matrix_c Phi = A.Exponential();
            Matrix_c Phi1 = Phi.SubMatrix(0, Phi.RowCount, 0, total_turns); //Phi[:,:n]
            Matrix_c Phi2 = Phi.SubMatrix(0, Phi.RowCount, total_turns, Phi.ColumnCount - total_turns); //Phi[:, n:]
            Matrix_c B11 = Phi1.Append((-1.0 * M_c.DenseIdentity(total_turns)).Stack(M_c.Dense(total_turns, total_turns)));
            Matrix_c B12 = Phi2.Append(M_c.Dense(total_turns, total_turns).Stack(-1.0 * M_c.DenseIdentity(total_turns)));
            Matrix_c B1 = B11.Append(B12);
            Matrix_c B = B1.Stack(B2);
            // v = [ V_turn_start ]
            //     [ V_turn_end   ]
            //     [ I_turn_start ]
            //     [ I_turn_end   ]
            Vector_c v = V_c.Dense(4 * total_turns);
            v[2 * total_turns] = 1.0; // Set applied voltage
            var x = B.Solve(v);
            var turn_end_voltages = x.SubVector(total_turns, 2* total_turns); // This should be grabbed the voltages at the _end_ of each turn
            return (Complex.One / x[2* total_turns], turn_end_voltages); //Divide by terminal voltage to get gain
        }

        protected override void Initialize()
        {
            total_cdrs = 0;
            total_turns = 0;
            foreach (Winding wdg in Tfmr.Windings)
            {
                total_cdrs += wdg.NumConductors;
                total_turns += wdg.NumTurns;
            }

            C = MatrixCalculator.Calc_Cmatrix(Tfmr);

            Gamma = M_d.Dense(total_turns, total_turns);

            // Gamma is the diagonal matrix of conductors radii (eq. 2)
            int start = 0;
            foreach (Winding wdg in Tfmr.Windings)
            {
                foreach (WindingSegment seg in wdg.Segments)
                {
                    // Add winding turn length to Gamma
                    Gamma.SetSubMatrix(start, start, M_d.DenseOfDiagonalVector(seg.Geometry.GetTurnLengths_m()));
                    start += seg.Geometry.NumConductors;
                }
            }    
            
            HA = MTLTerminalMatrixFactory.CalcHA(Tfmr);
        }
    }
}
