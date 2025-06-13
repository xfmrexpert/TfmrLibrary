﻿// SPDX-License-Identifier: MIT

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics;
using System.Numerics;

namespace MatrixExponential
{
    public static class Extensions
    {
        public static Matrix<Complex> Exponential(this Matrix<Complex> m)
        {
            if (m.RowCount != m.ColumnCount)
                throw new ArgumentException("Matrix should be square");

            Matrix<Complex> exp_m = null;

            // if m is diagonal, then matrix exponential is equal to pointwise exponential
            if (m.IsDiagonal())
                exp_m = Matrix<Complex>.Build.DenseOfDiagonalVector(m.Diagonal().PointwiseExp());
            else
            {
                // unfortunately m is not diagonal
                // so let's try to diagonalize it
                bool diagonalization_failed = !m.IsSymmetric();
                if (!diagonalization_failed) // if m is symmetric
                {
                    try
                    {
                        var evd = m.Evd();
                        Matrix<Complex> expD = Matrix<Complex>.Build.DenseOfDiagonalVector(evd.D.Diagonal().PointwiseExp());
                        exp_m = evd.EigenVectors * expD * (evd.EigenVectors.Inverse());
                    }
                    catch
                    {
                        diagonalization_failed = true;
                    }
                }

                if (diagonalization_failed) // get here if m is not symmetric or if diagnolization fails
                {
                    // last hope: Padé approximation method
                    // details could be found in 
                    // M.Arioli, B.Codenotti, C.Fassino The Padé method for computing the matrix exponential // Linear Algebra and its Applications, 1996, V. 240, P. 111-130
                    // https://www.sciencedirect.com/science/article/pii/0024379594001901

                    int p = 5; // order of Padé 

                    // high matrix norm may result in high roundoff errors, 
                    // so first we have to find normalizing coefficient such that || m / norm_coeff || < 0.5
                    // to reduce the following computations we set it norm_coeff = 2^k

                    double k = 0;
                    double mNorm = m.L1Norm();
                    if (mNorm > 0.5)
                    {
                        k = Math.Ceiling(Math.Log(mNorm) / Math.Log(2.0));
                        m = m / Math.Pow(2.0, k);
                    }

                    Matrix<Complex> N = Matrix<Complex>.Build.DenseIdentity(m.RowCount);
                    Matrix<Complex> D = Matrix<Complex>.Build.DenseIdentity(m.RowCount);
                    Matrix<Complex> m_pow_j = m;

                    int q = p; // here we use symmetric approximation, but in general p may not be equal to q.
                    for (int j = 1; j <= Math.Max(p, q); j++)
                    {
                        if (j > 1)
                            m_pow_j = m_pow_j * m;
                        if (j <= p)
                            N = N + SpecialFunctions.Factorial(p + q - j) * SpecialFunctions.Factorial(p) / SpecialFunctions.Factorial(p + q) / SpecialFunctions.Factorial(j) / SpecialFunctions.Factorial(p - j) * m_pow_j;
                        if (j <= q)
                            D = D + Math.Pow(-1.0, j) * SpecialFunctions.Factorial(p + q - j) * SpecialFunctions.Factorial(q) / SpecialFunctions.Factorial(p + q) / SpecialFunctions.Factorial(j) / SpecialFunctions.Factorial(q - j) * m_pow_j;
                    }

                    // calculate inv(D)*N with LU decomposition
                    exp_m = D.LU().Solve(N);

                    // denormalize if need
                    if (k > 0)
                    {
                        for (int i = 0; i < k; i++)
                        {
                            exp_m = exp_m * exp_m;
                        }
                    }
                }
            }
            return exp_m;
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
