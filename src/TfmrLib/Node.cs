namespace TfmrLib;

public class Node
{
    public int Id { get; }                 // creation id, stable
    public int Index { get; internal set; } = -1; // solver index assigned at Build()
    public string? Tag { get; }

    private readonly Graph _g;             // owner graph

    internal Node(Graph g, int id, string? tag)
    { _g = g; Id = id; Tag = tag; }

    /// Connect this node electrically to another (union)
    public Node ConnectTo(Node other)
        => _g.Union(this, other);
}
