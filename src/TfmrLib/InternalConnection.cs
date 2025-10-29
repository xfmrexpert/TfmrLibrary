namespace TfmrLib;

/// <summary>
/// Base class for defining fixed electrical connections within a transformer.
/// </summary>
public abstract class InternalConnection
{
    public abstract void Apply(Graph graph);
}

/// <summary>
/// Connects a list of winding segments in series.
/// </summary>
public class SeriesConnection : InternalConnection
{
    public List<WindingSegment> Segments { get; set; } = new();

    public override void Apply(Graph graph)
    {
        if (Segments == null || Segments.Count < 2) return;

        // Connect end of segment 'i' to start of segment 'i+1'
        for (int i = 0; i < Segments.Count - 1; i++)
        {
            var currentSegment = Segments[i];
            var nextSegment = Segments[i + 1];
            // Merge the 'next' start node into the 'current' end node.
            graph.Union(nextSegment.StartNode, currentSegment.EndNode);
        }
    }
}

/// <summary>
/// Connects a list of winding segments in parallel.
/// </summary>
public class ParallelConnection : InternalConnection
{
    public List<WindingSegment> Segments { get; set; } = new();

    public override void Apply(Graph graph)
    {
        if (Segments == null || Segments.Count < 2) return;

        var commonStart = Segments[0].StartNode;
        var commonEnd = Segments[0].EndNode;

        for (int i = 1; i < Segments.Count; i++)
        {
            // Use Union to merge the nodes correctly
            graph.Union(Segments[i].StartNode, commonStart);
            graph.Union(Segments[i].EndNode, commonEnd);
        }
    }
}