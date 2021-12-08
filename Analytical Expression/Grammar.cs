using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class Grammar
    {
        public Grammar(IEnumerable<Production> allProduction, NonTerminal startNonTerminal)
        {
            var symbols = allProduction.SelectMany(p => p.Right.Append(p.Left)).Distinct();
            _Vn.UnionWith(symbols.Where(s => s is NonTerminal).Cast<NonTerminal>());
            _Vt.UnionWith(symbols.Where(s => s is Terminal).Cast<Terminal>());
            _P.UnionWith(allProduction);
            S = startNonTerminal;
        }
        private HashSet<Terminal> _Vt = new();
        public IEnumerable<Terminal> Vt { get => _Vt.AsEnumerable(); }

        private HashSet<NonTerminal> _Vn = new();
        public IEnumerable<NonTerminal> Vn { get => _Vn.AsEnumerable(); }
        private HashSet<Production> _P = new();
        public IEnumerable<Production> P { get => _P.AsEnumerable(); }
        public NonTerminal S { get; private set; }

        public Grammar EliminateLeftRecursion()
        {
            var newP = P.ToHashSet();
            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;
                var p1 = newP.Where(p => p.Right.Length > 0)
                    .Where(p => p.Left == p.Right[0]).FirstOrDefault();
                if (p1 != null)
                {
                    hasChanged = true;
                    
                    HashSet<Production> exceptSet = new();
                    HashSet<Production> unionSet = new();
                    foreach (var p2 in newP.Where(p => p.Left == p1.Left))
                    {
                        var newLeft = new NonTerminal(p1.Left.Name + "'");
                        exceptSet.Add(p2);
                        if (p2.Right[0].Name == String.Empty)
                            continue;

                        if (p2.Left == p2.Right[0])
                        {
                            var newRight = p2.Right.Skip(1).Append(newLeft).ToArray();
                            unionSet.Add(new(newLeft, newRight));
                            unionSet.Add(new(newLeft, new Symbol[0]));
                        }
                        else
                        {                            
                            var newRight = p2.Right.Append(newLeft).ToArray();
                            unionSet.Add(new(p1.Left, newRight));
                        }
                    }
                    newP.ExceptWith(exceptSet);
                    newP.UnionWith(unionSet);
                }
            }

            return new(newP, S);
        }

        #region ToString()
        const string PRE = "    ";
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Grammar").AppendLine();
            VtToString(builder);
            VnToString(builder);
            PToString(builder);
            SToString(builder);
            return builder.ToString();
        }

        private void VtToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("Terminals : {");
            foreach (var t in Vt)
            {
                builder.Append($" {t},");
            }
            builder.Length -= 1;
            builder.Append(" }");
            builder.AppendLine();
        }

        private void VnToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("NonTerminal : {");
            foreach (var n in Vn)
            {
                builder.Append($" {n},");
            }
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
