namespace TfmrLib.FEM
{
    public class Excitation : INamed
    {
        public string Name { get; init; }
        public string TerminalName { get; set; }
        public double Magnitude { get; set; }
        public double Phase { get; set; }
    }
}
