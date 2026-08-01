// SPDX-License-Identifier: MIT

using MathNet.Numerics.LinearAlgebra;
using System.Numerics;
using System.Diagnostics;
using System.Text;

namespace MatrixExponential
{
    public static class Extensions
    {
        public static void DisplayMatrixAsTable(this Matrix<double> matrix)
        {
            StringBuilder html = new StringBuilder();
            html.AppendLine("<html><head><title>Matrix Display</title></head><body>");
            html.AppendLine("<table border='1'>");

            for (int i = 0; i < matrix.RowCount; i++)
            {
                html.AppendLine("<tr>");
                for (int j = 0; j < matrix.ColumnCount; j++)
                {
                    html.AppendFormat("<td>{0}</td>", matrix[i, j]);
                }
                html.AppendLine("</tr>");
            }

            html.AppendLine("</table>");
            html.AppendLine("</body></html>");

            // Generate a unique filename in the temporary directory
            string fileName = Path.Combine(Path.GetTempPath(), $"MatrixDisplay_{Guid.NewGuid()}.html");
            File.WriteAllText(fileName, html.ToString());

            // Open the HTML file in the default web browser
            Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = true
            });
        }

        public static void DisplayMatrixAsTable(this Matrix<Complex> matrix)
        {
            StringBuilder html = new StringBuilder();
            html.AppendLine("<html><head><title>Matrix Display</title></head><body>");
            html.AppendLine("<table border='1'>");

            for (int i = 0; i < matrix.RowCount; i++)
            {
                html.AppendLine("<tr>");
                for (int j = 0; j < matrix.ColumnCount; j++)
                {
                    html.AppendFormat("<td>{0}</td>", matrix[i, j]);
                }
                html.AppendLine("</tr>");
            }

            html.AppendLine("</table>");
            html.AppendLine("</body></html>");

            // Generate a unique filename in the temporary directory
            string fileName = Path.Combine(Path.GetTempPath(), $"MatrixDisplay_{Guid.NewGuid()}.html");
            File.WriteAllText(fileName, html.ToString());

            // Open the HTML file in the default web browser
            Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = true
            });
        }

        public static void PrintMatrix(this Matrix<Complex> matrix)
        {
            int rows = matrix.RowCount;
            int cols = matrix.ColumnCount;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Console.Write($"{matrix[i, j]} ");
                }
                Console.WriteLine();
            }
        }

        public static void PrintMatrix(this Matrix<double> matrix)
        {
            int rows = matrix.RowCount;
            int cols = matrix.ColumnCount;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Console.Write($"{matrix[i, j]:F4} ");
                }
                Console.WriteLine();
            }
        }
    
        public static Matrix<Complex> Exponential(this Matrix<Complex> m)
        {
            if (m.RowCount != m.ColumnCount)
                throw new ArgumentException("Matrix should be square");

            if (m.RowCount == 0)
                throw new ArgumentException("Zero-size matrix.");

            if (m.IsDiagonal())
                return Matrix<Complex>.Build.DenseOfDiagonalVector(m.Diagonal().PointwiseExp());

            const double theta3 = 1.495585217958292e-2;
            const double theta5 = 2.539398330063230e-1;
            const double theta7 = 9.504178996162932e-1;
            const double theta9 = 2.097847961257068;
            const double theta13 = 5.371920351148152;

            double norm = m.L1Norm();
            int squarings = norm > theta13
                ? Math.Max(0, (int)Math.Ceiling(Math.Log2(norm / theta13)))
                : 0;
            Matrix<Complex> scaled = squarings == 0
                ? m
                : m * Math.ScaleB(1.0, -squarings);
            double scaledNorm = squarings == 0 ? norm : norm * Math.ScaleB(1.0, -squarings);

            Matrix<Complex> result = scaledNorm <= theta3
                ? Pade3(scaled)
                : scaledNorm <= theta5
                    ? Pade5(scaled)
                    : scaledNorm <= theta7
                        ? Pade7(scaled)
                        : scaledNorm <= theta9
                            ? Pade9(scaled)
                            : Pade13(scaled);

            for (int i = 0; i < squarings; i++)
                result = result * result;

            return result;
        }

        private static Matrix<Complex> Pade3(Matrix<Complex> matrix)
        {
            Matrix<Complex> identity = Matrix<Complex>.Build.DenseIdentity(matrix.RowCount);
            Matrix<Complex> a2 = matrix * matrix;
            Matrix<Complex> u = matrix * (a2 + 60.0 * identity);
            Matrix<Complex> v = 12.0 * a2 + 120.0 * identity;
            return SolvePade(u, v);
        }

        private static Matrix<Complex> Pade5(Matrix<Complex> matrix)
        {
            Matrix<Complex> identity = Matrix<Complex>.Build.DenseIdentity(matrix.RowCount);
            Matrix<Complex> a2 = matrix * matrix;
            Matrix<Complex> a4 = a2 * a2;
            Matrix<Complex> u = matrix * (a4 + 420.0 * a2 + 15120.0 * identity);
            Matrix<Complex> v = 30.0 * a4 + 3360.0 * a2 + 30240.0 * identity;
            return SolvePade(u, v);
        }

        private static Matrix<Complex> Pade7(Matrix<Complex> matrix)
        {
            Matrix<Complex> identity = Matrix<Complex>.Build.DenseIdentity(matrix.RowCount);
            Matrix<Complex> a2 = matrix * matrix;
            Matrix<Complex> a4 = a2 * a2;
            Matrix<Complex> a6 = a4 * a2;
            Matrix<Complex> u = matrix * (a6 + 1512.0 * a4 + 277200.0 * a2 + 8648640.0 * identity);
            Matrix<Complex> v = 56.0 * a6 + 25200.0 * a4 + 1995840.0 * a2 + 17297280.0 * identity;
            return SolvePade(u, v);
        }

        private static Matrix<Complex> Pade9(Matrix<Complex> matrix)
        {
            Matrix<Complex> identity = Matrix<Complex>.Build.DenseIdentity(matrix.RowCount);
            Matrix<Complex> a2 = matrix * matrix;
            Matrix<Complex> a4 = a2 * a2;
            Matrix<Complex> a6 = a4 * a2;
            Matrix<Complex> a8 = a4 * a4;
            Matrix<Complex> u = matrix * (a8 + 3960.0 * a6 + 2162160.0 * a4 + 302702400.0 * a2 + 8821612800.0 * identity);
            Matrix<Complex> v = 90.0 * a8 + 110880.0 * a6 + 30270240.0 * a4 + 2075673600.0 * a2 + 17643225600.0 * identity;
            return SolvePade(u, v);
        }

        private static Matrix<Complex> Pade13(Matrix<Complex> matrix)
        {
            Matrix<Complex> identity = Matrix<Complex>.Build.DenseIdentity(matrix.RowCount);
            Matrix<Complex> a2 = matrix * matrix;
            Matrix<Complex> a4 = a2 * a2;
            Matrix<Complex> a6 = a4 * a2;
            Matrix<Complex> u = matrix *
                (a6 * (a6 + 16380.0 * a4 + 40840800.0 * a2) +
                 33522128640.0 * a6 + 10559470521600.0 * a4 +
                 1187353796428800.0 * a2 + 32382376266240000.0 * identity);
            Matrix<Complex> v =
                a6 * (182.0 * a6 + 960960.0 * a4 + 1323241920.0 * a2) +
                670442572800.0 * a6 + 129060195264000.0 * a4 +
                7771770303897600.0 * a2 + 64764752532480000.0 * identity;
            return SolvePade(u, v);
        }

        private static Matrix<Complex> SolvePade(Matrix<Complex> u, Matrix<Complex> v)
        {
            return (v - u).LU().Solve(v + u);
        }

        public static bool IsDiagonal(this Matrix<Complex> m)
        {
            if (m.RowCount != m.ColumnCount)
                throw new ArgumentException("Matrix should be square");

            for (int i = 0; i < m.ColumnCount; i++)
                for (int j = i + 1; j < m.ColumnCount; j++)
                {
                    if (m[i, j] != 0.0) return false;
                    if (m[j, i] != 0.0) return false;
                }

            return true;
        }
    }
}
