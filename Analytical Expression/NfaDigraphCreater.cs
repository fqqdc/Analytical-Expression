using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public static class NfaDigraphCreater
    {
        public const int EPSILON = -1;
        public static int IncNodeID { get; set; } = 0;

        //{fst}{snd}
        public static NfaDigraph Join(this NfaDigraph fst, NfaDigraph snd)
        {
            int id = 0;
            fst = fst.Clone(ref id);
            snd = snd.Clone(ref id);

            var n0 = fst.Tail;
            n0.Edges.Add((EPSILON, snd.Head));

            return new NfaDigraph { Head = fst.Head, Tail = snd.Tail };
        }

        //{fst}|{snd}
        public static NfaDigraph Union(this NfaDigraph fst, NfaDigraph snd)
        {
            var head = new NfaDigraphNode { ID = IncNodeID++ };
            head.Edges.Add((EPSILON, fst.Head));
            head.Edges.Add((EPSILON, snd.Head));
            var tail = new NfaDigraphNode { ID = IncNodeID++ };
            fst.Tail.Edges.Add((EPSILON, tail));
            snd.Tail.Edges.Add((EPSILON, tail));

            return new NfaDigraph { Head = head, Tail = tail };
        }

        //{dig}*
        public static NfaDigraph Closure(this NfaDigraph dig)
        {
            var head = new NfaDigraphNode { ID = IncNodeID++ };
            var tail = new NfaDigraphNode { ID = IncNodeID++ };
            head.Edges.Add((EPSILON, dig.Head));
            head.Edges.Add((EPSILON, tail));
            dig.Tail.Edges.Add((EPSILON, dig.Head));
            dig.Tail.Edges.Add((EPSILON, tail));

            return new NfaDigraph { Head = head, Tail = tail };
        }

        //[{from}-{to}]
        public static NfaDigraph CreateCharacterRange(char from, char to)
        {
            Debug.Assert(from <= to);
            NfaDigraph dig = CreateSingleCharacter(from);
            for (char c = (char)(from + 1); c <= to; c++)
            {
                var newDig = CreateSingleCharacter(c);
                dig = dig.Union(newDig);
            }

            return dig;
        }

        //{c}
        public static NfaDigraph CreateSingleCharacter(char c)
        {
            var head = new NfaDigraphNode { ID = IncNodeID++ };
            var tail = new NfaDigraphNode { ID = IncNodeID++ };
            head.Edges.Add((c, tail));

            return new NfaDigraph { Head = head, Tail = tail };
        }

        public static void PrintDigraph(this NfaDigraph dig, bool showCode = false)
        {
            HashSet<NfaDigraphNode> setVisited = new();
            Queue<NfaDigraphNode> queue = new();
            queue.Enqueue(dig.Head);

            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                if (setVisited.Contains(n))
                {
                    continue;
                }

                setVisited.Add(n);
                Console.WriteLine(n.PrintString(showCode));

                foreach (var e in n.Edges)
                {
                    queue.Enqueue(e.Node);
                }
            }
        }

        public static HashSet<NfaDigraphNode> EpsilonClosure(this NfaDigraphNode node)
        {
            HashSet<NfaDigraphNode> visited = new();
            HashSet<NfaDigraphNode> closure = new();
            Queue<NfaDigraphNode> queue = new();

            queue.Enqueue(node);
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                closure.Add(n);
                visited.Add(n);

                foreach (var edge in n.Edges)
                {
                    if (edge.Value == NfaDigraphCreater.EPSILON
                        && !visited.Contains(edge.Node))
                    {
                        queue.Enqueue(edge.Node);
                    }
                }
            }
            return closure;
        }

        public static NfaDigraph ReorderID(this NfaDigraph dig)
        {
            Queue<NfaDigraphNode> queue = new();
            HashSet<NfaDigraphNode> visited = new();
            int incID = 0;
            queue.Enqueue(dig.Head);
            visited.Add(dig.Head);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                node.ID = incID++;

                foreach (var edge in node.Edges)
                {
                    if (!visited.Contains(edge.Node))
                    {
                        visited.Add(edge.Node);
                        queue.Enqueue(edge.Node);
                    }
                }
            }
            return dig;
        }

        public static NfaDigraph Clone(this NfaDigraph old, ref int index)
        {
            Dictionary<NfaDigraphNode, NfaDigraphNode> tableOld2New = new();

            Queue<NfaDigraphNode> queue = new();
            queue.Enqueue(old.Head);
            while (queue.Count > 0)
            {
                var oldNode = queue.Dequeue();
                if (!tableOld2New.TryGetValue(oldNode, out NfaDigraphNode newNode))
                {
                    newNode = new() { ID = index++ };
                    tableOld2New[oldNode] = newNode;

                    foreach (var n in oldNode.Edges.Select(e => e.Node).Distinct())
                    {
                        queue.Enqueue(n);
                    }
                }
            }

            queue = new();
            queue.Enqueue(old.Head);
            HashSet<NfaDigraphNode> visited = new();
            while (queue.Count > 0)
            {
                var oldNode = queue.Dequeue();
                visited.Add(oldNode);

                foreach (var (value, oldOpNode) in oldNode.Edges)
                {
                    var newNode = tableOld2New[oldNode];
                    newNode.Edges.Add((value, tableOld2New[oldOpNode]));

                    if (!visited.Contains(oldOpNode))
                    {
                        queue.Enqueue(oldOpNode);
                    }
                }
            }

            return new() { Head = tableOld2New[old.Head], Tail = tableOld2New[old.Tail] };
        }
    }
}
