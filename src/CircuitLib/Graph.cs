using System.Collections.Generic;

namespace CircuitLib;

// Keeper of the Nodes
public class Graph
{
    private int _nextNodeId = 0;
    protected readonly HashSet<Node> _nodes = new();
    public HashSet<Node> Nodes
    {
        get => _nodes;
    }

    protected readonly List<IConnectedEntity> _entities = new();

    // node -> incident entities
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
        foreach (var t in e.Terminals)
            _touches[t].Add(e);
    }

    internal void OnEntityNodeChanged(IConnectedEntity e, Node oldNode, Node newNode)
    {
        if (ReferenceEquals(oldNode, newNode)) return;
        _touches[oldNode].Remove(e);
        _touches[newNode].Add(e);
    }

    public List<IConnectedEntity> GetBranchesForNode(Node n)
    {
        return [.. _touches[n]];
    }

    // Merge 'from' into 'to'. Returns the survivor (to).
    public Node Union(Node from, Node to)
    {
        // If they're the same object, we done
        if (ReferenceEquals(from, to)) return to;

        // Repoint all entity terminals touching 'from' to 'to'
        // Get list of all entities that the from node touches
        var inc = new List<IConnectedEntity>(_touches[from]);
        foreach (var e in inc)
        {
            // Get the terminals (connectable nodes) of the current current entity
            var terminals = e.Terminals;
            for (int i = 0; i < terminals.Count; i++)
                // If the terminal matches the from node, repoint to the to node
                if (ReferenceEquals(terminals[i], from))
                    e.RepointTerminal(terminals[i], to);
        }

        //Make from node go bye bye
        _touches.Remove(from);
        _nodes.Remove(from);
        return to;
    }

    // For convenience
    public Node Short(Node a, Node b) => a.ConnectTo(b);
}

