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

namespace TfmrLib
{
    public class ExtMatrixCalculator : IRLCMatrixCalculator
    {
        public double InductanceFudgeFactor { get; set; } = 1.0;
        public double SelfCapacitanceFudgeFactor { get; set; } = 1.0;
        public double MutualCapacitanceFudgeFactor { get; set; } = 1.0;

        public string DirectoryPath { get; set; }

        private List<(double Freq, Matrix<double> L_matrix)> L_matrices;

        public ExtMatrixCalculator(string directoryPath)
        {
            DirectoryPath = directoryPath;
            ReadInductances();
        }

        //PUL Inductances
        public Matrix<double> Calc_Lmatrix(Transformer tfmr, double f = 60)
        {
            if (f <= L_matrices[0].Freq) return L_matrices[0].L_matrix * InductanceFudgeFactor;
            if (f >= L_matrices[L_matrices.Count - 1].Freq) return L_matrices[L_matrices.Count - 1].L_matrix * InductanceFudgeFactor;
            for (int i = 0; i < L_matrices.Count - 1; i++)
            {
                if (f >= L_matrices[i].Freq && f <= L_matrices[i + 1].Freq)
                {
                    double f1 = L_matrices[i].Freq;
                    double f2 = L_matrices[i + 1].Freq;
                    var L1 = L_matrices[i].L_matrix;
                    var L2 = L_matrices[i + 1].L_matrix;

                    return (L1 + (L2 - L1) * (f - f1) / (f2 - f1)) * InductanceFudgeFactor;
                }
            }
            return null;
        }

        //PUL Capacitances
        public Matrix<double> Calc_Cmatrix(Transformer tfmr)
        {
            Matrix<double> C = DelimitedReader.Read<double>(DirectoryPath + "/C_getdp.csv", false, ",", false);
            Console.WriteLine($"C before: {C.RowSums().Sum()}");
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
            Console.WriteLine($"C after: {C.RowSums().Sum()}");
            return C;
        }

        private void ReadInductances()
        {
            List<(string FileName, double NumericValue)> filesWithValues = new List<(string FileName, double NumericValue)>();

            // Get all files in the directory
            string[] files = Directory.GetFiles(DirectoryPath);

            // Regex to extract the numeric value from the filename
            Regex regex = new Regex(@"L_getdp_(\d+\.\d+E\d+)", RegexOptions.IgnoreCase);

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);

                // Check if the filename starts with 'L_getdp'
                if (fileName.StartsWith("L_getdp", StringComparison.OrdinalIgnoreCase))
                {
                    Match match = regex.Match(fileName);
                    if (match.Success)
                    {
                        // Convert the extracted string to a double
                        double value = double.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.Float);

                        // Add both the filename and the numeric value to the list
                        filesWithValues.Add((fileName, value));
                    }
                }
            }

            // Sort the list by the numeric value
            filesWithValues.Sort((a, b) => a.NumericValue.CompareTo(b.NumericValue));

            L_matrices = new List<(double Freq, Matrix<double> L_matrix)>();

            // Output the sorted filenames and their values
            foreach (var file in filesWithValues)
            {
                L_matrices.Add((file.NumericValue, DelimitedReader.Read<double>(DirectoryPath + "/" + file.FileName, false, ",", false)));
            }
        }

        public LinAlg.Matrix<double> Calc_Rmatrix(Transformer tfmr, double f = 60)
        {
            int total_conductors = 0;
            foreach (Winding wdg in tfmr.Windings)
            {
                total_conductors += wdg.NumConductors;
            }

            LinAlg.Matrix<double> R = LinAlg.Matrix<double>.Build.Dense(total_conductors, total_conductors);

            int start = 0;
            foreach (Winding wdg in tfmr.Windings)
            {
                foreach (WindingSegment seg in wdg.Segments)
                {
                    R.SetSubMatrix(start, start, seg.Geometry.Calc_Rmatrix(f));
                    start += seg.Geometry.NumConductors;
                }
            }
            return R;
        }

        
    }
}
