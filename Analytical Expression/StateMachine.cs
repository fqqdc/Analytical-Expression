using System.Collections.Generic;
using System.Linq;

namespace Analytical_Expression
{
    public class StateMachine
    {
        public int State { get; private set; } = 0;

        public bool Acceptable
        {
            get
            {
                return (TypeTable[State] & DfaNodeType.Acceptable) == DfaNodeType.Acceptable;
            }
        }

        List<Dictionary<int, int>> JumpTable = new();
        List<DfaNodeType> TypeTable = new();

        public StateMachine(DfaDigraphNode dfa)
        {
            HashSet<DfaDigraphNode> visited = new();
            Stack<DfaDigraphNode> queue = new();
            List<DfaDigraphNode> lstNode = new();
            Dictionary<DfaDigraphNode, int> indexTable = new();

            queue.Push(dfa);
            while (queue.Count > 0)
            {
                var n = queue.Pop();
                visited.Add(n);
                lstNode.Add(n);
                n.Edges.Select(e => e.Value)
                    .Distinct()
                    .Where(e => !visited.Contains(e))
                    .ToList()
                    .ForEach(e => queue.Push(e));
            }

            for (int i = 0; i < lstNode.Count; i++)
            {
                var node = lstNode[i];
                TypeTable.Add(node.Type);
                indexTable[node] = i;
            }

            for (int i = 0; i < lstNode.Count; i++)
            {
                var node = lstNode[i];
                JumpTable.Add(node.Edges.ToDictionary(e => e.Key, e => indexTable[e.Value]));
            }
        }

        public bool Jump(int c)
        {
            if (JumpTable[State].TryGetValue(c, out var state))
            {
                State = state;
                return true;
            }
            return false;
        }
    }

}
