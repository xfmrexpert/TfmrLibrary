using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib.FEM
{
    public abstract record FrequencySpec
    {
        public sealed record Scalar(double Value) : FrequencySpec;

        public sealed record Sweep(
            FrequencyScale Scale,
            double Start,
            double Stop,
            int Points) : FrequencySpec;
    }

    public enum FrequencyScale
    {
        Linear,
        Log
    }

    public class Scenario : INamed
    {
        public string Name { get; init; }
        public List<Excitation> Excitations { get; set; }
        public FrequencySpec Frequency { get; set; }
    }
}
