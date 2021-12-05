using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Analytical_Expression
{
    public class NFA : FA
    {
        public NFA(IEnumerable<int> S, IEnumerable<Terminal> Sigma, IEnumerable<(int s1, Terminal t, int s2)> MappingTable, int S_0, IEnumerable<int> Z)
            : base(S, Sigma, MappingTable, S_0, Z) { }
        public Dictionary<(int s, Terminal t), HashSet<int>> MappingDictionary
        {
            get
            {
                return MappingTable
                    .GroupBy(i => (i.s1, i.t))
                    .ToDictionary(g => g.Key, g => g.Select(i => i.s2).ToHashSet());
            }
        }
        //{t}
        public static NFA CreateFrom(Terminal t)
        {
            //S
            var S = new HashSet<int>();
            int id = S.Count;
            int head = id;
            S.Add(head);
            id = S.Count;
            int tail = id;
            S.Add(tail);

            //Sigma
            var Sigma = new HashSet<Terminal>();
            Sigma.Add(t);

            //Mapping
            var MappingTable = new HashSet<(int s1, Terminal t, int s2)>();
            MappingTable.Add((head, t, tail));

            //Z
            var Z = new HashSet<int>();
            Z.Add(tail);

            //S_0
            var S_0 = head;

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        //{eps}
        public static NFA CreateEpsilon()
        {
            //S
            var S = new HashSet<int>();
            int id = S.Count;
            int head = id;
            S.Add(head);
            id = S.Count;
            int tail = id;
            S.Add(tail);

            //Sigma
            var Sigma = new HashSet<Terminal>();
            Sigma.Add(EPSILON);

            //Mapping
            var MappingTable = new HashSet<(int s1, Terminal t, int s2)>();
            MappingTable.Add((head, EPSILON, tail));

            //Z
            var Z = new HashSet<int>();
            Z.Add(tail);

            //S_0
            var S_0 = head;

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        //[{from}-{to}]
        public static NFA CreateRange(char from, char to)
        {
            Debug.Assert(from <= to);
            NFA dig = CreateFrom(from.ToString());
            for (char c = (char)(from + 1); c <= to; c++)
            {
                var newDig = CreateFrom(c.ToString());
                dig = dig.Or(newDig);
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
            int snd_base_id = S.Count;
            S.UnionWith(snd.S.Select(i => snd_base_id + i));

            //Sigma
            var Sigma = new HashSet<Terminal>();
            Sigma.UnionWith(fst.Sigma);
            Sigma.UnionWith(snd.Sigma);

            //Mapping
            var MappingTable = fst.MappingTable.Select(i => (i.s1, i.t, i.s2))
                .Union(snd.MappingTable.Select(i => (snd_base_id + i.s1, i.t, snd_base_id + i.s2))).ToHashSet();

            //Z
            var Z = new HashSet<int>();
            Z.UnionWith(snd.Z.Select(i => snd_base_id + i));

            //S_0
            var S_0 = fst.S_0;

            //Join
            foreach (var s1 in fst.Z)
            {
                MappingTable.Add((s1, FA.EPSILON, snd_base_id + snd.S_0));
            }

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        //{fst}|{snd}
        public static NFA Or(this NFA fst, NFA snd)
        {
            //S
            var S = new HashSet<int>();
            int head = S.Count;
            S.Add(head);
            int fst_base_id = S.Count;
            S.UnionWith(fst.S.Select(s => fst_base_id + s));
            int snd_base_id = S.Count;
            S.UnionWith(snd.S.Select(s => snd_base_id + s));
            int tail = S.Count;
            S.Add(tail);

            //Sigma
            var Sigma = new HashSet<Terminal>();
            Sigma.UnionWith(fst.Sigma);
            Sigma.UnionWith(snd.Sigma);

            //Mapping
            var MappingTable = fst.MappingTable.Select(i => (fst_base_id + i.s1, i.t, fst_base_id + i.s2))
                .Union(snd.MappingTable.Select(i => (snd_base_id + i.s1, i.t, snd_base_id + i.s2)))
                .ToHashSet();

            //Z
            var Z = new HashSet<int>();
            Z.Add(tail);

            //S_0
            var S_0 = head;

            //Or
            MappingTable.Add((head, FA.EPSILON, fst_base_id + fst.S_0));
            MappingTable.Add((head, FA.EPSILON, snd_base_id + snd.S_0));
            foreach (var s in fst.Z
                .Select(s => fst_base_id + s)
                .Union(snd.Z.Select(s => snd_base_id + s)))
            {
                MappingTable.Add((s, FA.EPSILON, tail));
            }

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        //{dig}*
        public static NFA Closure(this NFA dig)
        {
            //S
            var S = new HashSet<int>();
            int head = S.Count;
            S.Add(head);
            int dig_base_id = S.Count;
            S.UnionWith(dig.S.Select(s => dig_base_id + s));
            int tail = S.Count;
            S.Add(tail);

            //Sigma
            var Sigma = new HashSet<Terminal>();
            Sigma.UnionWith(dig.Sigma);

            //Mapping
            var MappingTable = dig.MappingTable.Select(i => (dig_base_id + i.s1, i.t, dig_base_id + i.s2))
                .ToHashSet();

            //Z
            var Z = new HashSet<int>();
            Z.Add(tail);

            //S_0
            var S_0 = head;

            //Union
            MappingTable.Add((head, FA.EPSILON, tail));
            MappingTable.Add((head, FA.EPSILON, dig_base_id + dig.S_0));
            foreach (var s in dig.Z)
            {
                MappingTable.Add((dig_base_id + s, FA.EPSILON, tail));
                MappingTable.Add((dig_base_id + s, FA.EPSILON, dig_base_id + dig.S_0));
            }

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        public static NFA UnionNFA(this NFA fst, NFA snd)
        {
            //S
            var S = new HashSet<int>();
            int head = S.Count;
            S.Add(head);
            int fst_base_id = S.Count;
            S.UnionWith(fst.S.Select(s => fst_base_id + s));
            int snd_base_id = S.Count;
            S.UnionWith(snd.S.Select(s => snd_base_id + s));

            //Sigma
            var Sigma = new HashSet<Terminal>();
            Sigma.UnionWith(fst.Sigma);
            Sigma.UnionWith(snd.Sigma);

            //Mapping
            var MappingTable = fst.MappingTable.Select(i => (fst_base_id + i.s1, i.t, fst_base_id + i.s2))
                .Union(snd.MappingTable.Select(i => (snd_base_id + i.s1, i.t, snd_base_id + i.s2)))
                .ToHashSet();

            //Z
            var Z = new HashSet<int>();
            Z.UnionWith(fst.Z.Select(s => fst_base_id + s));
            Z.UnionWith(snd.Z.Select(s => snd_base_id + s));

            //S_0
            var S_0 = head;

            //Union
            MappingTable.Add((head, FA.EPSILON, fst_base_id + fst.S_0));
            MappingTable.Add((head, FA.EPSILON, snd_base_id + snd.S_0));

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        public static NFA SingleZNFA(this NFA dig)
        {
            //S
            var S = new HashSet<int>();
            S.UnionWith(dig.S);
            var tail = S.Count;
            if (dig.Z.Count() > 0)
                S.Add(tail);

            //Sigma
            var Sigma = new HashSet<Terminal>();
            Sigma.UnionWith(dig.Sigma);

            //Mapping
            var MappingTable = dig.MappingTable.ToHashSet();

            //Z
            var Z = new HashSet<int>();
            if (dig.Z.Count() > 0)
                Z.Add(tail);
            else Z.UnionWith(dig.Z);

            //S_0
            var S_0 = dig.S_0;

            //SingleZ
            if (dig.Z.Count() > 0)
            {
                foreach (var s in dig.Z)
                {
                    MappingTable.Add((s, FA.EPSILON, tail));
                }
            }

            return new(S, Sigma, MappingTable, S_0, Z);
        }
    }
}
