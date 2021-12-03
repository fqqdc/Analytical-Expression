using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Analytical_Expression
{

    public static class DigraphNodeHelper
    {
        public static HashSet<DigraphNode<TContent, TEdgeValue>>
            EpsilonClosure<TContent, TEdgeValue>(this DigraphNode<TContent, TEdgeValue> node,
            TEdgeValue epsilonValue)
        {
            HashSet<DigraphNode<TContent, TEdgeValue>> visited = new();
            HashSet<DigraphNode<TContent, TEdgeValue>> closure = new();
            Queue<DigraphNode<TContent, TEdgeValue>> queue = new();

            queue.Enqueue(node);
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                closure.Add(n);
                visited.Add(n);

                foreach (var edge in n.Edges)
                {
                    if (edge.Value.Equals(epsilonValue)
                        && !visited.Contains(edge.Node))
                    {
                        queue.Enqueue(edge.Node);
                    }
                }
            }
            return closure;
        }

        public static string
            GetDigraphString<TContent, TEdgeValue>(this DigraphNode<TContent, TEdgeValue> headNode)
        {
            HashSet<DigraphNode<TContent, TEdgeValue>> setVisited = new();
            Queue<DigraphNode<TContent, TEdgeValue>> queue = new();
            Dictionary<DigraphNode<TContent, TEdgeValue>, int> idTable = new();
            StringBuilder builder = new();

            queue.Enqueue(headNode);
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                if (setVisited.Contains(n))
                {
                    continue;
                }

                setVisited.Add(n);

                if (!idTable.TryGetValue(n, out int id))
                {
                    id = idTable.Count;
                    idTable[n] = id;
                }
                builder.Append($"Node_{id}").AppendLine();
                if (n.Content is IEnumerable eContent)
                {
                    foreach (var itemContent in eContent)
                    {
                        builder.Append("    ").Append(itemContent).AppendLine();
                    }
                }
                else
                {
                    builder.Append("    ").Append(n.Content).AppendLine();
                }
                foreach (var eGroup in n.Edges.GroupBy(e => e.Value))
                {
                    builder.Append("@ ").Append(eGroup.Key).Append(" ---> ");
                    foreach (var e in eGroup)
                    {
                        queue.Enqueue(e.Node);
                        if (!idTable.TryGetValue(e.Node, out id))
                        {
                            id = idTable.Count;
                            idTable[e.Node] = id;
                        }
                        builder.Append($"Node_{id}, ");
                    }
                    builder.AppendLine();
                }
                builder.AppendLine();
            }            
            return builder.ToString();
        }

        public static DigraphNode<HashSet<TContent>, TEdgeValue>
            CreateDFA<TContent, TEdgeValue>(this DigraphNode<TContent, TEdgeValue> node,
            TEdgeValue epsilonValue, IEnumerable<TEdgeValue> allEdgeValue)
        {   // 子集构造算法
            Dictionary<HashSet<DigraphNode<TContent, TEdgeValue>>, DigraphNode<HashSet<TContent>, TEdgeValue>> dict = new(HashSetEqualityComparer<DigraphNode<TContent, TEdgeValue>>.Default);
            Dictionary<DigraphNode<TContent, TEdgeValue>, HashSet<DigraphNode<TContent, TEdgeValue>>> ecSetCache = new();

            HashSet<HashSet<DigraphNode<TContent, TEdgeValue>>> setQ = new(HashSetEqualityComparer<DigraphNode<TContent, TEdgeValue>>.Default);
            DigraphNode<HashSet<TContent>, TEdgeValue>? head = null;

            var n0 = node;
            var q0Set = node.EpsilonClosure(epsilonValue);
            setQ.Add(q0Set);
            var workList = new Queue<HashSet<DigraphNode<TContent, TEdgeValue>>>();
            workList.Enqueue(q0Set);
            while (workList.Count > 0)
            {
                var dfaSet = workList.Dequeue();
                foreach (var edgeValue in allEdgeValue)
                {
                    var deltaSet = GetDeltaSet(dfaSet, edgeValue); // 通过c到达子集q终点的集合
                    if (deltaSet.Count == 0) continue;

                    var tSet = GetEpsilonClosureSet(deltaSet, ecSetCache, epsilonValue); // 集合的e闭包

                    if (!dict.TryGetValue(dfaSet, out var dfaNode1))
                    {
                        dfaNode1 = new() { Content = new() };
                        dfaNode1.Content.UnionWith(dfaSet.Select(n => n.Content));

                        if (head == null) head = dfaNode1;
                        dict[dfaSet] = dfaNode1;
                    }
                    var isContain = setQ.Contains(tSet);
                    if (!isContain) setQ.Add(tSet);

                    if (!dict.TryGetValue(tSet, out var dfaNode2))
                    {
                        dfaNode2 = new() { Content = new() };
                        dfaNode2.Content.UnionWith(tSet.Select(n => n.Content));
                        dict[tSet] = dfaNode2;
                    }

                    dfaNode1.Edges.Add((edgeValue, dfaNode2));

                    if (!isContain)
                        workList.Enqueue(tSet);
                }
            }

            return head;

            HashSet<DigraphNode<TContent, TEdgeValue>> GetDeltaSet(HashSet<DigraphNode<TContent, TEdgeValue>> set, TEdgeValue edgeValue)
            {
                HashSet<DigraphNode<TContent, TEdgeValue>> returnSet = new();

                foreach (var n in set)
                {
                    foreach (var edge in n.Edges)
                    {
                        if (edge.Value.Equals(edgeValue))
                        {
                            returnSet.Add(edge.Node);
                        }
                    }
                }

                return returnSet;
            }

            HashSet<DigraphNode<TContent, TEdgeValue>> GetEpsilonClosureSet(HashSet<DigraphNode<TContent, TEdgeValue>> set,
                Dictionary<DigraphNode<TContent, TEdgeValue>, HashSet<DigraphNode<TContent, TEdgeValue>>> cache,
                TEdgeValue epsilonValue)
            {
                HashSet<DigraphNode<TContent, TEdgeValue>> newSet = new();

                foreach (var n in set)
                {
                    HashSet<DigraphNode<TContent, TEdgeValue>>? eClosureSet;
                    if (!cache.TryGetValue(n, out eClosureSet))
                    {
                        eClosureSet = n.EpsilonClosure(epsilonValue);
                        cache[n] = eClosureSet;
                    }
                    newSet.UnionWith(eClosureSet);
                }

                return newSet;
            }
        }


    }
}
