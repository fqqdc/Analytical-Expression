using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyntaxAnalyzer
{
    public class Grammar
    {
        public Grammar(IEnumerable<Production> allProduction, NonTerminal startNonTerminal)
        {
            var symbols = allProduction.SelectMany(p => p.Right.Append(p.Left))
                .Except(Production.Epsilon).ToHashSet();
            _Vn = symbols.Where(s => s is NonTerminal).Cast<NonTerminal>().ToHashSet();
            _Vt = symbols.Where(s => s is Terminal).Cast<Terminal>().ToHashSet();
            _P = allProduction.ToHashSet();

            var leftVn = allProduction.Select(p => p.Left).ToHashSet();
            if (!leftVn.Contains(startNonTerminal))
                throw new NotSupportedException($"无效的起始符:{startNonTerminal}");

            S = startNonTerminal;
        }

        private readonly HashSet<Terminal> _Vt;
        private readonly HashSet<NonTerminal> _Vn;
        private readonly HashSet<Production> _P;

        public IEnumerable<Terminal> Vt { get => _Vt.AsEnumerable(); }
        public IEnumerable<NonTerminal> Vn { get => _Vn.AsEnumerable(); }
        public IEnumerable<Production> P { get => _P.AsEnumerable(); }
        public NonTerminal S { get; private set; }




        #region ToString()
        protected const string PRE = "    ";

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(GetType().Name).AppendLine();
            VtToString(builder);
            VnToString(builder);
            PToString(builder);
            SToString(builder);
            return builder.ToString();
        }

        public string ToFullString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(ToString());
            return builder.ToString();
        }

        private void VtToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("Terminals : {");
            foreach (var t in Vt)
            {
                builder.Append($" {t},");
            }
            if (Vt.Any())
                builder.Length -= 1;
            builder.Append(" }");
            builder.AppendLine();
        }

        private void VnToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("NonTerminal : {");
            foreach (var n in Vn.OrderBy(n => n != S))
            {
                builder.Append($" {n},");
            }
            if (Vn.Any())
                builder.Length -= 1;
            builder.Append(" }");
            builder.AppendLine();
        }

        private void PToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("Productions :").AppendLine();
            builder.Append(PRE).Append("{").AppendLine();
            foreach (var p in P)
            {
                builder.Append(PRE).Append(PRE).Append(p).AppendLine();
            }
            builder.Append(PRE).Append("}").AppendLine();
        }

        private void SToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"START : {S}").AppendLine();
        }



        #endregion
    }
}
