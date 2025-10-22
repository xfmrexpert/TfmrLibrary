using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

        public LinAlg.Matrix<double> Assemble(Func<Transformer, LinAlg.Matrix<double>> func)
        {
            LinAlg.Matrix<double> _matrix = LinAlg.Matrix<double>.Build.Dense(_transformer.NumConductors, _transformer.NumConductors);

            if (_transformer.Windings.Count == 0)
            {
                throw new InvalidOperationException("Transformer must have at least one winding to assemble the matrix.");
            }
            // Initialize the matrix with zeros
            _matrix.Clear();
            // Loop through each winding and its segments
            int startIdx = 0;
            foreach (var winding in _transformer.Windings)
            {
                foreach (var segment in winding.Segments)
                {
                    
                }
            }
            return _matrix;
        }

    }
}
