using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public abstract class FA
    {
        public static Terminal EPSILON = new Terminal("eps");

        public FA(IEnumerable<int> S, IEnumerable<Terminal> Sigma, IEnumerable<(int s1, Terminal t, int s2)> MappingTable, int S_0, IEnumerable<int> Z)
        {
            _S = S.ToHashSet();
            _Sigma = Sigma.ToHashSet();
            _MappingTable = MappingTable.ToHashSet();
            this.S_0 = S_0;
            _Z = Z.ToHashSet();
        }

        private HashSet<int> _S = new();
        private HashSet<Terminal> _Sigma = new();
        private HashSet<(int s1, Terminal t, int s2)> _MappingTable = new();
        private HashSet<int> _Z = new();
        public int S_0 { get; private set; }

        public IEnumerable<int> S { get => _S.AsEnumerable(); }
        public IEnumerable<Terminal> Sigma { get => _Sigma.AsEnumerable(); }
        public IEnumerable<(int s1, Terminal t, int s2)> MappingTable { get => _MappingTable.AsEnumerable(); }
        public IEnumerable<int> Z { get => _Z.AsEnumerable(); }

        #region ToString
        protected const string PRE = "    ";
        public override string ToString()
        {
            StringBuilder builder = new();
            NameToString(builder);
            builder.Append("{").AppendLine();
            SToString(builder);
            SigmaToString(builder);
            MappingToString(builder);
            S0ToString(builder);
            ZToString(builder);
            builder.Append("}").AppendLine();
            return builder.ToString();
        }
        protected virtual void NameToString(StringBuilder builder)
        {
            builder.Append(this.GetType().Name).AppendLine();
        }
        protected virtual void SToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"S : {{");
            foreach (var s in S)
            {
                builder.Append($" {s},");
            }
            builder.Length -= 1;
            builder.Append(" }").AppendLine();
        }
        protected virtual void SigmaToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"Sigma : {{");
            foreach (var t in Sigma)
            {
                builder.Append($" {t},");
            }
            builder.Length -= 1;
            builder.Append(" }").AppendLine();
        }
        protected virtual void MappingToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("Mapping :").AppendLine();
            builder.Append(PRE).Append("{").AppendLine();
            foreach (var pGroup in MappingTable.GroupBy(i => (i.s1)).OrderBy(g => g.Key))
            {
                builder.Append(PRE).Append(PRE);
                foreach (var p in pGroup.OrderBy(p => p.t, TerminalComparer.Default))
                {
                    builder.Append($"({p.s1}, {p.t}) = {p.s2}, ");
                }
                builder.Length -= 2;
                builder.AppendLine();
            }
            builder.Append(PRE).Append("}").AppendLine();
        }
        protected virtual void S0ToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"S_0 : {S_0}").AppendLine();
        }
        protected virtual void ZToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"Z : {{");
            foreach (var s in Z)
            {
                builder.Append($" {s},");
            }
            if (Z.Count() > 0)
                builder.Length -= 1;
            builder.Append(" }").AppendLine();
        }
        #endregion ToString
    }

    class TerminalComparer : IComparer<Terminal>
    {
        public static IComparer<Terminal> Default { get; private set; }
        static TerminalComparer() { Default = new TerminalComparer(); }
        int IComparer<Terminal>.Compare(Terminal? x, Terminal? y)
        {
            return x.GetHashCode() - y.GetHashCode();
        }
    }
}
