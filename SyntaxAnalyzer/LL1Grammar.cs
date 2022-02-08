using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxAnalyzer
{
    public class LL1Grammar : Grammar
    {
        private LL1Grammar(IEnumerable<Production> allProduction, NonTerminal startNonTerminal) : base(allProduction, startNonTerminal)
        {
        }


        private Dictionary<NonTerminal, HashSet<Terminal>> mapFollow = new();

        /// <summary>
        /// 消除左递归
        /// </summary>
        public static Grammar EliminateLeftRecursion(Grammar grammar)
        {
            var (S, P) = (grammar.S, grammar.P);
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
                                        newRight = newRight.Concat(p.Right.Skip(1));
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
        public static Grammar ExtractLeftCommonfactor(Grammar grammar)
        {
            var (S, P) = (grammar.S, grammar.P);
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

            return new(oldSet, S);
        }

        public static void CreateFrom(Grammar grammar)
        {
            grammar = EliminateLeftRecursion(grammar);
            grammar = ExtractLeftCommonfactor(grammar);
            var (S, P) = (grammar.S, grammar.P);

            // FIRST集
            Dictionary<NonTerminal, HashSet<Terminal>> mapFirst = CalcFirsts(P);

            HashSet<Terminal> CalcFirst(IEnumerable<Symbol> alpha)
            {
                HashSet<Terminal> first = new();
                foreach (var symbol in alpha)
                {

                    if (symbol is Terminal terminal)
                    {
                        first.Add(terminal);
                        break;
                    }

                    if (symbol is NonTerminal nonTerminal)
                    {
                        if (!mapFirst.TryGetValue(nonTerminal, out var firstNonTerminal))
                        {
                            firstNonTerminal = new();
                        }

                        var elem = firstNonTerminal.ToHashSet();
                        elem.Remove(Terminal.Epsilon);
                        first.UnionWith(elem);

                        if (!firstNonTerminal.Contains(Terminal.Epsilon))
                            break;
                    }
                    first.Add(Terminal.Epsilon);
                }

                return first;
            }

            //FOLLOW集
            Dictionary<NonTerminal, HashSet<Terminal>> mapFollow = new();
            bool hasChanged = true;
            while (hasChanged)
            {
                foreach (var production in P)
                {
                    if (!mapFollow.TryGetValue(production.Left, out var follow))
                    {
                        follow = new();
                        mapFollow[production.Left] = follow;
                        if (production.Left == S)
                            follow.Add(Terminal.EndTerminal);
                        hasChanged = true;
                    }

                    var oldFollow = follow.ToHashSet();

                    foreach (var symbol in production.Right.Reverse())
                    {
                        if (symbol is Terminal terminal)
                        {
                            follow.Add(terminal);
                            break;
                        }

                        if (symbol is NonTerminal nonTerminal)
                        {
                            if (!mapFollow.TryGetValue(nonTerminal, out var followNonTerminal))
                            {
                                followNonTerminal = new();
                                mapFollow[production.Left] = followNonTerminal;
                                if (production.Left == S)
                                    followNonTerminal.Add(Terminal.EndTerminal);
                                hasChanged = true;
                            }

                            throw new NotImplementedException();
                        }
                    }
                }
            }
        }

        private static Dictionary<NonTerminal, HashSet<Terminal>> CalcFirsts(IEnumerable<Production>? P)
        {
            Dictionary<NonTerminal, HashSet<Terminal>> mapFirst = new();
            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;

                foreach (var production in P)
                {
                    if (!mapFirst.TryGetValue(production.Left, out var first))
                    {
                        first = new();
                        mapFirst[production.Left] = first;
                        hasChanged = true;
                    }

                    var oldFirst = first.ToHashSet();

                    foreach (var symbol in production.Right)
                    {

                        if (symbol is Terminal terminal)
                        {
                            first.Add(terminal);
                            break;
                        }

                        if (symbol is NonTerminal nonTerminal)
                        {
                            if (!mapFirst.TryGetValue(nonTerminal, out var firstNonTerminal))
                            {
                                firstNonTerminal = new();
                                mapFirst[nonTerminal] = firstNonTerminal;
                                hasChanged = true;
                            }

                            var elem = firstNonTerminal.ToHashSet();
                            elem.Remove(Terminal.Epsilon);
                            first.UnionWith(elem);

                            if (!firstNonTerminal.Contains(Terminal.Epsilon))
                                break;
                        }
                        first.Add(Terminal.Epsilon);
                    }

                    hasChanged = hasChanged || !first.SetEquals(oldFirst);
                }
            }

            StringBuilder builder = new StringBuilder();
            foreach (var kp in mapFirst)
            {
                builder.Append($"{kp.Key} => ");
                foreach (var t in kp.Value)
                {
                    builder.Append($"{t}, ");
                }
                builder.AppendLine();
            }
            Console.WriteLine(builder.ToString());
            return mapFirst;
        }

    }
}
