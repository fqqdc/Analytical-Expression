using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SyntaxAnalyzer
{
    public class NFA : FA
    {
        public NFA(IEnumerable<int> S, IEnumerable<Symbol> Sigma, IEnumerable<(int s1, Symbol symbol, int s2)> MappingTable, IEnumerable<int> S_0, IEnumerable<int> Z)
            : base(S, Sigma, MappingTable, Z)
        {
            _S_0 = S_0.ToHashSet();
        }

        protected HashSet<int> _S_0;

        public IEnumerable<int> S_0 { get => _S_0.AsEnumerable(); }

        //{t}
        public static NFA CreateFrom(Symbol symbol)
        {
            //S
            var S = new HashSet<int>();
            int id = 0;
            int head = id;
            S.Add(head);
            id = S.Count;
            int tail = id;
            S.Add(tail);

            //Sigma
            var Sigma = new HashSet<Symbol>();
            Sigma.Add(symbol);

            //Mapping
            var MappingTable = new HashSet<(int s1, Symbol symbol, int s2)>();
            MappingTable.Add((head, symbol, tail));

            //Z
            var Z = new HashSet<int>();
            Z.Add(tail);

            //S_0
            var S_0 = new HashSet<int>();
            S_0.Add(head);

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        //{eps}
        public static NFA CreateEpsilon()
        {
            //S
            var S = new HashSet<int>();
            int id = 0;
            int head = id;
            S.Add(head);
            id = S.Count;
            int tail = id;
            S.Add(tail);

            //Sigma
            var Sigma = new HashSet<Symbol>();

            //Mapping
            var MappingTable = new HashSet<(int s1, Symbol symbol, int s2)>();
            MappingTable.Add((head, Terminal.Epsilon, tail));

            //Z
            var Z = new HashSet<int>();
            Z.Add(tail);

            //S_0
            var S_0 = new HashSet<int>();
            S_0.Add(head);

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        protected override void S0ToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"S0 : {{");
            foreach (var s in S_0)
            {
                builder.Append($" {s},");
            }
            if (S_0.Any())
                builder.Length -= 1;
            builder.Append(" }").AppendLine();
        }

        public static NFA CreateFromString(string str)
        {
            NFA dig = CreateEpsilon();
            foreach (var c in str)
            {
                dig = dig.Join(CreateFrom(c));
            }
            return dig;
        }
    }

    public static class NFAHelper
    {
        //{fst}{snd}
        public static NFA Join(this NFA fst, NFA snd)
        {
            //S
            var S = new HashSet<int>();
            S.UnionWith(fst.S);
            var mid = S.Count;
            S.Add(mid);
            int snd_base_id = S.Count;
            S.UnionWith(snd.S.Select(s => snd_base_id + s));

            //Sigma
            var Sigma = new HashSet<char>();
            Sigma.UnionWith(fst.Sigma);
            Sigma.UnionWith(snd.Sigma);

            //Mapping
            var MappingTable = new HashSet<(int s1, char c, int s2)>();
            MappingTable.UnionWith(fst.MappingTable);
            MappingTable.UnionWith(snd.MappingTable.Select(i => (snd_base_id + i.s1, i.c, snd_base_id + i.s2)));

            //Z
            var Z = new HashSet<int>();
            Z.UnionWith(snd.Z.Select(z => snd_base_id + z));

            //S_0
            var S_0 = new HashSet<int>();
            S_0.UnionWith(fst.S_0);

            //Join
            MappingTable.UnionWith(fst.Z.Select(z => (z, FA.CHAR_Epsilon, mid)));
            MappingTable.UnionWith(snd.S_0.Select(s => (mid, FA.CHAR_Epsilon, snd_base_id + s)));

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        //{fst}|{snd}
        public static NFA Or(this NFA fst, NFA snd)
        {
            //S
            var S = new HashSet<int>();
            int head = 0;
            S.Add(head);
            int fst_base_id = S.Count;
            S.UnionWith(fst.S.Select(s => fst_base_id + s));
            int snd_base_id = S.Count;
            S.UnionWith(snd.S.Select(s => snd_base_id + s));
            int tail = S.Count;
            S.Add(tail);

            //Sigma
            var Sigma = new HashSet<char>();
            Sigma.UnionWith(fst.Sigma);
            Sigma.UnionWith(snd.Sigma);

            //Mapping
            var MappingTable = new HashSet<(int s1, char c, int s2)>();
            MappingTable.UnionWith(fst.MappingTable.Select(i => (fst_base_id + i.s1, i.c, fst_base_id + i.s2)));
            MappingTable.UnionWith(snd.MappingTable.Select(i => (snd_base_id + i.s1, i.c, snd_base_id + i.s2)));

            //Z
            var Z = new HashSet<int>();
            Z.Add(tail);

            //S_0
            var S_0 = new HashSet<int>();
            S_0.Add(head);

            //Or
            MappingTable.UnionWith(fst.S_0.Select(s => (head, FA.CHAR_Epsilon, fst_base_id + s)));
            MappingTable.UnionWith(snd.S_0.Select(s => (head, FA.CHAR_Epsilon, snd_base_id + s)));
            MappingTable.UnionWith(fst.Z.Select(z => (fst_base_id + z, FA.CHAR_Epsilon, tail)));
            MappingTable.UnionWith(snd.Z.Select(z => (snd_base_id + z, FA.CHAR_Epsilon, tail)));

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        //{dig}*
        public static NFA Closure(this NFA dig)
        {
            //S
            var S = new HashSet<int>();
            int head = 0;
            S.Add(head);
            int dig_base_id = S.Count;
            S.UnionWith(dig.S.Select(s => dig_base_id + s));
            int tail = S.Count;
            S.Add(tail);

            //Sigma
            var Sigma = new HashSet<char>();
            Sigma.UnionWith(dig.Sigma);

            //Mapping
            var MappingTable = new HashSet<(int s1, char c, int s2)>();
            MappingTable.UnionWith(dig.MappingTable.Select(i => (dig_base_id + i.s1, i.c, dig_base_id + i.s2)));

            //Z
            var Z = new HashSet<int>();
            Z.Add(tail);

            //S_0
            var S_0 = new HashSet<int>();
            S_0.Add(head);

            //Union
            MappingTable.Add((head, FA.CHAR_Epsilon, tail));
            MappingTable.UnionWith(dig.S_0.Select(s => (head, FA.CHAR_Epsilon, dig_base_id + s))); // head --eps-> dig.S_0
            MappingTable.UnionWith(dig.Z.Select(z => (dig_base_id + z, FA.CHAR_Epsilon, tail))); // dig.Z --eps-> tail
            MappingTable.UnionWith(dig.Z.Select(z => (tail, FA.CHAR_Epsilon, head))); // tail --eps-> head
            //MappingTable.UnionWith(dig.Z.SelectMany(z => dig.S_0.Select(s => (dig_base_id + z, FA.CHAR_Epsilon, dig_base_id + s)))); // dig.Z --eps-> dig.S_0

            return new(S, Sigma, MappingTable, S_0, Z);
        }
    }
}
