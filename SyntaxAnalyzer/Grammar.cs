using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxAnalyzer
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

        private Dictionary<NonTerminal, HashSet<Terminal>> mapFirst = new();
        private Dictionary<NonTerminal, HashSet<Terminal>> mapFollow = new();
        private HashSet<NonTerminal> nullableSet = new();
        private HashSet<Terminal> _Vt = new();
        private HashSet<NonTerminal> _Vn = new();
        private HashSet<Production> _P = new();

        public IEnumerable<Terminal> Vt { get => _Vt.AsEnumerable(); }
        public IEnumerable<NonTerminal> Vn { get => _Vn.AsEnumerable(); }
        public IEnumerable<Production> P { get => _P.AsEnumerable(); }
        public NonTerminal S { get; private set; }

        /// <summary>
        /// 消除左递归
        /// </summary>
        public Grammar EliminateLeftRecursion()
        {
            var groups = P.GroupBy(p => p.Left).Select(g => g.ToHashSet())
                .ToArray();

            //消除间接递归
            for (int i = 0; i < groups.Length; i++)
            {
                bool isChanged = true;
                while (isChanged)
                {
                    isChanged = false;

                    List<Production> exceptSet = new();
                    List<Production> unionSet = new();
                    foreach (var p in groups[i])
                    {
                        if (!p.Right.Any())
                            continue;
                        var fstSymbol = p.Right.ElementAt(0);
                        if (fstSymbol is NonTerminal nonTerminal)
                        {
                            for (int j = 0; j < i; j++)
                            {
                                var key = groups[j].First().Left;
                                if (nonTerminal == key)
                                {
                                    exceptSet.Add(p);
                                    foreach (var pReplaced in groups[j])
                                    {
                                        var newRight = Enumerable.Empty<Symbol>();
                                        if (pReplaced.Right != Production.Epsilon)
                                            newRight = pReplaced.Right;
                                        newRight = newRight.Union(p.Right.Skip(1));
                                        unionSet.Add(new(p.Left, newRight));
                                    }
                                }
                            }
                        }
                    }

                    isChanged = isChanged || exceptSet.Any() || unionSet.Any();

                    groups[i].ExceptWith(exceptSet);
                    groups[i].UnionWith(unionSet);
                }
            }

            //消除直接递归
            for (int i = 0; i < groups.Length; i++)
            {
                //存在左递归
                if (groups[i].Any(p => p.Right.Any() && p.Left == p.Right.First()))
                {
                    HashSet<Production> newP = new();

                    var subGroups = groups[i].GroupBy(p => p.Right.Any() && p.Left == p.Right.First());
                    var left = groups[i].First().Left;
                    var newLeft = new NonTerminal(left + "'");
                    foreach (var subGroup in subGroups)
                    {
                        if (subGroup.Key)
                        {
                            foreach (var p in subGroup)
                                newP.Add(new(newLeft, p.Right.Skip(1).Append(newLeft)));
                            newP.Add(new(newLeft, Production.Epsilon));
                        }
                        else
                        {
                            foreach (var p in subGroup)
                                newP.Add(new(left, p.Right.Append(newLeft)));
                        }
                    }
                    groups[i] = newP;
                }
            }

            //移除不能抵达的生成式
            Queue<NonTerminal> queue = new();
            HashSet<NonTerminal> visited = new();
            queue.Enqueue(S);
            visited.Add(S);
            List<Production> reachableP = new();
            while (queue.Count > 0)
            {
                var nLeft = queue.Dequeue();
                foreach (var p in groups.SelectMany(g => g).Where(p => p.Left == nLeft))
                {
                    reachableP.Add(p);
                    foreach (var n in p.Right.Where(n => n is NonTerminal).Cast<NonTerminal>())
                    {
                        if (!visited.Contains(n))
                        {
                            visited.Add(n);
                            queue.Enqueue(n);
                        }
                    }
                }
            }

            return new(reachableP, S);
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
            foreach (var n in Vn)
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
