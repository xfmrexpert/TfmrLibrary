namespace TfmrLib
{
    public class TapDefinition
    {
        public string Label { get; set; }
        public int TurnNumber { get; set; } // 1-based index (e.g. after turn 1) 

        public Node? Node { get; internal set; }
    }
}
