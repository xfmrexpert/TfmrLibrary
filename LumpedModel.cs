using MathNet.Numerics;
using MathNet.Numerics.Distributions;
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
        public Matrix_d Gamma { get; set; } // turn lengths

        private int total_turns = 0;

        public LumpedModel(Transformer tfmr, IRLCMatrixCalculator matrixCalculator) : base(tfmr, matrixCalculator) { }
        public LumpedModel(Transformer tfmr, IRLCMatrixCalculator matrixCalculator, double minFreq, double maxFreq, int numSteps) : base(tfmr, matrixCalculator, minFreq, maxFreq, numSteps) { }

        protected override void Initialize()
        {
            total_turns = 0;
            foreach (Winding wdg in Tfmr.Windings)
            {
                total_turns += wdg.num_turns;
            }

            // Gamma is the diagonal matrix of conductors radii
            Gamma = M_d.Dense(total_turns, total_turns); 

            int start = 0;
            foreach (Winding wdg in Tfmr.Windings)
            {
                if (wdg.num_turns > 0)
                {
                    // Add winding turn length to Gamma
                    Gamma.SetSubMatrix(start, start, M_d.DenseOfDiagonalVector(2d * Math.PI * wdg.Calc_TurnRadii()));
                    start += wdg.num_turns;
                }
            }

            C = M_d.Dense(total_turns, total_turns);

            var C_PUL = MatrixCalculator.Calc_Cmatrix(Tfmr);

            C = Gamma * C_PUL;

            // branch-node incidence matrix
            // in this context, this matrix relates the inductor currents and the node voltages
            Q = CalcIncidenceMatrix();
        }

        public Matrix<double> CalcIncidenceMatrix()
        {
            // branch-node incidence matrix
            // in this context, this matrix relates the inductor currents and the node voltages
            var Q = M_d.Dense(total_turns, total_turns);
            // rows = branches
            // columns = nodes
            int start_turn = 0;
            foreach (var wdg in Tfmr.Windings)
            {
                if (wdg.num_turns == 0) continue;
                for (int t = 0; t < wdg.num_turns; t++)
                {
                    // t is branch number
                    // first node in branch 
                    Q[start_turn + t, start_turn + t] = 1.0;
                    if (t != (wdg.num_turns - 1))
                    {
                        Q[start_turn + t, start_turn + t + 1] = -1.0;
                    }
                }
                start_turn += wdg.num_turns;
            }
            
            return Q;
        }

        public override (Complex Z_term, Vector_c V_EndOfTurn) CalcResponseAtFreq(double f)
        {
            Vector_c V_TurnEnd_AtF = V_c.Dense(total_turns-1);

            Matrix_d L_PUL = MatrixCalculator.Calc_Lmatrix(Tfmr, f);

            Matrix_d L = Gamma * L_PUL;

            Matrix_d R_PUL = MatrixCalculator.Calc_Rmatrix(Tfmr, f);

            Matrix_d R = Gamma * R_PUL;

            Matrix_d G = M_d.Dense(total_turns, total_turns);
            G = Math.Tan(Tfmr.ins_loss_factor) * 2d * Math.PI * f * C;

            Matrix_c Z = M_c.Dense(0, 0);

            //Y = 1j * 2 * math.pi * f * C + Q.transpose() @np.linalg.inv(R + 1j * 2 * math.pi * f * L)@Q
            var Y = G.ToComplex() + Complex.ImaginaryOne * 2 * Math.PI * f * C.ToComplex() + Q.ToComplex().Transpose() * (R.ToComplex() + Complex.ImaginaryOne * 2 * Math.PI * f * L.ToComplex()).Inverse() * Q.ToComplex();
            if (!Y.ConditionNumber().IsInfinity())
            {
                Z = Y.Inverse();
            }
            else
            {
                Console.WriteLine("Matrix is shite");
            }
            
            for (int t = 0; t < (total_turns-1); t++)
            {
                V_TurnEnd_AtF[t] = Z[0, t+1] / Z[0, 0];
            }

            return (Z[0, 0], V_TurnEnd_AtF);
        }
    }
}
