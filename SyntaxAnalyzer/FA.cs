using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxAnalyzer
{
    public abstract class FA
    {
        public FA(IEnumerable<int> S, IEnumerable<Symbol> Sigma, IEnumerable<(int s1, Symbol symbol, int s2)> MappingTable, IEnumerable<int> Z)
        {
            _S = S.ToHashSet();
            _Sigma = Sigma.ToHashSet();
            _MappingTable = MappingTable.ToHashSet();
            _Z = Z.ToHashSet();
        }

        public const char CHAR_Epsilon = '\0';
        private const string STRING_Epsilon = "ε";

        protected HashSet<int> _S = new();
        protected HashSet<Symbol> _Sigma = new();
        protected HashSet<(int s1, Symbol symbol, int s2)> _MappingTable = new();
        protected HashSet<int> _Z = new();

        public IEnumerable<int> S { get => _S.AsEnumerable(); }
        public IEnumerable<Symbol> Sigma { get => _Sigma.AsEnumerable(); }
        public IEnumerable<(int s1, Symbol symbol, int s2)> MappingTable { get => _MappingTable.AsEnumerable(); }
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
                foreach (var p in pGroup.OrderBy(p => p.symbol.ToString()).ThenBy(p => p.s2))
                {
                    string strChar = p.symbol.ToString();
                    if (p.symbol == Terminal.Epsilon)
                        strChar = STRING_Epsilon;

                    builder.Append($"({p.s1}, {strChar}) = {p.s2}, ");
                }
                builder.Length -= 2;
                builder.AppendLine();
            }
            builder.Append(PRE).Append("}").AppendLine();
        }
        protected abstract void S0ToString(StringBuilder builder);
        protected virtual void ZToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"Z : {{");
            foreach (var s in Z)
            {
                builder.Append($" {s},");
            }
            if (Z.Any())
                builder.Length -= 1;
            builder.Append(" }").AppendLine();
        }
        #endregion ToString
    }
}
