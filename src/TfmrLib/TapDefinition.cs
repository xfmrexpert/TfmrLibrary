namespace TfmrLib
{
    public class TapDefinition
    {
        public string Label { get; set; }
        public int TurnNumber { get; set; }

        public Node? Node { get; internal set; }
    }
}
