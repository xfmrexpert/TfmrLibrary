using System.Collections.Generic;

namespace TfmrLib
{
    public class Branch : IConnectedEntity
    {
        public WindingSegment ParentSegment { get; }
        public int Id { get; }
        
        public int StartTurnIndex { get; }
        public int NumTurns { get; }

        private Node _startNode;
        private Node _endNode;

        public Node StartNode => _startNode;
        public Node EndNode => _endNode;

        public IReadOnlyList<Node> Ports => new[] { _startNode, _endNode };

        public Branch(WindingSegment parent, int id, Node startNode, Node endNode, int startTurn, int numTurns)
        {
            ParentSegment = parent;
            Id = id;
            _startNode = startNode;
            _endNode = endNode;
            StartTurnIndex = startTurn;
            NumTurns = numTurns;
        }

        public void RepointPort(Node oldNode, Node newNode)
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
                throw new System.Exception($"Branch {Id} of {ParentSegment.Label}: Attempted to repoint a node that isn't attached.");
            }
        }

        public override string ToString()
        {
            return $"{ParentSegment.Label}.Branch[{Id}] (Turns {StartTurnIndex}-{StartTurnIndex + NumTurns})";
        }
    }
}
