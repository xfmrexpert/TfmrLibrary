﻿using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
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

    public class LumpedModel : FreqResponseModel
    {
        public Matrix_d C { get; set; }
        public Matrix_d Q { get; set; }
        public Vector_d d_t { get; set; } // turn diameters

        public LumpedModel(Winding wdg) : base(wdg) { }
        public LumpedModel(Winding wdg, double minFreq, double maxFreq, int numSteps) : base(wdg, minFreq, maxFreq, numSteps) { }

        protected override void Initialize()
        {
            C = M_d.Dense(Wdg.num_turns, Wdg.num_turns);

            var C_matrix = Wdg.Calc_Cmatrix();

            d_t = 2 * Wdg.Calc_TurnRadii();

            for (int t = 0; t < Wdg.num_turns; t++)
            {
                C[t, t] = C_matrix[t, t] * Math.PI * d_t[t];
                for (int t2 = 0; t2 < Wdg.num_turns; t2++)
                {
                    if (t != t2)
                    {
                        C[t, t2] = C_matrix[t, t2] * Math.PI * d_t[t];
                    }
                }
            }

            // branch-node incidence matrix
            // in this context, this matrix relates the inductor currents and the node voltages
            Q = M_d.Dense(Wdg.num_turns, Wdg.num_turns);
            // rows = branches
            // columns = nodes
            for (int t = 0; t < Wdg.num_turns; t++)
            {
                // t is branch number
                // first node in branch 
                Q[t, t] = 1.0;
                if (t != (Wdg.num_turns - 1))
                {
                    Q[t, t + 1] = -1.0;
                }
            }
        }

        public override (Complex Z_term, Vector_c V_EndOfTurn) CalcResponseAtFreq(double f)
        {
            Vector_c V_TurnEnd_AtF = V_c.Dense(Wdg.num_turns-1);

            var L_matrix = Wdg.Calc_Lmatrix(f);

            Matrix_d L = M_d.Dense(Wdg.num_turns, Wdg.num_turns);

            for (int t = 0; t < Wdg.num_turns; t++)
            {
                L[t, t] = L_matrix[t, t] * Math.PI * d_t[t];
                for (int t2 = 0; t2 < Wdg.num_turns; t2++)
                {
                    if (t != t2)
                    {
                        L[t, t2] = L_matrix[t, t2] * Math.PI * d_t[t];
                    }
                }
            }

            Matrix_d R = Wdg.Calc_Rmatrix(f);
            for (int t = 0; t < Wdg.num_turns; t++)
            {
                R[t, t] = R[t, t] * Math.PI * d_t[t];
            }

            Matrix_d G = M_d.Dense(Wdg.num_turns, Wdg.num_turns);
            G = Math.Tan(Wdg.ins_loss_factor) * 2d * Math.PI * f * C;

            Matrix_c Z = M_c.Dense(0, 0);

            //Console.WriteLine($"Calculating at {f / 1e6}MHz");
            //Y = 1j * 2 * math.pi * f * C + Q.transpose() @np.linalg.inv(R + 1j * 2 * math.pi * f * L)@Q
            var Y = G.ToComplex() + Complex.ImaginaryOne * 2 * Math.PI * f * C.ToComplex() + Q.ToComplex().Transpose() * (R.ToComplex() + Complex.ImaginaryOne * 2 * Math.PI * f * L.ToComplex()).Inverse() * Q.ToComplex();
            if (!Y.ConditionNumber().IsInfinity())
            {
                //print(np.linalg.cond(Y))
                Z = Y.Inverse();
            }
            else
            {
                Console.WriteLine("Matrix is shite");
            }

            // TODO: Need to verify return values
            // UPDATE: THEM IS F'ING WRONG, NEED TO _FIX_ RETURN VALUES
            
            //Z_term.Add(Z[0, 0].Magnitude);
            
            for (int t = 0; t < (Wdg.num_turns-1); t++)
            {
                V_TurnEnd_AtF[t] = Z[0, t+1] / Z[0, 0];
            }

            return (Z[0, 0], V_TurnEnd_AtF);
        }
    }
}
