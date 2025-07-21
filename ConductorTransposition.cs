namespace TfmrLib
{
    public enum TranspositionType { None, Gamma, Epsilon, Hobart, Continuous }

    public class ConductorTransposition
    {
        public TranspositionType Type { get; set; }  // Gamma, Epsilon, Hobart, Continuous, None
        public List<double> AxialPositionsFraction { get; set; } = new(); // e.g., [0.33, 0.67]
    }
}