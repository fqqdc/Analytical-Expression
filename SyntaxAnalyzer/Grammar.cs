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
            S = startNonTerminal;
        }

        private readonly HashSet<Terminal> _Vt;
        private readonly HashSet<NonTerminal> _Vn;
        private readonly HashSet<Production> _P;

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
                    groups[i].ExceptWith(exceptSet);
                    groups[i].UnionWith(unionSet);

                    isChanged = isChanged || exceptSet.Any() || unionSet.Any();
                }
            }

            //消除直接递归
            for (int i = 0; i < groups.Length; i++)
            {
                bool hasLeftRecursion = false; //是否存在左递归
                var left = groups[i].First().Left;
                var newLeft = new NonTerminal(left + "'");
                HashSet<Production> newGroup = new();
                foreach (var p in groups[i])
                {
                    if (p.Right.Any() && p.Left == p.Right.First())
                    {
                        newGroup.Add(new(newLeft, p.Right.Skip(1).Append(newLeft)));
                        hasLeftRecursion = true;
                    }
                    else
                    {
                        newGroup.Add(new(left, p.Right.Append(newLeft)));
                    }
                }
                if (hasLeftRecursion)
                {
                    newGroup.Add(new(newLeft, Production.Epsilon)); // N'->eps
                    groups[i] = newGroup;
                }
            }

            //移除不能抵达的生成式
            var newP = groups.SelectMany(g => g).ToHashSet();
            Queue<NonTerminal> queue = new();
            HashSet<NonTerminal> visited = new();
            queue.Enqueue(S);
            visited.Add(S);
            List<Production> reachableP = new();
            while (queue.Count > 0)
            {
                var nLeft = queue.Dequeue();
                foreach (var p in newP.Where(p => p.Left == nLeft))
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

        /// <summary>
        /// 提取左公因子
        /// </summary>
        public Grammar ExtractLeftCommonfactor()
        {
            HashSet<Production> oldSet, newSet;
            newSet = P.ToHashSet();
            do
            {
                oldSet = newSet.ToHashSet();
                var groups = oldSet.Where(p => p.Right.Any()).GroupBy(p => p.Left);

                foreach (var group in groups)
                {
                    if (!group.Skip(1).Any()) //group.Count() > 1
                        continue;

                    NonTerminal newLeft = new(group.Key.Name + "'");
                    while (newSet.Any(p => p.Left == newLeft))
                        newLeft = new NonTerminal(newLeft.Name + "'");

                    var subGroups2 = group.GroupBy(p => p.Right.First());
                    foreach (var subGroup in subGroups2)
                    {
                        if (!subGroup.Skip(1).Any()) //subGroup.Count() > 1
                            continue;
                        newSet.ExceptWith(subGroup);
                        newSet.Add(new(group.Key, new Symbol[] { subGroup.Key, newLeft }));
                        newSet.UnionWith(subGroup.Select(p =>
                        {
                            var newRight = p.Right.Skip(1);
                            if (!newRight.Any())
                                newRight = Production.Epsilon;
                            return new Production(newLeft, newRight);
                        }));
                    }
                }

            } while (!newSet.SetEquals(oldSet));

            CombineSingleProduction(oldSet, S);
            return new(oldSet, S);

            static void CombineSingleProduction(HashSet<Production> set, NonTerminal start)
            {
                Queue<Symbol> queue = new();
                queue.Enqueue(start);
                HashSet<Symbol> visited = new();
                visited.Add(start);

                while (queue.Count > 0)
                {
                    var symbol = queue.Dequeue();
                    var productions = set.Where(p => p.Left == symbol && p.Right.Any())
                        .Where(p => p.Right.Last() is NonTerminal)
                        .ToArray();

                    foreach (var prod in productions)
                    {
                        Production pTemp = prod;
                        var lastSymbol = pTemp.Right.Last();
                        bool continueLoop = !visited.Contains(lastSymbol);

                        set.Remove(pTemp);
                        while (continueLoop)
                        {
                            continueLoop = false;

                            if (set.Count(p => p.Left == lastSymbol) == 1)
                            {
                                var p2 = set.Where(p => p.Left == lastSymbol).Single();                                
                                set.Remove(p2);
                                pTemp = new(pTemp.Left, pTemp.Right.SkipLast(1).Union(p2.Right));

                                lastSymbol = pTemp.Right.Last();
                                continueLoop = !visited.Contains(lastSymbol);
                            }
                        }
                        set.Add(pTemp);

                        pTemp.Right.Where(s => s is NonTerminal && !visited.Contains(s))
                            .ToList().ForEach(s =>
                            {
                                queue.Enqueue(s);
                                visited.Add(s);
                            });
                    }
                }




            }
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
