using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class Program
    {
        static void Main(string[] args)
        {
            //var nfa = NfaDigraphCreater.CreateSingleCharacter('b')
            //    .Union(NfaDigraphCreater.CreateSingleCharacter('c'))
            //    .Closure();
            var nfa = NfaDigraphCreater.CreateSingleCharacter('b');
            NfaDigraphCreater.PrintDigraph(nfa);

            DfaDigraphNode dfa = CreateFrom(nfa);
            PrintDigraph(dfa);
        }

        static void PrintDigraph(DfaDigraphNode dig)
        {
            HashSet<DfaDigraphNode> setVisited = new();
            Queue<DfaDigraphNode> queue = new();
            queue.Enqueue(dig);

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

        static DfaDigraphNode CreateFrom(NfaDigraph nfa)
        {
            Dictionary<HashSet<NfaDigraphNode>, DfaDigraphNode> dict = new();
            HashSet<HashSet<NfaDigraphNode>> Q = new();
            DfaDigraphNode? head = null;

            var n0 = nfa.Head;
            var q0 = n0.EpsilonClosure();
            Q.Add(q0);
            var workList = new Queue<HashSet<NfaDigraphNode>>();
            workList.Enqueue(q0);
            while (workList.Count > 0)
            {
                var q = workList.Dequeue();
                for (int i = 20; i < 127; i++)
                {
                    char c = (char)i;
                    var t = delta(q, c);
                    if (t.Count == 0) continue;
                    t = EpsilonClosure(t);

                    DfaDigraphNode qNode;
                    if (!dict.TryGetValue(q, out qNode))
                    {
                        qNode = new DfaDigraphNode { NfaElement = q };
                        if (head == null) head = qNode;
                        dict[q] = qNode;
                    }
                    var isContain = Add(Q, ref t);

                    DfaDigraphNode tNode;
                    if (!dict.TryGetValue(t, out tNode))
                    {
                        tNode = new DfaDigraphNode { NfaElement = t };
                        dict[t] = tNode;
                    }

                    qNode.Edges.Add((new(new int[] { i }), tNode));

                    if (!isContain)
                        workList.Enqueue(t);
                }
            }

            return head;
        }

        static bool Add(HashSet<HashSet<NfaDigraphNode>> container, ref HashSet<NfaDigraphNode> item)
        {
            foreach (var set in container)
            {
                if (set.SetEquals(item))
                {
                    item = set;
                    return false;
                }
            }
            container.Add(item);
            return true;
        }

        static HashSet<NfaDigraphNode> delta(HashSet<NfaDigraphNode> q, char c)
        {
            HashSet<NfaDigraphNode> set = new();

            foreach (var n in q)
            {
                foreach (var edge in n.Edges)
                {
                    if (edge.Value == c)
                    {
                        set.Add(edge.Node);
                    }
                }
            }

            return set;
        }

        static Dictionary<NfaDigraphNode, HashSet<NfaDigraphNode>> EpsilonClosureCache = new();
        static HashSet<NfaDigraphNode> EpsilonClosure(HashSet<NfaDigraphNode> set)
        {
            HashSet<NfaDigraphNode> newSet = new();

            foreach (var n in set)
            {
                HashSet<NfaDigraphNode> eClosureSet;
                if (!EpsilonClosureCache.TryGetValue(n, out eClosureSet))
                {
                    eClosureSet = n.EpsilonClosure();
                    EpsilonClosureCache[n] = eClosureSet;
                }
                newSet.UnionWith(eClosureSet);
            }

            return newSet;
        }

    }


    class DfaDigraphNode
    {
        static int number;

        public int ID { get; private set; }
        public DfaDigraphNode()
        {
            ID = Interlocked.Increment(ref number);
        }

        public HashSet<NfaDigraphNode> NfaElement { get; init; } = new();

        public HashSet<(HashSet<int> Value, DfaDigraphNode Node)> Edges { get; init; } = new();

        public override string ToString()
        {
            StringBuilder builder = new();
            builder.AppendLine($"\"dfa{ID} [{JoinNfaElement(NfaElement)}]\"");
            foreach (var e in Edges)
            {
                builder.AppendLine($"  --({JoinEdgeValue(e)})-->\"dfa{e.Node.ID}\"");
            }

            return builder.ToString();
        }

        private string JoinNfaElement(HashSet<NfaDigraphNode> elem)
        {
            StringBuilder builder = new();
            foreach (var n
                in elem)
            {
                if (builder.Length > 0)
                    builder.Append(",");
                builder.Append($"nfa{n.ID}");
            }

            return builder.ToString();
        }
        private string JoinEdgeValue((HashSet<int> Value, DfaDigraphNode Node) edge)
        {
            StringBuilder builder = new();
            foreach (var v in edge.Value)
            {
                if (builder.Length > 0)
                    builder.Append(",");
                builder.Append($"{v}[{ (v >= 0 && v <= 127 ? (char)v : "??") }]");
            }

            return builder.ToString();
        }
    }


}
