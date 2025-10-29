using System.Xml;

namespace TfmrLib;

public interface IConnectedEntity
{
    IReadOnlyList<Node> Ports { get; }

    void RepointPort(Node oldNode, Node newNode);
}