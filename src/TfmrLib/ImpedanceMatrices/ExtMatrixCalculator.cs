using GeometryLib;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LinAlg = MathNet.Numerics.LinearAlgebra;
using Vector_d = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MatrixExponential;
using TfmrLib.FEM;
using System.Numerics;

namespace TfmrLib
{
    public class ExtMatrixCalculator : IRLCMatrixCalculator
    {
        public double InductanceFudgeFactor { get; set; } = 1.0;
        public double SelfCapacitanceFudgeFactor { get; set; } = 1.0;
        public double MutualCapacitanceFudgeFactor { get; set; } = 1.0;

        public string LR_file { get; set; }
        public string C_file { get; set; } 

        private List<(double Freq, Matrix<double> L_matrix)> L_matrices;
        private List<(double Freq, Matrix<double> R_matrix)> R_matrices;
        private Matrix<double> C_matrix;

        public ExtMatrixCalculator(string lr_file, string c_file)
        {
            LR_file = lr_file;
            C_file = c_file;
            ReadMatrices();
        }

        public Matrix<double> Calc_Lmatrix(Transformer tfmr, double f)
        {
            if (f <= L_matrices[0].Freq) return L_matrices[0].L_matrix * InductanceFudgeFactor;
            for (int i = 0; i < L_matrices.Count - 1; i++)
            {
               if (f >= L_matrices[i].Freq && f <= L_matrices[i + 1].Freq)
               {
                   double f1 = L_matrices[i].Freq;
                   double f2 = L_matrices[i + 1].Freq;
                   var L1 = L_matrices[i].L_matrix;
                   var L2 = L_matrices[i + 1].L_matrix;

                   return L1 + (L2 - L1) * (f - f1) / (f2 - f1);
               }
            }
            return L_matrices[L_matrices.Count - 1].L_matrix * InductanceFudgeFactor;
        }

        // PUL Inductances
        public List<(double, Matrix<double>)> Calc_Lmatrix(Transformer tfmr, FrequencySpec freq)
        {
            return L_matrices;
        }

        //PUL Capacitances
        public Matrix<double> Calc_Cmatrix(Transformer tfmr)
        {
            var C = C_matrix.Clone();
            for (int i = 0; i < C.RowCount; i++)
            {
                for (int j = i; j < C.ColumnCount; j++)
                {
                    if (i == j)
                    {
                        C[i, j] += (SelfCapacitanceFudgeFactor - 1.0) * C.Row(i).Sum();
                    }
                    else
                    {
                        C[i, i] -= (MutualCapacitanceFudgeFactor - 1.0) * C[i, j];
                        C[j, j] -= (MutualCapacitanceFudgeFactor - 1.0) * C[i, j];
                        C[i, j] *= MutualCapacitanceFudgeFactor;
                        C[j, i] *= MutualCapacitanceFudgeFactor;

                    }
                }
            }
            
            return C;
        }

        private void ReadMatrices()
        {
            var L_matrices = new List<(double, Matrix<double>)>();
            var R_matrices = new List<(double, Matrix<double>)>();

            var LR_results = MFEMResultsReader.Read(LR_file);

            // Read the L matrices from the output directory and return them as a list of tuples (frequency, L matrix)
            foreach (var sample in LR_results.Coupling.Samples)
            {
                L_matrices.Add((sample.FrequencyHz, sample.InductanceMatrix));
                R_matrices.Add((sample.FrequencyHz, sample.ResistanceMatrix));
            }

            var C_results = MFEMResultsReader.Read(C_file);
            C_matrix = C_results.Coupling.CapacitanceMatrix;

        }

        public LinAlg.Matrix<double> Calc_Rmatrix(Transformer tfmr, double f)
        {
            if (f <= R_matrices[0].Freq) return R_matrices[0].R_matrix;
            for (int i = 0; i < R_matrices.Count - 1; i++)
            {
               if (f >= R_matrices[i].Freq && f <= R_matrices[i + 1].Freq)
               {
                   double f1 = R_matrices[i].Freq;
                   double f2 = R_matrices[i + 1].Freq;
                   var R1 = R_matrices[i].R_matrix;
                   var R2 = R_matrices[i + 1].R_matrix;

                   return R1 + (R2 - R1) * (f - f1) / (f2 - f1);
               }
            }
            return R_matrices[R_matrices.Count - 1].R_matrix;
        }

        public List<(double, Matrix<double>)> Calc_Rmatrix(Transformer tfmr, FrequencySpec freq)
        {
            
            
            return R_matrices;
        }


        
    }
}
