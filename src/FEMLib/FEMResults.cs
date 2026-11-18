using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;

namespace FEMLib;

public class FEMResults
{
    public CouplingMatrixResults Coupling { get; private set; }
}

public class CouplingMatrixResults
{
    public Matrix<double> CapacitanceMatrix { get; private set; }
    public List<FrequencySample> Samples { get; private set; }
}

public class FrequencySample
{
    public double FrequencyHz { get; private set; }
    public Matrix<double> ResistanceMatrix { get; private set; }
    public Matrix<double> InductanceMatrix { get; private set; }
}