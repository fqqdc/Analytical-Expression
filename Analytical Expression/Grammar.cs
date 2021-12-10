using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class Grammar
    {
        public static Terminal Epsilon { get; private set; } = new("\"eps\"");
        public static Terminal EndToken { get; private set; } = new("\"end\"");
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

        /// <summary>
        /// 消除左递归
        /// </summary>
        public Grammar EliminateLeftRecursion()
        {
            var set = P.ToHashSet();
            EliminateDirectLeftRecursion(set);
            ConvertIndirectLeftRecursion(set);
            set = FilterUnreachable(set, S);
            EliminateDirectLeftRecursion(set);

            return new(set, S);

            static void EliminateDirectLeftRecursion(HashSet<Production> set)
            {
                var recursions = set.Where(p => p.Right.Length > 0)
                        .Where(p => p.Left == p.Right[0]);

                HashSet<Production> exceptSet = new();
                HashSet<Production> unionSet = new();
                foreach (var p1 in recursions)
                {
                    var group = set.Where(p => p.Left == p1.Left);
                    foreach (var p2 in group)
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
                }
                set.ExceptWith(exceptSet);
                set.UnionWith(unionSet);
            }
            static void ConvertIndirectLeftRecursion(HashSet<Production> set)
            {
                var pending = set
                    .Where(p => p.Right.Length > 0)
                    .Where(p => p.Right[0] is NonTerminal)
                    .Where(p => p.Left != p.Right[0]);
                HashSet<Production> exceptSet = new();
                HashSet<Production> unionSet = new();

                foreach (var p in pending)
                {
                    HashSet<Production> derivative = new();
                    derivative.Add(p);
                    var group1 = derivative.ToArray();
                    bool needContinue = true;
                    while (needContinue)
                    {
                        needContinue = false;
                        foreach (var p1 in group1)
                        {
                            derivative.Remove(p1);
                            var group2 = set
                                .Where(p => p.Left == p1.Right[0]);
                            foreach (var p2 in group2)
                            {
                                var newRight = p2.Right.Union(p1.Right.Skip(1)).ToArray();
                                derivative.Add(new(p1.Left, newRight));
                            }
                        }

                        group1 = derivative
                            .Where(p => p.Right.Length > 0)
                            .Where(p => p.Right[0] is NonTerminal)
                            .Where(p => p.Left != p.Right[0])
                            .ToArray();
                        needContinue = group1.Length > 0;
                    }

                    bool hasRecursion = derivative.Any(p => p.Right.Length > 0 && p.Right[0] == p.Left);
                    if (hasRecursion)
                    {
                        exceptSet.Add(p);
                        unionSet.UnionWith(derivative);
                    }
                }

                set.ExceptWith(exceptSet);
                set.UnionWith(unionSet);
            }
            static HashSet<Production> FilterUnreachable(HashSet<Production> set, NonTerminal S)
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
        }

        /// <summary>
        /// 提取左公因子
        /// </summary>
        public Grammar ExtractLeftCommonfactor()
        {
            HashSet<Production> oldSet;
            var newSet = P.ToHashSet();
            do
            {
                oldSet = newSet;
                HashSet<Production> exceptSet = new();
                HashSet<Production> unionSet = new();
                var groups1 = oldSet.Where(p => p.Right.Length > 0).GroupBy(p => p.Left);
                foreach (var g1 in groups1)
                {
                    if (g1.Count() == 1)
                        continue;

                    var groups2 = g1.GroupBy(p => p.Right[0]);
                    foreach (var g2 in groups2)
                    {
                        if (g2.Count() == 1)
                            continue;
                        exceptSet.UnionWith(g2);
                        NonTerminal newLeft = new(g1.Key.Name + "'");
                        unionSet.Add(new(g1.Key, new Symbol[] { g2.Key, newLeft }));
                        foreach (var p in g2)
                        {
                            unionSet.Add(new(newLeft, p.Right.Skip(1).ToArray()));
                        }
                    }
                }

                newSet = oldSet.ToHashSet();
                newSet.ExceptWith(exceptSet);
                newSet.UnionWith(unionSet);

            } while (!newSet.SetEquals(oldSet));

            CombineSingleProduction(newSet);

            return new(newSet, S);

            static void CombineSingleProduction(HashSet<Production> set)
            {
                bool hasChanged = true;
                while (hasChanged)
                {
                    hasChanged = false;
                    foreach (var p in set)
                    {
                        for (int i = 0; i < p.Right.Length; i++)
                        {
                            var n = p.Right[i] as NonTerminal;
                            if (n == null)
                                continue;
                            var count = set.Count(p => p.Left == n);
                            if (count != 1)
                                continue;
                            var p1 = set.First(p => p.Left == n);

                            var newRight = p.Right.Take(i).Union(p1.Right).Union(p.Right.Skip(i + 1));
                            Production p2 = new(p.Left, newRight.ToArray());
                            set.Remove(p);
                            set.Remove(p1);
                            set.Add(p2);
                            hasChanged = true;
                            break;
                        }
                        if (hasChanged)
                            break;
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
            HashSet<NonTerminal> visited = new();
            Queue<NonTerminal> queue = new();
            queue.Enqueue(S);
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                if (visited.Contains(n))
                    continue;
                visited.Add(n);
                foreach (var p in P.Where(p => p.Left == n))
                {
                    foreach (var s in p.Right)
                    {
                        if (s is NonTerminal nonTerminal)
                            queue.Enqueue(nonTerminal);
                    }
                }
            }
            var list = visited.ToList();

            builder.Append(PRE).Append("Productions :").AppendLine();
            builder.Append(PRE).Append("{").AppendLine();
            foreach (var p in P.OrderBy(p => list.IndexOf(p.Left))
                .ThenByDescending(p => p.Right.Length))
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
