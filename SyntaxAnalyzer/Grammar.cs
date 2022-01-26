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
        /// 重新排序
        /// </summary>
        private static Production[] Reorder(HashSet<Production> set, NonTerminal S)
        {
            return set.ToArray();
        }

        /// <summary>
        /// 移除不能抵达的生成式
        /// </summary>
        private static HashSet<Production> FilterUnreachable(HashSet<Production> set, NonTerminal S)
        {
            Queue<NonTerminal> queue = new();
            HashSet<NonTerminal> visited = new();
            queue.Enqueue(S);
            visited.Add(S);
            HashSet<Production> newSet = new();
            while (queue.Count > 0)
            {
                var nLeft = queue.Dequeue();
                foreach (var p in set.Where(p => p.Left == nLeft))
                {
                    newSet.Add(p);
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
            return newSet;
        }


        /// <summary>
        /// 消除左递归
        /// </summary>
        public Grammar EliminateLeftRecursion()
        {
            throw new NotImplementedException();

            HashSet<Production> newP = new();
            var groups = P.GroupBy(p => p.Left).ToArray();            

            //消除间接递归
            for (int i = 0; i < groups.Length; i++)
            {
                foreach (var p in groups[i])
                {
                    if (!p.Right.Any())
                        continue;
                    var fstSymbol = p.Right.ElementAt(0);
                    bool hasReplaced = false;

                    if (fstSymbol is NonTerminal nonTerminal)
                    {
                        for (int j = 0; j < i - 1; j++)
                        {
                            if (nonTerminal == groups[j].Key)
                            {
                                hasReplaced = true;
                                foreach (var pReplaced in groups[j])
                                {
                                    var newRight = Enumerable.Empty<Symbol>();
                                    if (pReplaced.Right != Production.Epsilon)
                                        newRight = pReplaced.Right;
                                    newRight = newRight.Union(p.Right.Skip(1));
                                    newP.Add(new(p.Left, newRight));
                                }
                            }
                        }
                    }

                    if (!hasReplaced)
                        newP.Add(p);
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
