namespace TfmrLib;

/// <summary>
/// Base class for defining fixed electrical connections within a winding.
/// </summary>
public abstract class InternalConnection
{
    public abstract void Apply(Winding winding, Graph graph);

    protected List<WindingSegment> ResolveSegments(Winding winding, List<string> labels, List<int> indices, List<WindingSegment> directSegments)
    {
        var result = new List<WindingSegment>();
        
        // Explicit instances
        if (directSegments != null) result.AddRange(directSegments);

        // By Label
        if (labels != null)
        {
            foreach (var label in labels)
            {
                var seg = winding.Segments.FirstOrDefault(s => s.Label == label);
                if (seg != null) result.Add(seg);
                else throw new Exception($"InternalConnection could not find segment with Label '{label}'");
            }
        }

        // By Index
        if (indices != null)
        {
            foreach (var idx in indices)
            {
                if (idx >= 0 && idx < winding.Segments.Count) result.Add(winding.Segments[idx]);
                else throw new Exception($"InternalConnection index {idx} out of range.");
            }
        }
        
        // If nothing specified, maybe return all?
        if (result.Count == 0 && (labels == null || labels.Count == 0) && (indices == null || indices.Count == 0) && (directSegments == null || directSegments.Count == 0))
        {
             // For convenience, some connections might default to "All segments in order"
             // But let's leave that to the specific implementation
             return winding.Segments.ToList();
        }

        return result;
    }
}

/// <summary>
/// Connects a list of winding segments in series.
/// Default (no list provided) = All segments in Winding order.
/// </summary>
public class SeriesConnection : InternalConnection
{
    public List<WindingSegment> Segments { get; set; } = new();
    public List<string> SegmentLabels { get; set; } = new();
    
    public override void Apply(Winding winding, Graph graph)
    {
        var segmentsToConnect = ResolveSegments(winding, SegmentLabels, null, Segments);
        
        if (segmentsToConnect.Count < 2) return;

        // Connect end of segment 'i' to start of segment 'i+1'
        for (int i = 0; i < segmentsToConnect.Count - 1; i++)
        {
            var currentSegment = segmentsToConnect[i];
            var nextSegment = segmentsToConnect[i + 1];
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
    public List<string> SegmentLabels { get; set; } = new();

    public override void Apply(Winding winding, Graph graph)
    {
        var segmentsToConnect = ResolveSegments(winding, SegmentLabels, null, Segments);

        if (segmentsToConnect.Count < 2) return;

        var commonStart = segmentsToConnect[0].StartNode;
        var commonEnd = segmentsToConnect[0].EndNode;

        for (int i = 1; i < segmentsToConnect.Count; i++)
        {
            // Use Union to merge the nodes correctly
            graph.Union(segmentsToConnect[i].StartNode, commonStart);
            graph.Union(segmentsToConnect[i].EndNode, commonEnd);
        }
    }
}