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
            var q0Set = n0.EpsilonClosure();
            setQ.Add(q0Set);
            var workList = new Queue<HashSet<NfaDigraphNode>>();
            workList.Enqueue(q0Set);
            while (workList.Count > 0)
            {
                var qSet = workList.Dequeue();
                for (int i = Constant.MinimumCharacter; i < Constant.MaximumCharacter + 1; i++)
                {
                    char c = (char)i;
                    var deltaSet = GetDeltaSet(qSet, c); // 通过c到达子集q终点的集合
                    if (deltaSet.Count == 0) continue;

                    var tSet = GetEpsilonClosureSet(deltaSet, ecSetCache); // 集合的e闭包

                    DfaDigraphNode qNode;
                    if (!dict.TryGetValue(qSet, out qNode))
                    {
                        qNode = new DfaDigraphNode { NfaElement = qSet };

                        if (head == null) head = qNode;
                        dict[qSet] = qNode;
                    }
                    var isContain = !setQ.AddEx(ref tSet);

                    DfaDigraphNode tNode;
                    if (!dict.TryGetValue(tSet, out tNode))
                    {
                        tNode = new DfaDigraphNode { NfaElement = tSet };

                        dict[tSet] = tNode;
                    }

                    qNode.Edges.Add((i, tNode));

                    if (!isContain)
                        workList.Enqueue(tSet);
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

        /// <summary>
        /// 集合set中各元素经过c能到达的终点的集合
        /// </summary>
        private static HashSet<NfaDigraphNode> GetDeltaSet(HashSet<NfaDigraphNode> set, char c)
        {
            HashSet<NfaDigraphNode> returnSet = new();

            foreach (var n in set)
            {
                foreach (var edge in n.Edges)
                {
                    if (edge.Value == c)
                    {
                        returnSet.Add(edge.Node);
                    }
                }
            }

            return returnSet;
        }

        /// <summary>
        /// 集合set中各个元素e闭包并集
        /// </summary>
        /// <param name="cache">存储元素e闭包</param>
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
