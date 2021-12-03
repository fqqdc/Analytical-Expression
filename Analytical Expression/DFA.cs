using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class DFA : FA
    {
        public DFA(IEnumerable<int> S, IEnumerable<Terminal> Sigma, IEnumerable<(int s1, Terminal t, int s2)> MappingTable, int S_0, IEnumerable<int> Z)
            : base(S, Sigma, MappingTable, S_0, Z) { }

        public Dictionary<(int s, Terminal t), int> MappingDictionary
        {
            get
            {
                return MappingTable.ToDictionary(i => (i.s1, i.t), i => i.s2);
            }
        }

        private static HashSet<int> EpsilonClosure(IEnumerable<(int s1, Terminal t, int s2)> nfaMappingTable, int s)
        {
            HashSet<int> visited = new();
            HashSet<int> closure = new();
            Queue<int> queue = new();

            queue.Enqueue(s);
            while (queue.Count > 0)
            {
                var s1 = queue.Dequeue();
                closure.Add(s1);
                visited.Add(s1);

                foreach (var s2 in nfaMappingTable.Where(i => i.s1 == s1 && i.t == FA.EPSILON)
                    .Select(i => i.s2))
                {
                    if (!visited.Contains(s2))
                    {
                        queue.Enqueue(s2);
                    }
                }
            }
            return closure;
        }
        private static HashSet<int> EpsilonClosure(IEnumerable<(int s1, Terminal t, int s2)> nfaMappingTable, HashSet<int> S)
        {
            HashSet<int> newSet = new();

            foreach (var s in S)
            {
                HashSet<int> closureSet = EpsilonClosure(nfaMappingTable, s);
                newSet.UnionWith(closureSet);
            }

            return newSet;
        }
        private static HashSet<int> Delta(IEnumerable<(int s1, Terminal t, int s2)> nfaMappingTable, HashSet<int> S, Terminal t)
        {
            return nfaMappingTable.Where(i => S.Contains(i.s1) && i.t == t)
                .Select(i => i.s2).ToHashSet();
        }

        public static DFA CreateFrom(FA nfa)
        {
            Dictionary<HashSet<int>, int> IToID = new(HashSetComparer<int>.Default);
            //Mapping
            var MappingTable = new HashSet<(int s1, Terminal t, int s2)>();
            //Z
            var Z = new HashSet<int>();

            var nfaMappingTable = nfa.MappingTable;
            var I_0 = EpsilonClosure(nfaMappingTable, nfa.S_0);
            I_0.Add(nfa.S_0);
            var Q = new HashSet<HashSet<int>>(HashSetComparer<int>.Default);
            Q.Add(I_0);
            IToID[I_0] = IToID.Count;
            var workQueue = new Queue<HashSet<int>>();
            workQueue.Enqueue(I_0);
            while (workQueue.Count > 0)
            {
                var I = workQueue.Dequeue();
                foreach (var t in nfa.Sigma)
                {
                    var I_t = Delta(nfaMappingTable, I, t);
                    if (I_t.Count == 0)
                        continue;
                    I_t = EpsilonClosure(nfaMappingTable, I_t);
                    if (Q.Add(I_t))
                    {
                        workQueue.Enqueue(I_t);
                        IToID[I_t] = IToID.Count;
                        if (I_t.Intersect(nfa.Z).Count() != 0)
                            Z.Add(IToID[I_t]);
                    }
                    MappingTable.Add((IToID[I], t, IToID[I_t]));
                }
            }

            //S
            var S = new HashSet<int>();
            S.UnionWith(MappingTable.SelectMany(i => new int[] { i.s1, i.s2 }));

            //Sigma
            var Sigma = new HashSet<Terminal>();
            Sigma.UnionWith(nfa.Sigma);

            //S_0
            int S_0 = IToID[I_0];

            return new(S, Sigma, MappingTable, S_0, Z);
        }
    }

    public static class DFAHelper
    {
        public static DFA Minimize(this DFA dfa)
        {
            var dict = dfa.MappingDictionary;
            var Q = dfa.S.GroupBy(s => dfa.Z.Contains(s)).Select(g => g.ToHashSet())
                .ToHashSet(HashSetComparer<int>.Default);

            // 分割集合
            bool isChanged = true;
            while (isChanged)
            {
                isChanged = false;
                foreach (var I in Q)
                {
                    if (I.Count == 1)
                        continue;
                    foreach (var t in dfa.Sigma)
                    {
                        var arrI = I.GroupBy(s1 =>
                        {
                            int s2 = -1;
                            dict.TryGetValue((s1, t), out s2);
                            return Q.Single(iI => iI.Contains(s2));
                        }, HashSetComparer<int>.Default).Select(g => g.ToHashSet())
                        .ToArray();

                        if (arrI.Length > 1)
                        {
                            isChanged = true;
                            Q.Remove(I);
                            Q.UnionWith(arrI);
                        }
                        if (isChanged) break;
                    }
                    if (isChanged) break;
                }
            }

            Dictionary<HashSet<int>, int> IToID = new(HashSetComparer<int>.Default);
            //Mapping
            var MappingTable = new HashSet<(int s1, Terminal t, int s2)>();
            //Z
            var Z = new HashSet<int>();

            var I_0 = Q.Single(I => I.Contains(dfa.S_0));
            IToID[I_0] = IToID.Count;
            Queue<HashSet<int>> workQueue = new();
            workQueue.Enqueue(I_0);

            // 根据子集创建状态、添加映射表
            while (workQueue.Count > 0)
            {
                var I = workQueue.Dequeue();
                if (!IToID.TryGetValue(I, out var _))
                {
                    IToID[I] = IToID.Count;
                }
                if (I.Intersect(dfa.Z).Count() > 0)
                {
                    Z.Add(IToID[I]);
                }

                foreach (var s1 in I)
                {
                    foreach (var t in dfa.Sigma)
                    {
                        if (!dict.TryGetValue((s1, t), out var s2))
                            continue;
                        foreach (var I_t in Q.Where(I => I.Contains(s2)))
                        {
                            if (!IToID.TryGetValue(I_t, out var _))
                            {
                                IToID[I_t] = IToID.Count;
                                workQueue.Enqueue(I_t);
                            }
                            MappingTable.Add((IToID[I], t, IToID[I_t]));
                        }
                    }
                }
            }

            //S
            var S = new HashSet<int>();
            S.UnionWith(MappingTable.SelectMany(i => new int[] { i.s1, i.s2 }));

            //Sigma
            var Sigma = new HashSet<Terminal>();
            Sigma.UnionWith(dfa.Sigma);

            //S_0
            var S_0 = IToID[I_0];

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        public static NFA ToNFA(this DFA dfa)
        {
            return new(dfa.S, dfa.Sigma, dfa.MappingTable, dfa.S_0, dfa.Z);
        }
    }
}
