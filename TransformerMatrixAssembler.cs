using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinAlg = MathNet.Numerics.LinearAlgebra;
using Vector_d = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace TfmrLib
{
    internal class TransformerMatrixAssembler
    {
        private readonly Transformer _transformer;

        public TransformerMatrixAssembler(Transformer tfmr)
        {
            _transformer = tfmr ?? throw new ArgumentNullException(nameof(tfmr), "Transformer cannot be null");
        }

        public LinAlg.Matrix<double> Assemble()
        {
            LinAlg.Matrix<double> _matrix = LinAlg.Matrix<double>.Build.Dense(_transformer.NumConductors, _transformer.NumConductors);

            if (_transformer.Windings.Count == 0)
            {
                throw new InvalidOperationException("Transformer must have at least one winding to assemble the matrix.");
            }
            // Initialize the matrix with zeros
            _matrix.Clear();
            // Loop through each winding and its segments
            foreach (var winding in _transformer.Windings)
            {
                foreach (var segment in winding.Segments)
                {
                    if (segment.Geometry == null)
                    {
                        continue; // Skip segments without geometry
                    }
                    var segmentGeometry = segment.Geometry;
                    // Update the matrix based on the segment's geometry
                    for (int turn = 0; turn < segmentGeometry.NumTurns; turn++)
                    {
                        for (int cond = 0; cond < segmentGeometry.NumParallelConductors; cond++)
                        {
                            //_matrix[turn, cond] += CalculateContribution(radius, turn, cond);
                        }
                    }
                }
            }
            return _matrix;
        }

    }
}
