using MathNet.Numerics.Distributions;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using LinAlg = MathNet.Numerics.LinearAlgebra;

namespace TfmrLib
{
    using Matrix_d = LinAlg.Matrix<double>;
    using Matrix_c = LinAlg.Matrix<Complex>;
    using Vector_d = LinAlg.Vector<double>;
    using Vector_c = LinAlg.Vector<Complex>;

    public abstract class FreqResponseModel
    {
        static protected MatrixBuilder<double> M_d = Matrix_d.Build;
        static protected MatrixBuilder<Complex> M_c = Matrix_c.Build;
        static protected VectorBuilder<double> V_d = Vector_d.Build;
        static protected VectorBuilder<Complex> V_c = Vector_c.Build;

        public Transformer Tfmr { get; set; }
        public IRLCMatrixCalculator MatrixCalculator { get; set; }
        public double MinFreq { get; set; }
        public double MaxFreq { get; set; }
        public int NumSteps { get; set; }

        public FreqResponseModel(Transformer tfmr, IRLCMatrixCalculator matrixCalculator, double minFreq = 10e3, double maxFreq = 100e3, int numSteps = 1000)
        {
            Tfmr = tfmr;
            MatrixCalculator = matrixCalculator;
            MinFreq = minFreq;
            MaxFreq = maxFreq;
            NumSteps = numSteps;
            Control.UseNativeMKL();
        }

        protected abstract void Initialize();

        // Should return complex vector of calculated voltage (magnitude and phase) at each turn
        public abstract (Complex Z_term, Vector_c V_EndOfTurn) CalcResponseAtFreq(double f);

        // Impedances passed here are per unit length
        // C matrix should be in self/mutual form, not Maxwell form
        public (Complex[], List<Complex[]>) CalcResponse(IProgress<int> progress = null)
        {
            Initialize();

            // Create vector of frequencies
            var freqs = Generate.LogSpaced(NumSteps, Math.Log10(MinFreq), Math.Log10(MaxFreq));
            int totalSteps = freqs.Count();

            Vector_c[] V_turn = new Vector_c[totalSteps];
            Complex[] Z = new Complex[totalSteps];

            // Counter for progress reporting.
            int completed = 0;

            // Choose a batch size based on the granularity of your work.
            // You can tune 'batchSize' to find the optimal balance.
            int batchSize = 10;  // For example, update progress every 10 iterations.

            // Create a partitioner that yields ranges (tuples with start and exclusive end indices).
            var rangePartitioner = System.Collections.Concurrent.Partitioner.Create(0, totalSteps, batchSize);

            // Parallelize over the frequency indices.
            //System.Threading.Tasks.Parallel.For(0, totalSteps, i =>
            //{
            for (int i = 0; i < totalSteps; i++)
            {
                var f = freqs[i];
                // Calculate the response at frequency 'f'
                var (Z_term, V_endofturn_at_freq) = CalcResponseAtFreq(f);

                // Save the results in the preallocated arrays.
                V_turn[i] = V_endofturn_at_freq;
                Z[i] = Z_term;

                // Update progress safely.
                int done = System.Threading.Interlocked.Increment(ref completed);
                progress?.Report((int)((done) / (double)totalSteps * 100));
            }
            //);

            // Use Parallel.ForEach to process batches in parallel.
            //System.Threading.Tasks.Parallel.ForEach(rangePartitioner, range =>
            //{
            //    // Process each batch.
            //    for (int i = range.Item1; i < range.Item2; i++)
            //    {
            //        var f = freqs[i];
            //        // Calculate the response at frequency f.
            //        var (Z_term, V_endofturn_at_freq) = CalcResponseAtFreq(f);
            //        V_turn[i] = V_endofturn_at_freq;
            //        Z[i] = Z_term;
            //    }
            //    // After processing the entire batch, update the completed counter.
            //    int batchCount = range.Item2 - range.Item1;
            //    int done = System.Threading.Interlocked.Add(ref completed, batchCount);
            //    progress?.Report((int)(done / (double)totalSteps * 100));
            //});

            var V_response = new List<Complex[]>();
            for (int cond = 0; cond < Tfmr.NumConductors; cond++)
            {
                V_response.Add(new Complex[NumSteps]);
            }

            // Enumerate through raw turn responses (vector of Complex Gain for each turn) at each frequency
            for (int f = 0; f < NumSteps; f++)
            {
                int offset = 0;
                // Enumerate through each winding
                foreach (var wdg in Tfmr.Windings)
                {
                    foreach (var seg in wdg.Segments)
                    {
                        var seggeo = seg.Geometry;
                        for (int t = 0; t < seggeo.NumTurns * seggeo.NumParallelConductors; t++)
                        {
                            //Translate to dB
                            V_response[offset + t][f] = V_turn[f][offset + t];

                        }
                        offset += seggeo.NumTurns*seggeo.NumParallelConductors;
                    }
                    
                }
            }

            return (Z, V_response);
        }
    }
}
