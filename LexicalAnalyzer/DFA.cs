using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexicalAnalyzer
{
    public class DFA : FA
    {
        private DFA(IEnumerable<int> S, IEnumerable<char> Sigma, IEnumerable<(int s1, char c, int s2)> MappingTable, int S_0, IEnumerable<int> Z)
            : base(S, Sigma, MappingTable, Z)
        {
            this.S_0 = S_0;
            this._ZNfaNodes = new();
        }

        public int S_0 { get; private set; }

        protected override void S0ToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"S_0 : {S_0}").AppendLine();
        }

        protected void NfaNodesToString(StringBuilder builder)
        {
            builder.Append($"NfaNodesToString :").AppendLine();
            foreach (var s in _Z)
            {
                builder.Append(PRE).Append($"{s} : {string.Join(" ", _ZNfaNodes[s].OrderBy(i => i))}").AppendLine();
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new();
            builder.AppendLine(base.ToString());
            NfaNodesToString(builder);
            return builder.ToString();
        }

        private Dictionary<int, HashSet<int>> _ZNfaNodes;
        public Dictionary<int, HashSet<int>> ZNfaNodes
        {
            get { return _ZNfaNodes.ToDictionary(i => i.Key, i => i.Value.ToHashSet()); }
        }

        public static DFA CreateFrom(NFA nfa)
        {
            // 初始化，添加唯一的初态x，返回(x, 新NFA)
            static (int x, NFA newNfa) InitNfa(NFA dig)
            {
                //S
                var S = new HashSet<int>(dig.S);
                int x = S.Count;
                S.Add(x);

                //Sigma
                var Sigma = new HashSet<char>();
                Sigma.UnionWith(dig.Sigma);

                //Mapping
                var MappingTable = new HashSet<(int s1, char c, int s2)>();
                MappingTable.UnionWith(dig.MappingTable);

                //Z
                var Z = new HashSet<int>();
                Z.UnionWith(dig.Z);

                //S_0
                var S_0 = new HashSet<int>();
                S_0.Add(x);

                //Init
                MappingTable.UnionWith(dig.S_0.Select(s => (x, FA.CHAR_Epsilon, s)));

                return (x, new(S, Sigma, MappingTable, S_0, Z));
            }
            static HashSet<int> EpsilonClosureSingle(IEnumerable<(int s1, char c, int s2)> nfaMappingTable, int s)
            {
                HashSet<int> visited = new();
                Queue<int> queue = new();

                queue.Enqueue(s);
                while (queue.Count > 0)
                {
                    var s1 = queue.Dequeue();
                    visited.Add(s1);

                    foreach (var s2 in nfaMappingTable.Where(i => i.s1 == s1 && i.c == FA.CHAR_Epsilon)
                        .Select(i => i.s2))
                    {
                        if (!visited.Contains(s2))
                        {
                            queue.Enqueue(s2);
                        }
                    }
                }
                return visited;
            }
            static HashSet<int> EpsilonClosure(IEnumerable<(int s1, char c, int s2)> nfaMappingTable, IEnumerable<int> S)
            {
                HashSet<int> newSet = new();

                foreach (var s in S)
                {
                    HashSet<int> closureSet = EpsilonClosureSingle(nfaMappingTable, s);
                    newSet.UnionWith(closureSet);
                }

                return newSet;
            }
            // I集合中的状态，经过c到达状态的集合
            static HashSet<int> Delta(IEnumerable<(int s1, char c, int s2)> nfaMappingTable, HashSet<int> I, char c)
            {
                return nfaMappingTable.Where(i => I.Contains(i.s1) && i.c == c)
                    .Select(i => i.s2).ToHashSet();
            }

            Dictionary<HashSet<int>, int> Set2Id = new(HashSetComparer<int>.Default);
            (var x, nfa) = InitNfa(nfa); // 初始化
            var nfaMappingTable = nfa.MappingTable;
            var Q = new HashSet<HashSet<int>>(HashSetComparer<int>.Default);
            var queue = new Queue<HashSet<int>>();
            var I = EpsilonClosure(nfaMappingTable, nfa.S_0);
            Q.Add(I);
            Set2Id[I] = Set2Id.Count;
            queue.Enqueue(I);

            //Mapping
            var MappingTable = new HashSet<(int s1, char c, int s2)>();

            // 构造子集
            while (queue.Count > 0)
            {
                I = queue.Dequeue();
                foreach (var c in nfa.Sigma)
                {
                    var J = Delta(nfaMappingTable, I, c);
                    if (!J.Any()) continue;
                    var I_c = EpsilonClosure(nfaMappingTable, J);
                    if (Q.Add(I_c))
                    {
                        Set2Id[I_c] = Set2Id.Count;
                        queue.Enqueue(I_c);
                    }
                    MappingTable.Add((Set2Id[I], c, Set2Id[I_c]));
                }
            }

            //S
            var S = Q.Select(s => Set2Id[s]).ToArray();

            //Sigma
            var Sigma = nfa.Sigma.ToArray();

            //Z
            var Qz = Q.Where(s => s.Intersect(nfa.Z).Any());
            var Z = Qz.Select(s => Set2Id[s]).ToArray();

            //ZNfaNodes
            var nfaNodes = Qz.ToDictionary(I => Set2Id[I], I => I.Intersect(nfa.Z).ToHashSet());

            //S_0
            var S_0 = Q.Where(s => s.Contains(x)).Select(s => Set2Id[s]).Single();

            return new(S, Sigma, MappingTable, S_0, Z) { _ZNfaNodes = nfaNodes };
        }

        /// <summary>
        /// 分割集合
        /// 按 非终态集合、终态集合 分割
        /// </summary>
        private HashSet<HashSet<int>> SplitSet()
        {
            return S.GroupBy(s => Z.Contains(s)).Select(g => g.ToHashSet())
                .ToHashSet(HashSetComparer<int>.Default);
        }

        /// <summary>
        /// 分割集合
        /// 按 非终态集合、终态集合 分割
        /// 其中 终态集合 按原NFA中的终态情况分割
        /// </summary>
        private HashSet<HashSet<int>> SplitSetByNfa()
        {
            return S.GroupJoin(Z,
                    s => s,
                    z => z,
                    (s, arrZ) => new { s, nfaNodes = arrZ.SelectMany(z => _ZNfaNodes[z]).ToHashSet() })
                .GroupBy(i => i.nfaNodes, HashSetComparer<int>.Default)
                .Select(g => g.Select(i => i.s).ToHashSet())
                .ToHashSet(HashSetComparer<int>.Default);
        }

        /// <summary>
        /// 分割集合
        /// 按 非终态集合、终态集合 分割
        /// 其中 终态集合 按原DFA中的终态情况分割
        /// </summary>
        private HashSet<HashSet<int>> SplitSetByZ()
        {
            return S.GroupBy(s =>
            {
                if (_ZNfaNodes.TryGetValue(s, out var set))
                {
                   return string.Join(" ", _ZNfaNodes[s].OrderBy(i => i));
                }
                return string.Empty;
            })
                .Select(g => g.ToHashSet())
                .ToHashSet(HashSetComparer<int>.Default);
        }

        public DFA MinimizeProcess(HashSet<HashSet<int>> Q)
        {
            // 分割 子集
            bool isChanged = true;
            while (isChanged)
            {
                isChanged = false;
                foreach (var I in Q)
                {
                    if (I.Count <= 1)
                        continue;
                    foreach (var c in Sigma)
                    {
                        // 分割
                        // 查找所有状态转移记录
                        var e1 = MappingTable.Where(i => I.Contains(i.s1) && i.c == c);
#if DEBUG
                        // 左连接，I中各个元素的状态转移结果
                        var e2 = I.GroupJoin(e1, out_i => out_i, in_i => in_i.s1,
                            (out_i, in_arr) => new { s1 = out_i, I = in_arr.Any() ? Q.Single(I => I.Contains(in_arr.Single().s2)) : null });
#else
                        var e2 = I.GroupJoin(e1, out_i => out_i, in_i => in_i.s1,
                            (out_i, in_arr) => new { s1 = out_i, I = in_arr.Any() ? Q.First(I => I.Contains(in_arr.First().s2)) : null });
#endif

                        // 根据转移结果分组
                        var e3 = e2.GroupBy(i => i.I)
                            .Select(g => g.Select(i => i.s1).ToHashSet());
                        var arrI = e3.ToArray();

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

            Dictionary<HashSet<int>, int> Set2Id = new(HashSetComparer<int>.Default);
            var I_0 = Q.Single(I => I.Contains(S_0));
            Set2Id[I_0] = Set2Id.Count;

            //Mapping
            var newMappingTable = new HashSet<(int s1, char c, int s2)>();

            // 根据子集创建状态、添加映射表
            var workQ = new Queue<HashSet<int>>();
            workQ.Enqueue(I_0);
            while (workQ.Count > 0)
            {
                var I = workQ.Dequeue();
                var s = I.First();
                foreach (var i in MappingTable.Where(i => i.s1 == s))
                {
                    var sI = Set2Id[I];
                    var J = Q.Single(I => I.Contains(i.s2));
                    if (!Set2Id.TryGetValue(J, out var sJ))
                    {
                        workQ.Enqueue(J);
                        Set2Id[J] = Set2Id.Count;
                        sJ = Set2Id[J];
                    }
                    newMappingTable.Add((sI, i.c, sJ));
                }
            }

            //S
            var newS = newMappingTable.SelectMany(i => new int[] { i.s1, i.s2 }).ToHashSet();

            //Sigma
            var newSigma = Sigma;

            //Z
            var Z_I = Q.Where(I => I.Any(s => Z.Contains(s)));
            var newZ = Z_I.Select(I => Set2Id[I]).OrderBy(i => i).ToArray();

            //NfaNodes
            var nfaNodes = Z_I.ToDictionary(I => Set2Id[I], I => I.SelectMany(s => _ZNfaNodes[s]).ToHashSet());

            //S_0
            var newS_0 = Set2Id[I_0];

            return new(newS, newSigma, newMappingTable, newS_0, newZ) { _ZNfaNodes = nfaNodes };
        }

        public DFA Minimize()
        {
            return MinimizeProcess(SplitSet());
        }

        public DFA MinimizeByNfaFinal()
        {
            return MinimizeProcess(SplitSetByNfa());
        }

        public DFA MinimizeByZ()
        {
            return MinimizeProcess(SplitSetByZ());
        }

        public NFA ToNFA()
        {
            bool addTail = _Z.Count > 1;
            var nfa = new NFA(S, Sigma, MappingTable, new int[] { S_0 }, Z);
            if (addTail)
                nfa = nfa.Join(NFA.CreateEpsilon());
            return nfa;
        }
    }
}
