using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public static class DfaDigraphCreater
    {
        public static DfaDigraphNode CreateFrom(NfaDigraph nfa)
        {
            // 子集构造算法

            Dictionary<HashSet<NfaDigraphNode>, DfaDigraphNode> dict = new();
            Dictionary<NfaDigraphNode, HashSet<NfaDigraphNode>> ecSetCache = new();

            HashSet<HashSet<NfaDigraphNode>> setQ = new();
            DfaDigraphNode? head = null;

            var n0 = nfa.Head;
            var q0 = n0.EpsilonClosure();
            setQ.Add(q0);
            var workList = new Queue<HashSet<NfaDigraphNode>>();
            workList.Enqueue(q0);
            while (workList.Count > 0)
            {
                var q = workList.Dequeue();
                for (int i = 20; i < 127; i++)
                {
                    char c = (char)i;
                    var t = GetDeltaSet(q, c);
                    if (t.Count == 0) continue;
                    t = GetEpsilonClosureSet(t, ecSetCache);

                    DfaDigraphNode qNode;
                    if (!dict.TryGetValue(q, out qNode))
                    {
                        qNode = new DfaDigraphNode { NfaElement = q };
                        if (head == null) head = qNode;
                        dict[q] = qNode;
                    }
                    var isContain = setQ.AddEx(ref t);

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

        private static bool AddEx(this HashSet<HashSet<NfaDigraphNode>> container, ref HashSet<NfaDigraphNode> item)
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

        private static HashSet<NfaDigraphNode> GetDeltaSet(HashSet<NfaDigraphNode> q, char c)
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

        private static HashSet<NfaDigraphNode> GetEpsilonClosureSet(HashSet<NfaDigraphNode> set, Dictionary<NfaDigraphNode, HashSet<NfaDigraphNode>> cache)
        {
            HashSet<NfaDigraphNode> newSet = new();

            foreach (var n in set)
            {
                HashSet<NfaDigraphNode> eClosureSet;
                if (!cache.TryGetValue(n, out eClosureSet))
                {
                    eClosureSet = n.EpsilonClosure();
                    cache[n] = eClosureSet;
                }
                newSet.UnionWith(eClosureSet);
            }

            return newSet;
        }

        public static void PrintDigraph(DfaDigraphNode dig)
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
    }
}
