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
        {   // 子集构造算法
            int id = 0;

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
                        qNode = new DfaDigraphNode(id++) { IsAcceptable = qSet.Contains(nfa.Tail) };

                        if (head == null) head = qNode;
                        dict[qSet] = qNode;
                    }
                    var isContain = !setQ.AddEx(ref tSet);

                    DfaDigraphNode tNode;
                    if (!dict.TryGetValue(tSet, out tNode))
                    {
                        tNode = new DfaDigraphNode(id++) { IsAcceptable = tSet.Contains(nfa.Tail) };

                        dict[tSet] = tNode;
                    }

                    qNode.Edges[i] = tNode;

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

        public static void PrintDigraph(DfaDigraphNode dig, bool showNfa)
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
                Console.WriteLine(n.PrintString(showNfa));

                foreach (var e in n.Edges)
                {
                    queue.Enqueue(e.Value);
                }
            }
        }

        /// <summary>
        /// 最小化
        /// </summary>
        /// <param name="headNode">对应NFA中的头节点</param>
        /// <param name="tailNode">对应NFA中的尾节点</param>
        /// <returns></returns>
        public static DfaDigraphNode Minimize(this DfaDigraphNode node)
        {
            var sets = SplitByTail(node);
            var sets2 = SplitByState(sets);
            return CreateFrom(sets2);
        }

        /// <summary>
        /// 将Hopcroft算法得出的子集生成DFA
        /// </summary>
        static DfaDigraphNode CreateFrom(HashSet<HashSet<DfaDigraphNode>> setQ)
        {
            int id = 0;
            Dictionary<HashSet<DfaDigraphNode>, DfaDigraphNode> tableSet2Dfa = new(HashSetComparer<DfaDigraphNode>.Instance);

            foreach (var setQelem in setQ)
            {
                // 为每一个集合创建节点

                //var nfaNodes = setQelem.SelectMany(n => n.NfaElement);
                DfaDigraphNode node = new(id++) { IsAcceptable = setQelem.Any(n => n.IsAcceptable) };

                tableSet2Dfa[setQelem] = node;
            }


            foreach (var setQelem in setQ)
            {
                var beforeNode = tableSet2Dfa[setQelem];

                // 根据集合为每个节点添加边
                var edges = setQelem.SelectMany(n => n.Edges).Distinct(); // 查询集合原dfa节点所有的原边
                // 遍历原边，并根据原边的终点dfa节点找到对应的集合
                foreach (var (value, node) in edges)
                {
                    //所有办好原本终点的集合
                    HashSet<HashSet<DfaDigraphNode>> setQsub = new(setQ.Where(sub => sub.Contains(node)));
                    foreach (var setQsubElem in setQsub)
                    {
                        var afterNode = tableSet2Dfa[setQsubElem];
                        beforeNode.Edges[value] = afterNode;
                    }
                }
            }

            //return tableSet2Dfa.Values.Single(n => n.NfaElement.Contains(head));
            var allNodes = tableSet2Dfa.Values.ToHashSet();
            var shiftNodes = tableSet2Dfa.Values.SelectMany(n => n.Edges.Select(e => e.Value)).ToHashSet();
            return allNodes.Except(shiftNodes).Single();
        }

        /// <summary>
        /// 按等效状态划分子集
        /// </summary>
        static HashSet<HashSet<DfaDigraphNode>> SplitByState(HashSet<HashSet<DfaDigraphNode>> set)
        {
            var W = set.Select(s => s.ToHashSet()).ToHashSet(HashSetComparer<DfaDigraphNode>.Instance);
            bool hasSplited = false;
            do
            {
                hasSplited = false;
                foreach (var A in W)
                {
                    if (A.Count == 1)
                        continue;

                    var superA = A.GroupBy(n => n.Edges, EdgesComparer.Instance).ToArray();
                    if (superA.Length > 1) // 按转换条件分割
                    {
                        W.Remove(A);
                        W.UnionWith(superA.Select(g => g.ToHashSet()));
                        hasSplited = true;
                        break;
                    }

                    superA = A.GroupBy(n => n.Edges, new EdgesMoveComparer(W)).ToArray();
                    if (superA.Count() > 1) // 按转换后状态的集合分割
                    {
                        W.Remove(A);
                        W.UnionWith(superA.Select(g => g.ToHashSet()));
                        hasSplited = true;
                        break;
                    }
                }
            }
            while (hasSplited);

            return W;
        }

        class HashSetComparer<T> : IEqualityComparer<HashSet<T>>
        {
            public static HashSetComparer<T> Instance { get; private set; } = new();

            bool IEqualityComparer<HashSet<T>>.Equals(HashSet<T>? x, HashSet<T>? y)
            {
                return x.SetEquals(y);
            }

            int IEqualityComparer<HashSet<T>>.GetHashCode(HashSet<T> obj) => 0;
        }

        class EdgesMoveComparer : IEqualityComparer<Dictionary<int, DfaDigraphNode>>
        {
            HashSet<HashSet<DfaDigraphNode>> superSet;
            public EdgesMoveComparer(HashSet<HashSet<DfaDigraphNode>> superSet) => this.superSet = superSet;

            private HashSet<DfaDigraphNode> GetSet(DfaDigraphNode node)
            {
                return superSet.First(s => s.Contains(node));
            }

            bool IEqualityComparer<Dictionary<int, DfaDigraphNode>>.Equals(Dictionary<int, DfaDigraphNode>? x, Dictionary<int, DfaDigraphNode>? y)
            {
                var xSet = x.Values.Select(v => GetSet(v));
                var ySet = y.Values.Select(v => GetSet(v));
                return xSet.ToHashSet().SetEquals(ySet.ToHashSet());
            }

            int IEqualityComparer<Dictionary<int, DfaDigraphNode>>.GetHashCode(Dictionary<int, DfaDigraphNode> obj) => 0;
        }

        class EdgesComparer : IEqualityComparer<Dictionary<int, DfaDigraphNode>>
        {
            public static EdgesComparer Instance { get; private set; } = new();

            bool IEqualityComparer<Dictionary<int, DfaDigraphNode>>.Equals(Dictionary<int, DfaDigraphNode>? x, Dictionary<int, DfaDigraphNode>? y)
            {
                return x.Keys.ToHashSet().SetEquals(y.Keys.ToHashSet());
            }

            int IEqualityComparer<Dictionary<int, DfaDigraphNode>>.GetHashCode(Dictionary<int, DfaDigraphNode> obj) => 0;
        }

        /// <summary>
        /// 将DFA的所有元素，按接受状态和非接受状态，划分子集
        /// </summary>
        static HashSet<HashSet<DfaDigraphNode>> SplitByTail(DfaDigraphNode digraph)
        {
            HashSet<DfaDigraphNode> N = new(), A = new(), visited = new();
            Queue<DfaDigraphNode> queue = new();
            var node = digraph;
            queue.Enqueue(node);
            while (queue.Count > 0)
            {
                node = queue.Dequeue();

                visited.Add(node);
                if (node.IsAcceptable) A.Add(node);
                else N.Add(node);

                foreach (var edge in node.Edges)
                {
                    if (!visited.Contains(edge.Value))
                        queue.Enqueue(edge.Value);
                }
            }

            return new() { N, A };
        }
    }
}
