using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexicalAnalyzer
{
    public class DFA : FA
    {
        public DFA(IEnumerable<int> S, IEnumerable<char> Sigma, IEnumerable<(int s1, char c, int s2)> MappingTable, int S_0, IEnumerable<int> Z)
            : base(S, Sigma, MappingTable, Z) { this.S_0 = S_0; }
        public int S_0 { get; private set; }

        protected override void S0ToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"S_0 : {S_0}").AppendLine();
        }

        public static DFA CreateFrom(NFA nfa)
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
                MappingTable.UnionWith(dig.Z.Select(z => (y, FA.CHAR_Epsilon, base_id + z)));

                return (x, new(S, Sigma, MappingTable, S_0, Z), y);
            }
            static HashSet<int> EpsilonClosureSingle(IEnumerable<(int s1, char c, int s2)> nfaMappingTable, int s)
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

                    foreach (var s2 in nfaMappingTable.Where(i => i.s1 == s1 && i.c == FA.CHAR_Epsilon)
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
            // 返回：I集合中的状态，经过c到达状态的集合
            static HashSet<int> Delta(IEnumerable<(int s1, char c, int s2)> nfaMappingTable, HashSet<int> I, char c)
            {
                return nfaMappingTable.Where(i => I.Contains(i.s1) && i.c == c)
                    .Select(i => i.s2).ToHashSet();
            }

            // 初始化
            var (x, dig, y) = InitNfa(nfa);
            var nfaMappingTable = nfa.MappingTable;
            var Q = new HashSet<HashSet<int>>(HashSetComparer<int>.Default);
            var workQ = new Queue<HashSet<int>>();
            var I = EpsilonClosure(nfaMappingTable, nfa.S_0);
            Q.Add(I);
            workQ.Enqueue(I);

            // 构造子集
            while(workQ.Count > 0)
            {
                I = workQ.Dequeue();
                foreach (var c in dig.Sigma)
                {
                    var J = Delta(nfaMappingTable, I, c);
                    var I_c = EpsilonClosure(nfaMappingTable, J);
                    if (Q.Add(I_c))
                        workQ.Enqueue(I_c);
                }
            }

            //S
            var S = new HashSet<int>();

            throw new NotImplementedException();

            //Mapping
            var MappingTable = new HashSet<(int s1, char c, int s2)>();
            //Z
            var Z = new HashSet<int>();

            throw new Exception();
        }
    }

    public static class DFAHelper
    {

    }
}
