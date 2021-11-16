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
                        qNode = new DfaDigraphNode(id++) { NfaElement = qSet };

                        if (head == null) head = qNode;
                        dict[qSet] = qNode;
                    }
                    var isContain = !setQ.AddEx(ref tSet);

                    DfaDigraphNode tNode;
                    if (!dict.TryGetValue(tSet, out tNode))
                    {
                        tNode = new DfaDigraphNode(id++) { NfaElement = tSet };

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

        public static void PrintDigraph(DfaDigraphNode dig, string pre, bool showNfa)
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
                Console.WriteLine(n.PrintString(pre, showNfa));

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
        public static DfaDigraphNode Minimize(this DfaDigraphNode node, NfaDigraphNode headNode, NfaDigraphNode tailNode)
        {
            var sets = SplitByTail(node, tailNode);
            sets = HopcroftSplit(sets);
            var sets2 = HopcroftSplit2(sets);
            return CreateFrom(sets, headNode);
        }

        /// <summary>
        /// 将Hopcroft算法得出的子集生成DFA
        /// </summary>
        static DfaDigraphNode CreateFrom(HashSet<HashSet<DfaDigraphNode>> setQ, NfaDigraphNode head)
        {
            int id = 0;
            Dictionary<HashSet<DfaDigraphNode>, DfaDigraphNode> tableSet2Dfa = new(new HashSetEqualityComparer<DfaDigraphNode>());

            foreach (var setQelem in setQ)
            {
                // 为每一个集合创建节点
                DfaDigraphNode node = new(id++) { NfaElement = new(setQelem.SelectMany(n => n.NfaElement)) };
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

            return tableSet2Dfa.Values.Single(n => n.NfaElement.Contains(head));
        }

        static HashSet<HashSet<DfaDigraphNode>> HopcroftSplit2(HashSet<HashSet<DfaDigraphNode>> set)
        {
            //P := {F, Q \ F};
            //W := {F, Q \ F};
            //while (W is not empty) do
            //     choose and remove a set A from W
            //     for each c in Σ do
            //          let X be the set of states for which a transition on c leads to a state in A
            //          for each set Y in P for which X ∩ Y is nonempty and Y \ X is nonempty do
            //               replace Y in P by the two sets X ∩ Y and Y \ X
            //               if Y is in W
            //                    replace Y in W by the same two sets
            //               else
            //                    if |X ∩ Y| <= |Y \ X|
            //                         add X ∩ Y to W
            //                    else
            //                         add Y \ X to W
            //          end;
            //     end;
            //end;
            HashSet<HashSet<DfaDigraphNode>> P = new(new HashSetEqualityComparer<DfaDigraphNode>());
            HashSet<HashSet<DfaDigraphNode>> W = new(new HashSetEqualityComparer<DfaDigraphNode>());
            //P := {F, Q \ F};
            //W := {F, Q \ F};
            foreach (var subSet in set)
            {
                P.Add(new(subSet));
                W.Add(new(subSet));
            }
            while (W.Count > 0) //while (W is not empty) do
            {
                //choose and remove a set A from W
                var A = W.First();
                W.Remove(A);
                //for each c in Σ do
                for (int i = Constant.MinimumCharacter; i < Constant.MaximumCharacter + 1; i++)
                {
                    char c = (char)i;
                    //let X be the set of states for which a transition on c leads to a state in A
                    HashSet<DfaDigraphNode> X = new(A.SelectMany(n => n.Edges.Values));
                    if (X.Count == 0) continue;
                    //for each set Y in P for which X ∩ Y is nonempty and Y \ X is nonempty do
                    foreach (var Y in P.ToArray())
                    {
                        HashSet<DfaDigraphNode> intersectSet = new(X.Intersect(Y).Count());
                        HashSet<DfaDigraphNode> exceptSet = new(X.Intersect(Y).Count());                        
                        if (X.Intersect(Y).Count() != 0 && Y.Except(X).Count() != 0)
                        {
                            //replace Y in P by the two sets X ∩ Y and Y \ X
                            P.Remove(Y);
                            P.Add(intersectSet);
                            P.Add(exceptSet);

                            if (W.Contains(Y)) //if Y is in W
                            {
                                //replace Y in W by the same two sets
                                W.Remove(Y);
                                W.Add(intersectSet);
                                W.Add(exceptSet);
                            }
                            else
                            {
                                //if |X ∩ Y| <= |Y \ X|
                                if (intersectSet.Count <= exceptSet.Count)
                                {
                                    //add X ∩ Y to W
                                    W.Add(intersectSet);
                                }
                                else
                                {
                                    //add Y \ X to W
                                    W.Add(exceptSet);
                                }
                            }
                        }
                    }

                }
            }

            HashSet<HashSet<DfaDigraphNode>> ResultSet = new(new HashSetEqualityComparer<DfaDigraphNode>());
            ResultSet.Union(P);
            ResultSet.Union(W);

            return ResultSet;
        }

        /// <summary>
        /// Hopcroft算法 分割多个子集
        /// </summary>
        static HashSet<HashSet<DfaDigraphNode>> HopcroftSplit(HashSet<HashSet<DfaDigraphNode>> set)
        {
            bool haveSplited = false;
            set = new(set);
            do
            {
                haveSplited = false;
                foreach (var subSet in set.ToArray())
                {
                    set.Remove(subSet);
                    var newSet = SingleSplit(subSet);
                    if (newSet.Count > 1)
                        haveSplited = true;
                    set.UnionWith(newSet);
                }
            } while (haveSplited);

            return set;
        }

        /// <summary>
        /// 分割单个子集
        /// </summary>
        static HashSet<HashSet<DfaDigraphNode>> SingleSplit(HashSet<DfaDigraphNode> set)
        {
            HashSet<HashSet<DfaDigraphNode>> sets = new();
            var chars = set.SelectMany(n => n.Edges).Select(p => p.Key).Distinct().ToArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (set.Count <= 1)
                    break;

                char c = (char)chars[i];
                //HashSet<DfaDigraphNode> newSet = new(set.Where(n
                //    => n.Edges.Any(e
                //         =>
                //    {
                //        return e.Value == c
                //        && !set.Contains(e.Node);
                //    })));
                HashSet<DfaDigraphNode> newSet = new(set.Where(n => n.Edges.ContainsKey(c) && !set.Contains(n.Edges[c])));


                if (newSet.Count > 0)
                {
                    set = new(set.Except(newSet));
                    sets.Add(newSet);
                }
            }
            if (set.Count > 0)
                sets.Add(new(set));

            return sets;
        }

        /// <summary>
        /// 将DFA的所有元素，按接受状态和非接受状态，划分子集
        /// </summary>
        static HashSet<HashSet<DfaDigraphNode>> SplitByTail(DfaDigraphNode digraph, NfaDigraphNode endNode)
        {
            HashSet<DfaDigraphNode> N = new(), A = new(), visited = new();
            Queue<DfaDigraphNode> queue = new();
            var node = digraph;
            queue.Enqueue(node);
            while (queue.Count > 0)
            {
                node = queue.Dequeue();

                visited.Add(node);
                if (node.NfaElement.Contains(endNode)) A.Add(node);
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
