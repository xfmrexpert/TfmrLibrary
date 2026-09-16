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

        public sealed record List(List<double> Frequencies) : FrequencySpec;
    }

    public enum FrequencyScale
    {
        Linear,
        Log
    }
}
