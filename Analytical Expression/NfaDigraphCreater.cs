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

        //{fst}{snd}
        public static NfaDigraph Join(this NfaDigraph fst, NfaDigraph snd)
        {
            var n0 = fst.Tail;
            n0.Edges.Add((EPSILON, snd.Head));

            return new NfaDigraph { Head = fst.Head, Tail = snd.Tail };
        }

        //{fst}|{snd}
        public static NfaDigraph Union(this NfaDigraph fst, NfaDigraph snd)
        {
            var head = new NfaDigraphNode();
            head.Edges.Add((EPSILON, fst.Head));
            head.Edges.Add((EPSILON, snd.Head));
            var tail = new NfaDigraphNode();
            fst.Tail.Edges.Add((EPSILON, tail));
            snd.Tail.Edges.Add((EPSILON, tail));

            return new NfaDigraph { Head = head, Tail = tail };
        }

        //{dig}*
        public static NfaDigraph Closure(this NfaDigraph dig)
        {
            var head = new NfaDigraphNode();
            var tail = new NfaDigraphNode();
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
                dig = dig.Join(newDig);
            }

            return dig;
        }

        //{c}
        public static NfaDigraph CreateSingleCharacter(char c)
        {
            var head = new NfaDigraphNode();
            var tail = new NfaDigraphNode();
            head.Edges.Add((c, tail));

            return new NfaDigraph { Head = head, Tail = tail };
        }

        public static void PrintDigraph(this NfaDigraph dig)
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
                Console.WriteLine(n);

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
    }
}
