using System.Collections.Generic;

namespace CircuitLib;

public interface IConnectedEntity
{
    public int Id { get; }
    IReadOnlyList<Node> Terminals { get; }

    void RepointTerminal(Node oldNode, Node newNode);
}