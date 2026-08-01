using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinAlg = MathNet.Numerics.LinearAlgebra;

namespace TfmrLib
{
    public interface IRLCMatrixCalculator
    {
        LinAlg.Matrix<double> Calc_Cmatrix(Transformer tfmr);
        LinAlg.Matrix<double> Calc_Lmatrix(Transformer tfmr, FEM.FrequencySpec f);
        LinAlg.Matrix<double> Calc_Rmatrix(Transformer tfmr, FEM.FrequencySpec f);
    }

}
