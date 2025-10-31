namespace TfmrLib;

// Keeper of the Nodes
public sealed class Graph
{
    private int _nextNodeId = 0;
    private readonly List<Node> _nodes = new();
    private readonly List<IConnectedEntity> _entities = new();

    // node → incident entities
    private readonly Dictionary<Node, HashSet<IConnectedEntity>> _touches = new();

    public Node CreateNode(string? tag = null)
    {
        var n = new Node(this, _nextNodeId++, tag);
        _nodes.Add(n);
        _touches[n] = new HashSet<IConnectedEntity>();
        return n;
    }

    public void Register(IConnectedEntity e)
    {
        _entities.Add(e);
        foreach (var p in e.Ports)
            _touches[p].Add(e);
    }

    internal void OnEntityPortChanged(IConnectedEntity e, Node oldNode, Node newNode)
    {
        if (ReferenceEquals(oldNode, newNode)) return;
        _touches[oldNode].Remove(e);
        _touches[newNode].Add(e);
    }

    /// Merge 'from' into 'to'. Returns the survivor (to).
    public Node Union(Node from, Node to)
    {
        if (ReferenceEquals(from, to)) return to;

        // Repoint all entity ports touching 'from' to 'to'
        var inc = new List<IConnectedEntity>(_touches[from]);
        foreach (var e in inc)
        {
            var ports = e.Ports;
            for (int i = 0; i < ports.Count; i++)
                if (ReferenceEquals(ports[i], from))
                    e.RepointPort(ports[i], to);
        }

        _touches.Remove(from);
        _nodes.Remove(from);
        return to;
    }

    // Optional: convenience
    public Node Short(Node a, Node b) => a.ConnectTo(b);
}

