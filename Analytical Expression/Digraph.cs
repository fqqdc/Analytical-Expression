using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    internal class Digraph
    {
        private HashSet<int>[] adj;
        private int edgeCount = 0;
        public Digraph(int size)
        {
            adj = new HashSet<int>[size];
        }

        public int NodeCount
        {
            get => adj.Length;
        }

        public int EdgeCount
        {
            get => edgeCount;
        }

        public void AddEdge(int vNode, int wNode)
        {
            if (adj[vNode] == null)
                adj[vNode] = new();
            adj[vNode].Add(wNode);
            edgeCount++;
        }

        IEnumerable<int> GetEndNodes(int vNode)
        {
            return (adj[vNode] ?? Enumerable.Empty<int>()).AsEnumerable();
        }

        public Digraph Reverse
        {
            get
            {
                Digraph reverseDigraph = new(adj.Length);
                for (int i = 0; i < adj.Length; i++)
                {
                    foreach (int w in GetEndNodes(i))
                    {
                        reverseDigraph.AddEdge(w, i);
                    }
                }
                return reverseDigraph;
            }
        }
    }
}
