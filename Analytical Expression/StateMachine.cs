using System.Collections.Generic;
using System.Linq;

namespace Analytical_Expression
{
    public class StateMachine
    {
        public int State { get; private set; } = 0;

        private bool IsAcceptable
        {
            get
            {
                return (typeTable[State] & DfaNodeType.Acceptable) == DfaNodeType.Acceptable;
            }
        }

        List<Dictionary<int, int>> jumpTable = new();
        List<DfaNodeType> typeTable = new();
        public int Count { get; private set; } = 0;
        private int jumpCount = 0;

        public bool IsWork { get; private set; } = true;

        public string Name { get; set; }

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
                typeTable.Add(node.Type);
                indexTable[node] = i;
            }

            for (int i = 0; i < lstNode.Count; i++)
            {
                var node = lstNode[i];
                jumpTable.Add(node.Edges.ToDictionary(e => e.Key, e => indexTable[e.Value]));
            }
        }

        public bool Jump(int c)
        {
            if (!IsWork)
                return false;

            if (jumpTable[State].TryGetValue(c, out var state))
            {
                State = state;
                jumpCount += 1;

                if (IsAcceptable)
                {
                    Count += jumpCount;
                    jumpCount = 0;
                }

                

                return true;
            }
            else {
                IsWork = false;
                jumpCount = 0;
                return false; 
            }
        }

        

        public void Reset()
        {
            IsWork = true;
            State = 0;
            Count = 0;
            jumpCount = 0;
        }
    }

}
