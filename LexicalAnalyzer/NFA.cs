using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LexicalAnalyzer
{
    public class NFA : FA
    {
        public NFA(IEnumerable<State> S, IEnumerable<Symbol> Sigma, IEnumerable<(State s1, Symbol symbol, State s2)> MappingTable, IEnumerable<State> S_0, IEnumerable<State> Z)
            : base(S, Sigma, MappingTable, Z)
        {
            _S_0 = S_0.ToHashSet();
        }

        protected HashSet<State> _S_0;

        public IEnumerable<State> S_0 { get => _S_0.AsEnumerable(); }

        //{t}
        public static NFA CreateFrom(char c)
        {
            //S
            var S = new HashSet<State>();
            int id = 0;
            State head = new(id);
            S.Add(head);
            id = S.Count;
            State tail = new(id);
            S.Add(tail);

            //Sigma
            var Sigma = new HashSet<Symbol>();
            Sigma.Add(c);

            //Mapping
            var MappingTable = new HashSet<(State s1, Symbol symbol, State s2)>();
            MappingTable.Add((head, c, tail));

            //Z
            var Z = new HashSet<State>();
            Z.Add(tail);

            //S_0
            var S_0 = new HashSet<State>();
            S_0.Add(head);

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        //{eps}
        public static NFA CreateEpsilon()
        {
            //S
            var S = new HashSet<State>();
            int id = 0;
            State head = new(id);
            S.Add(head);
            id = S.Count;
            State tail = new(id);
            S.Add(tail);

            //Sigma
            var Sigma = new HashSet<Symbol>();

            //Mapping
            var MappingTable = new HashSet<(State s1, Symbol symbol, State s2)>();
            MappingTable.Add((head, Symbol.Epsilon, tail));

            //Z
            var Z = new HashSet<State>();
            Z.Add(tail);

            //S_0
            var S_0 = new HashSet<State>();
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

        ////[{from}-{to}]
        //public static NFA CreateRange(char from, char to)
        //{
        //    Debug.Assert(from <= to);
        //    NFA dig = CreateFrom(from);
        //    for (char c = (char)(from + 1); c <= to; c++)
        //    {
        //        var newDig = CreateFrom(c);
        //        dig = dig.Or(newDig);
        //    }
        //    return dig;
        //}

        //public static NFA CreateFromString(string str)
        //{
        //    NFA dig = CreateEpsilon();
        //    foreach (var c in str)
        //    {
        //        dig = dig.Join(CreateFrom(c.ToString()));
        //    }
        //    return dig;
        //}
    }

    public static class NFAHelper
    {
        //{fst}{snd}
        public static NFA Join(this NFA fst, NFA snd)
        {
            //S
            var S = new HashSet<State>();
            S.UnionWith(fst.S);
            var mid = new State(S.Count);
            S.Add(mid);
            int snd_base_id = S.Count;
            S.UnionWith(snd.S.Select(i => new State(i, snd_base_id + i.Id)));

            //Sigma
            var Sigma = new HashSet<Symbol>();
            Sigma.UnionWith(fst.Sigma);
            Sigma.UnionWith(snd.Sigma);

            //Mapping
            var MappingTable = fst.MappingTable.Select(i => (i.s1, i.symbol, i.s2)).ToHashSet();
            foreach (var item in snd.MappingTable)
            {
                var s1 = S.Single(s => s.Guid == item.s1.Guid);
                var s2 = S.Single(s => s.Guid == item.s2.Guid);
                MappingTable.Add((s1, item.symbol, s2));
            }

            //Z
            var Z = new HashSet<State>();
            foreach (var snd_z in snd.Z)
            {
                var z = S.Single(s => s.Guid == snd_z.Guid);
                Z.Add(z);
            }

            //S_0
            var S_0 = fst.S_0;

            //Join
            foreach (var s1 in fst.Z)
            {
                MappingTable.Add((s1, Symbol.Epsilon, mid));
            }
            foreach (var snd_s2 in snd.S_0)
            {
                var s2 = S.Single(s => s.Guid == snd_s2.Guid);
                MappingTable.Add((mid, Symbol.Epsilon, s2));
            }

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        //{fst}|{snd}
        public static NFA Or(this NFA fst, NFA snd)
        {
            //S
            var S = new HashSet<State>();
            State head = new(0);
            S.Add(head);
            int fst_base_id = S.Count;
            S.UnionWith(fst.S.Select(s => new State(s, fst_base_id + s.Id)));
            int snd_base_id = S.Count;
            S.UnionWith(snd.S.Select(s => new State(s, snd_base_id + s.Id)));
            State tail = new(S.Count);
            S.Add(tail);

            //Sigma
            var Sigma = new HashSet<Symbol>();
            Sigma.UnionWith(fst.Sigma);
            Sigma.UnionWith(snd.Sigma);

            //Mapping
            var MappingTable = new HashSet<(State s1, Symbol symbol, State s2)>();
            foreach (var item in fst.MappingTable)
            {
                var s1 = S.Single(s => s.Guid == item.s1.Guid);
                var s2 = S.Single(s => s.Guid == item.s2.Guid);
                MappingTable.Add((s1, item.symbol, s2));
            }
            foreach (var item in snd.MappingTable)
            {
                var s1 = S.Single(s => s.Guid == item.s1.Guid);
                var s2 = S.Single(s => s.Guid == item.s2.Guid);
                MappingTable.Add((s1, item.symbol, s2));
            }

            //Z
            var Z = new HashSet<State>();
            Z.Add(tail);

            //S_0
            var S_0 = new HashSet<State>();
            S_0.Add(head);

            //Or
            foreach (var item in fst.S_0)
            {
                var s2 = S.Single(s => s.Guid == item.Guid);
                MappingTable.Add((head, Symbol.Epsilon, s2));
            }
            foreach (var item in snd.S_0)
            {
                var s2 = S.Single(s => s.Guid == item.Guid);
                MappingTable.Add((head, Symbol.Epsilon, s2));
            }
            foreach (var item in fst.Z)
            {
                var s1 = S.Single(s => s.Guid == item.Guid);
                MappingTable.Add((s1, Symbol.Epsilon, tail));
            }
            foreach (var item in snd.Z)
            {
                var s1 = S.Single(s => s.Guid == item.Guid);
                MappingTable.Add((s1, Symbol.Epsilon, tail));
            }

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        //{dig}*
        public static NFA Closure(this NFA dig)
        {
            //S
            var S = new HashSet<State>();
            State head = new(0);
            S.Add(head);
            int dig_base_id = S.Count;
            S.UnionWith(dig.S.Select(s => new State(s, dig_base_id + s.Id)));
            State tail = new(S.Count);
            S.Add(tail);

            //Sigma
            var Sigma = new HashSet<Symbol>();
            Sigma.UnionWith(dig.Sigma);

            //Mapping
            var MappingTable = new HashSet<(State s1, Symbol symbol, State s2)>();
            foreach (var item in dig.MappingTable)
            {
                var s1 = S.Single(s => s.Guid == item.s1.Guid);
                var s2 = S.Single(s => s.Guid == item.s2.Guid);
                MappingTable.Add((s1, item.symbol, s2));
            }

            //Z
            var Z = new HashSet<State>();
            Z.Add(tail);

            //S_0
            var S_0 = new HashSet<State>();
            S_0.Add(head);

            //Union
            MappingTable.Add((head, Symbol.Epsilon, tail));
            foreach (var item in dig.S_0)
            {
                var s2 = S.Single(s => s.Guid == item.Guid);
                MappingTable.Add((head, Symbol.Epsilon, s2));
            }
            foreach (var item in dig.Z)
            {
                var s1 = S.Single(s => s.Guid == item.Guid);
                foreach (var item2 in dig.S_0)
                {
                    var s2 = S.Single(s => s.Guid == item2.Guid);
                    MappingTable.Add((s1, Symbol.Epsilon, s2));
                }
                MappingTable.Add((s1, Symbol.Epsilon, tail));
            }

            return new(S, Sigma, MappingTable, S_0, Z);
        }
    }
}
