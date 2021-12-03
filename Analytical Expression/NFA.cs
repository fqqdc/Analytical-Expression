using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class NFA
    {
        public static Terminal EPSILON = new Terminal("eps");

        public NFA(IEnumerable<int> S, IEnumerable<Terminal> Sigma, IEnumerable<(int s1, Terminal t, int s2)> MappingTable, int S_0, IEnumerable<int> Z)
        {
            _S = S.ToHashSet();
            _Sigma = Sigma.ToHashSet();
            _MappingTable = MappingTable.ToHashSet();
            this.S_0 = S_0;
            _Z = Z.ToHashSet();
        }

        private HashSet<int> _S = new();
        public IEnumerable<int> S { get => _S.AsEnumerable(); }

        private HashSet<Terminal> _Sigma = new();
        public IEnumerable<Terminal> Sigma { get => _Sigma.AsEnumerable(); }

        private HashSet<(int s1, Terminal t, int s2)> _MappingTable = new();

        public HashSet<(int s1, Terminal t, int s2)> MappingTable { get => _MappingTable.ToHashSet(); }
        public Dictionary<(int s, Terminal t), HashSet<int>> MappingDictionary
        {
            get
            {
                return _MappingTable
                    .GroupBy(i => (i.s1, i.t))
                    .ToDictionary(g => g.Key, g => g.Select(i => i.s2).ToHashSet());
            }
        }

        public int S_0 { get; private set; }

        private HashSet<int> _Z = new();
        public IEnumerable<int> Z { get => _Z.AsEnumerable(); }

        //{t}
        public static NFA NFACreateFrom(Terminal t)
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


        #region ToString
        const string PRE = "    ";
        public override string ToString()
        {
            StringBuilder builder = new();
            builder.Append("NFA").AppendLine();
            builder.Append("{").AppendLine();
            SToString(builder);
            SigmaToString(builder);
            MappingToString(builder);
            S0ToString(builder);
            ZToString(builder);
            builder.Append("}").AppendLine();
            return builder.ToString();
        }
        public void SToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"S : {{");
            foreach (var s in S)
            {
                builder.Append($" {s},");
            }
            builder.Length -= 1;
            builder.Append(" }").AppendLine();
        }
        public void SigmaToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"Sigma : {{");
            foreach (var t in Sigma)
            {
                builder.Append($" {t},");
            }
            builder.Length -= 1;
            builder.Append(" }").AppendLine();
        }
        public void MappingToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("Mapping :").AppendLine();
            builder.Append(PRE).Append("{").AppendLine();
            foreach (var pGroup in _MappingTable.GroupBy(i => (i.s1)).OrderBy(g => g.Key))
            {
                builder.Append(PRE).Append(PRE);
                foreach (var p in pGroup.OrderBy(p => p.t))
                {
                    builder.Append($"f({p.s1}, {p.t}) = {p.s2}, ");
                }
                builder.Length -= 2;
                builder.AppendLine();
            }
            builder.Append(PRE).Append("}").AppendLine();
        }
        public void S0ToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"S_0 : {S_0}").AppendLine();
        }
        public void ZToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"Z : {{");
            foreach (var s in Z)
            {
                builder.Append($" {s},");
            }
            builder.Length -= 1;
            builder.Append(" }").AppendLine();
        }
        #endregion ToString
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
                MappingTable.Add((s1, NFA.EPSILON, snd_base_id + snd.S_0));
            }

            return new(S, Sigma, MappingTable, S_0, Z);
        }

        //{fst}|{snd}
        public static NFA Union(this NFA fst, NFA snd)
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

            //Union
            MappingTable.Add((head, NFA.EPSILON, fst_base_id + fst.S_0));
            MappingTable.Add((head, NFA.EPSILON, snd_base_id + snd.S_0));
            foreach (var s in fst.Z
                .Select(s => fst_base_id + s)
                .Union(snd.Z.Select(s => snd_base_id + s)))
            {
                MappingTable.Add((s, NFA.EPSILON, tail));
            }

            return new(S, Sigma, MappingTable, S_0, Z);
        }
    }
}
