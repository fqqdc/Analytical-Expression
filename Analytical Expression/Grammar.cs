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

            CalculateFollowSet();

            SortNonTerminal();
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

        public HashSet<Terminal> GetFirstSet(NonTerminal nonTerminal)
        {
            if (mapFirst.TryGetValue(nonTerminal, out var set))
            {
                return set.ToHashSet();
            }
            return new();
        }
        public HashSet<Terminal> GetFollowSet(NonTerminal nonTerminal)
        {
            if (mapFollow.TryGetValue(nonTerminal, out var set))
            {
                return set.ToHashSet();
            }
            return new();
        }
        public HashSet<Terminal> GetFirstSet(Symbol[] symbols)
        {
            var set = new HashSet<Terminal>();
            set.Add(Grammar.Epsilon);
            foreach (var s in symbols)
            {
                if (s is Terminal terminal)
                {
                    set.Add(terminal);
                    set.Remove(Grammar.Epsilon);
                    return set;
                }
                else if (s is NonTerminal nonTerminal)
                {
                    set.UnionWith(GetFirstSet(nonTerminal));
                    if (!nullableSet.Contains(nonTerminal))
                    {
                        set.Remove(Grammar.Epsilon);
                        return set;
                    }
                }
            }
            return set;
        }
        public HashSet<Terminal> GetSelectSet(Production p)
        {
            var set = GetFirstSet(p.Right);
            if (set.Contains(Grammar.Epsilon))
            {
                set.UnionWith(GetFollowSet(p.Left));
            }
            return set;
        }

        private void CalculateNullableSet()
        {
            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;
                var oldNullableSet = nullableSet.ToHashSet();
                foreach (var p in P)
                {
                    if (p.Right.Length == 0)
                        nullableSet.Add(p.Left);
                    else if (p.Right.All(s => nullableSet.Contains(s)))
                        nullableSet.Add(p.Left);
                }
                hasChanged = !oldNullableSet.SetEquals(nullableSet);
            }
        }
        private void CalculateFirstSet()
        {
            CalculateNullableSet();
            foreach (var s in P.Select(p=>p.Left).Union(P.SelectMany(p=>p.Right)).Distinct())
            {
                if(s is NonTerminal nonTerminal)
                    mapFirst[nonTerminal] = new();
            }

            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;
                foreach (var p in P)
                {
                    var leftFirst = mapFirst[p.Left];
                    var old_leftFirst = leftFirst.ToHashSet();
                    if (p.Right.Length == 0)
                    {
                        leftFirst.Add(Grammar.Epsilon);
                    }
                    else
                    {
                        foreach (var s in p.Right)
                        {
                            if (s is Terminal terminal)
                            {
                                leftFirst.Add(terminal);
                            }

                            else if (s is NonTerminal nonTerminal)
                            {
                                var rightFirst = mapFirst[nonTerminal];
                                leftFirst.UnionWith(rightFirst.Where(s => s != Grammar.Epsilon));
                            }
                            if (!nullableSet.Contains(s))
                                break;
                        }
                    }
                    hasChanged = hasChanged || !leftFirst.SetEquals(old_leftFirst);
                }
            }
        }
        private void CalculateFollowSet()
        {
            CalculateFirstSet();
            mapFollow[S] = new();
            mapFollow[S].Add(Grammar.EndToken);

            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;
                foreach (var p in P)
                {
                    if (!mapFollow.TryGetValue(p.Left, out var leftFollow))
                    {
                        leftFollow = new();
                        mapFollow[p.Left] = leftFollow;
                        hasChanged = hasChanged || true;
                    }

                    HashSet<Terminal> set = leftFollow.ToHashSet();
                    foreach (var s in p.Right.Reverse())
                    {
                        if (s is Terminal terminal)
                        {
                            set = new();
                            set.Add(terminal);
                        }
                        else if (s is NonTerminal nonTerminal)
                        {
                            if (!mapFollow.TryGetValue(nonTerminal, out var sFollow))
                            {
                                sFollow = new();
                                mapFollow[nonTerminal] = sFollow;
                                hasChanged = hasChanged || true;
                            }

                            var old_sFollow = sFollow.ToHashSet();
                            sFollow.UnionWith(set);
                            hasChanged = hasChanged || !old_sFollow.SetEquals(sFollow);

                            if (nullableSet.Contains(nonTerminal))
                                set.UnionWith(mapFirst[nonTerminal].Where(s => s != Grammar.Epsilon));
                            else
                                set = mapFirst[nonTerminal].ToHashSet();
                        }
                    }
                }
            }
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

                    var newLeft = new NonTerminal(p1.Left.Name + "'");
                    while (set.Union(unionSet).Select(p => p.Left).Contains(newLeft))
                        newLeft = new NonTerminal(newLeft.Name + "'");

                    foreach (var p2 in group)
                    {
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
                    HashSet<NonTerminal> loopVisited = new();
                    derivative.Add(p);
                    var group1 = derivative.ToArray();
                    bool needContinue = true;
                    while (needContinue)
                    {
                        needContinue = false;

                        if (group1.All(p => loopVisited.Contains(p.Right[0])))
                            continue;
                        loopVisited.UnionWith(group1.Select(p=>(NonTerminal)p.Right[0]));

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

                    NonTerminal newLeft = new(g1.Key.Name + "'");
                    while (this.P.Union(unionSet).Select(p => p.Left).Contains(newLeft))
                        newLeft = new NonTerminal(newLeft.Name + "'");

                    var groups2 = g1.GroupBy(p => p.Right[0]);
                    foreach (var g2 in groups2)
                    {
                        if (g2.Count() == 1)
                            continue;
                        exceptSet.UnionWith(g2);                        

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
            var set = FilterUnreachable(newSet, S);

            return new(set, S);

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
                            var p1 = set.Single(p => p.Left == n);

                            var newRight = p.Right.Take(i).Union(p1.Right).Union(p.Right.Skip(i + 1));
                            Production p2 = new(p.Left, newRight.ToArray());
                            set.Remove(p);
                            //set.Remove(p1);
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
        private List<NonTerminal> sortedList;
        private void SortNonTerminal()
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
            sortedList = visited.ToList();
        }
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
            NullableSetToString(builder);
            FirstSetToString(builder);
            FollowSetToString(builder);
            SelectSetToString(builder);
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
            foreach (var p in P.OrderBy(p => sortedList.IndexOf(p.Left))
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

        private void NullableSetToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("Nullable Set: {");
            foreach (var n in nullableSet)
            {
                builder.Append($" {n},");
            }
            builder.Length -= 1;
            builder.Append(" }");
            builder.AppendLine();
        }

        private void FirstSetToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("First Set:").AppendLine();
            builder.Append(PRE).Append("{").AppendLine();
            foreach (var n in Vn.OrderBy(n => sortedList.IndexOf(n)))
            {
                builder.Append(PRE).Append(PRE).Append($"{n} : {{");
                foreach (var s in GetFirstSet(n))
                {
                    builder.Append($" {s},");
                }
                builder.Length -= 1;
                builder.Append(" }");
                builder.AppendLine();
            }
            builder.Append(PRE).Append("}").AppendLine();
        }

        private void FollowSetToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("Follow Set:").AppendLine();
            builder.Append(PRE).Append("{").AppendLine();
            foreach (var n in Vn.OrderBy(n => sortedList.IndexOf(n)))
            {
                builder.Append(PRE).Append(PRE).Append($"{n} : {{");
                foreach (var s in GetFollowSet(n))
                {
                    builder.Append($" {s},");
                }
                builder.Length -= 1;
                builder.Append(" }");
                builder.AppendLine();
            }
            builder.Append(PRE).Append("}").AppendLine();
        }

        private void SelectSetToString(StringBuilder builder)
        {
            builder.Append(PRE).Append("Select Set:").AppendLine();
            builder.Append(PRE).Append("{").AppendLine();
            foreach (var p in P.OrderBy(p => sortedList.IndexOf(p.Left))
                .ThenByDescending(p => p.Right.Length))
            {
                builder.Append(PRE).Append(PRE).Append($"{p} : {{");
                foreach (var s in GetSelectSet(p))
                {
                    builder.Append($" {s},");
                }
                builder.Length -= 1;
                builder.Append(" }");
                builder.AppendLine();
            }
            builder.Append(PRE).Append("}").AppendLine();
        }
        #endregion
    }
}
