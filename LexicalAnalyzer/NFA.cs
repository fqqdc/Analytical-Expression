using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LexicalAnalyzer
{
    public class NFA : FA
    {
        public NFA(IEnumerable<State> S, IEnumerable<char> Sigma, IEnumerable<(State s1, Edge t, State s2)> MappingTable, IEnumerable<State> S_0, IEnumerable<State> Z)
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
            var Sigma = new HashSet<char>();
            Sigma.Add(c);

            //Mapping
            var MappingTable = new HashSet<(State s1, Edge t, State s2)>();
            MappingTable.Add((head, new EdgeRight(c), tail));

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
            var Sigma = new HashSet<char>();

            //Mapping
            var MappingTable = new HashSet<(State s1, Edge t, State s2)>();
            MappingTable.Add((head, EdgeNoright.Instance, tail));

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
            if (S_0.Count() > 0)
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
            var Sigma = new HashSet<char>();
            Sigma.UnionWith(fst.Sigma);
            Sigma.UnionWith(snd.Sigma);

            //Mapping
            var MappingTable = fst.MappingTable.Select(i => (i.s1, i.e, i.s2)).ToHashSet();
            foreach (var item in snd.MappingTable)
            {
                var s1 = S.Single(s => s.Guid == item.s1.Guid);
                var s2 = S.Single(s => s.Guid == item.s2.Guid);
                MappingTable.Add((s1, item.e, s2));
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
                MappingTable.Add((s1, EdgeNoright.Instance, mid));
            }
            foreach (var snd_s2 in snd.S_0)
            {
                var s2 = S.Single(s => s.Guid == snd_s2.Guid);
                MappingTable.Add((mid, EdgeNoright.Instance, s2));
            }

            return new(S, Sigma, MappingTable, S_0, Z);
        }
    }
}
