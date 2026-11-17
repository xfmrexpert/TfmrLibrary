using System.Collections.Generic;

namespace CircuitLib
{
    public class Branch : IConnectedEntity
    {
        public int Id { get; set; }

        private Node _startNode;
        private Node _endNode;

        public Node StartNode => _startNode;
        public Node EndNode => _endNode;

        public IReadOnlyList<Node> Terminals => new[] { _startNode, _endNode };

        public Branch(int id, Node startNode, Node endNode)
        {
            Id = id;
            _startNode = startNode;
            _endNode = endNode;
        }

        public void RepointTerminal(Node oldNode, Node newNode)
        {
            bool found = false;
            if (ReferenceEquals(_startNode, oldNode))
            {
                _startNode = newNode;
                found = true;
            }
            if (ReferenceEquals(_endNode, oldNode))
            {
                _endNode = newNode;
                found = true;
            }

            if (!found)
            {
                throw new System.Exception($"Branch {Id}: Attempted to repoint a node that isn't attached.");
            }
        }

        public override string ToString()
        {
            return $"Branch[{Id}";
        }
    }
}
