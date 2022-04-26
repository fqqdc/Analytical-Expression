using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexicalAnalyzer
{
    public class DFA_Old : FA
    {
        public DFA_Old(IEnumerable<int> S, IEnumerable<char> Sigma, IEnumerable<(int s1, char c, int s2)> MappingTable, int S_0, IEnumerable<int> Z)
            : base(S, Sigma, MappingTable, Z) { this.S_0 = S_0; }
        public int S_0 { get; private set; }

        protected override void S0ToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"S_0 : {S_0}").AppendLine();
        }

        public static DFA_Old CreateFrom(NFA nfa)
        {
            // 初始化，添加唯一的初态x，唯一的终态y，返回(x, 新NFA, y)
            static (int x, NFA newNfa, int y) InitNfa(NFA dig)
            {
                //S
                var S = new HashSet<int>();
                int x = 0;
                S.Add(x);
                var base_id = S.Count;
                S.UnionWith(dig.S.Select(s => base_id + s));
                int y = S.Count;
                S.Add(y);

                //Sigma
                var Sigma = new HashSet<char>();
                Sigma.UnionWith(dig.Sigma);

                //Mapping
                var MappingTable = new HashSet<(int s1, char c, int s2)>();
                MappingTable.UnionWith(dig.MappingTable.Select(i => (base_id + i.s1, i.c, base_id + i.s2)));

                //Z
                var Z = new HashSet<int>();
                Z.Add(y);

                //S_0
                var S_0 = new HashSet<int>();
                S_0.Add(x);

                //Init
                MappingTable.UnionWith(dig.S_0.Select(s => (x, FA.CHAR_Epsilon, base_id + s)));
                MappingTable.UnionWith(dig.Z.Select(z => (base_id + z, FA.CHAR_Epsilon, y)));

                return (x, new(S, Sigma, MappingTable, S_0, Z), y);
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
            (var x, nfa, var y) = InitNfa(nfa); // 初始化
            var nfaMappingTable = nfa.MappingTable;
            var Q = new HashSet<HashSet<int>>(HashSetComparer<int>.Default);
            var workQ = new Queue<HashSet<int>>();
            var I = EpsilonClosure(nfaMappingTable, nfa.S_0);
            Q.Add(I);
            Set2Id[I] = Set2Id.Count;
            workQ.Enqueue(I);

            //Mapping
            var MappingTable = new HashSet<(int s1, char c, int s2)>();

            // 构造子集
            while (workQ.Count > 0)
            {
                I = workQ.Dequeue();
                foreach (var c in nfa.Sigma)
                {
                    var J = Delta(nfaMappingTable, I, c);
                    if (!J.Any()) continue;
                    var I_c = EpsilonClosure(nfaMappingTable, J);
                    if (Q.Add(I_c))
                    {
                        Set2Id[I_c] = Set2Id.Count;
                        workQ.Enqueue(I_c);
                    }
                    MappingTable.Add((Set2Id[I], c, Set2Id[I_c]));
                }
            }

            //S
            var S = Q.Select(s => Set2Id[s]).ToArray();

            //Sigma
            var Sigma = nfa.Sigma.ToArray();

            //Z
            var Z = Q.Where(s => s.Contains(y)).Select(s => Set2Id[s]).ToArray();

            //S_0
            var S_0 = Q.Where(s => s.Contains(x)).Select(s => Set2Id[s]).Single();

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        public DFA_Old Minimize()
        {
            // 分割 非终态集合、终态集合
            var Q = S.GroupBy(s => Z.Contains(s)).Select(g => g.ToHashSet())
                .ToHashSet(HashSetComparer<int>.Default);

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
            var newZ = Z_I.Select(I => Set2Id[I]).ToArray();

            //S_0
            var newS_0 = Set2Id[I_0];

            return new(newS, newSigma, newMappingTable, newS_0, newZ);
        }

        public NFA ToNFA()
        {
            return new(S, Sigma, MappingTable, new int[] { S_0 }, Z);
        }
    }

    public static class DFAHelper
    {

    }
}
