using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

namespace CircuitLib;

public class Circuit : Graph
{
    public Circuit()
    { }

    public void GenerateImpedanceMatrix()
    {
        foreach (var branch in _entities)
        {

        }
    }

    public Matrix<int> GenerateIncidenceMatrix()
    {
        Matrix<int> incidence = Matrix<int>.Build.Dense(_entities.Count, _nodes.Count);
        foreach (var branch in _entities.OfType<Branch>())
        {
            incidence[branch.Id, branch.StartNode.Id] = 1;
            incidence[branch.Id, branch.EndNode.Id] = -1;
        }
        return incidence;
    }
}