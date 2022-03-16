using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SyntaxAnalyzer
{
    public class LL1Grammar : Grammar
    {
        private LL1Grammar(IEnumerable<Production> allProduction, NonTerminal startNonTerminal
            , Dictionary<NonTerminal, HashSet<Terminal>> mapFirst
            , Dictionary<NonTerminal, HashSet<Terminal>> mapFollow
            ) : base(allProduction, startNonTerminal)
        {
            this.mapFirst = mapFirst.ToDictionary(i => i.Key, i => i.Value.ToHashSet());
            this.mapFollow = mapFollow.ToDictionary(i => i.Key, i => i.Value.ToHashSet()); ;
        }

        private Dictionary<NonTerminal, HashSet<Terminal>> mapFirst;
        private Dictionary<NonTerminal, HashSet<Terminal>> mapFollow;

        public HashSet<Terminal> CalcFirst(IEnumerable<Symbol> alpha)
        {
            return CalcFirst(alpha, this.mapFirst);
        }

        public HashSet<Terminal> GetFirst(Symbol symbol)
        {
            if (symbol is Terminal terminal)
                return new(new Terminal[] { terminal });
            else
            {
                var nonTerminal = (NonTerminal)symbol;
                return mapFirst[nonTerminal].ToHashSet();
            }
        }
        public HashSet<Terminal> GetFollow(NonTerminal nonTerminal)
        {
            return mapFollow[nonTerminal].ToHashSet();

        }

        #region EliminateLeftRecursion 消除左递归

        /// <summary>
        /// 消除关于left的产生式的直接左递归
        /// </summary>
        private static void EliminateDirectLeftRecursion(HashSet<Production> productions, NonTerminal left)
        {
            var prods = productions.Where(p => p.Left == left).ToHashSet();

            if (!prods.Any())
                return;
            //if (prods.Any(p => p.Right.SequenceEqual(Production.Epsilon)))
            //    throw new NotSupportedException($"不能含有以ε为右边的产生式");
            if (prods.Any(p => p.Right.ElementAtOrDefault(1) == null && p.Right.ElementAt(0) == p.Left))
                throw new NotSupportedException($"产生式中不能含有回路");

            //左递归产生式
            var recProds = prods.Where(p => p.Right.First() == left).ToArray();
            var newLeft = new NonTerminal(left + "'"); // 新的产生式左部

            if (!recProds.Any()) return; //无左递归
            foreach (var p in recProds)
            {
                productions.Add(new(newLeft, p.Right.Skip(1).Append(newLeft)));
            }
            productions.Add(new(newLeft, Production.Epsilon));

            productions.ExceptWith(prods);
            prods.ExceptWith(recProds);

            if (prods.Any())
            {
                foreach (var p in prods)
                {
                    productions.Add(new(left, p.Right.Append(newLeft)));
                }
            }
            else
            {
                productions.Add(new(left, newLeft));
            }
        }

        /// <summary>
        /// 消除左递归
        /// </summary>
        private static IEnumerable<Production> EliminateLeftRecursion(IEnumerable<Production> P)
        {
            var setP = P.ToHashSet();
            var nonTerminals = P.Select(p => p.Left).Distinct().ToArray(); //产生固定的非终结符序列

            /// 消除间接左递归
            for (int i = 0; i < nonTerminals.Length; i++)
            {
                bool changed = true;
                while (changed)
                {
                    var iGroup = setP.Where(p => p.Left == nonTerminals[i]).ToHashSet();
                    var iGroupCopy = iGroup.ToHashSet();

                    List<Production> exceptSet = new();
                    List<Production> unionSet = new();

                    foreach (var p in iGroup)
                    {
                        var fstSymbol = p.Right.ElementAt(0);
                        if (fstSymbol is NonTerminal fstRight)
                        {
                            for (int j = 0; j < i; j++)
                            {
                                var jLeft = nonTerminals[j];
                                if (fstRight == jLeft)
                                {
                                    var jGroup = setP.Where(p => p.Left == nonTerminals[j]).ToHashSet();
                                    exceptSet.Add(p);
                                    foreach (var pReplaced in jGroup)
                                    {
                                        var newRight = Enumerable.Empty<Symbol>();
                                        if (!pReplaced.Right.SequenceEqual(Production.Epsilon))
                                            newRight = pReplaced.Right;
                                        newRight = newRight.Concat(p.Right.Skip(1));
                                        unionSet.Add(new(p.Left, newRight));
                                    }
                                }
                            }
                        }
                    }

                    setP.ExceptWith(exceptSet);
                    setP.UnionWith(unionSet);
                    iGroup = setP.Where(p => p.Left == nonTerminals[i]).ToHashSet();
                    changed = !iGroup.SetEquals(iGroupCopy);
                }
                /// 消除直接左递归
                EliminateDirectLeftRecursion(setP, nonTerminals[i]);
            }
            return setP;
        }

        /// <summary>
        /// 移除不能抵达的生成式
        /// </summary>
        /// <param name="start">开始符</param>
        private static void RemoveUnreachable(HashSet<Production> productions, NonTerminal start)
        {
            Queue<NonTerminal> queue = new();
            HashSet<NonTerminal> visited = new();
            queue.Enqueue(start);
            visited.Add(start);
            List<Production> reachableP = new();
            while (queue.Count > 0)
            {
                var nLeft = queue.Dequeue();
                foreach (var p in productions.Where(p => p.Left == nLeft))
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
        }

        #endregion

        /// <summary>
        /// 消除左递归
        /// </summary>
        public static Grammar EliminateLeftRecursion(Grammar grammar)
        {
            var prodSet = EliminateLeftRecursion(grammar.P).ToHashSet();
            RemoveUnreachable(prodSet, grammar.S);

            return new(prodSet, grammar.S);
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

                    var keyName = group.Key.Name;
                    if (group.Key.Name.Contains("_"))
                    {
                        var indexDelimiter = group.Key.Name.LastIndexOf("_");
                        keyName = keyName.Substring(0, indexDelimiter);
                    }

                    int index = 1;
                    NonTerminal newLeft = new($"{keyName}_{index}");
                    while (newSet.Any(p => p.Left == newLeft))
                    {
                        index += 1;
                        newLeft = new($"{keyName}_{index}");
                    }

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

        public static bool TryCreateLL1Grammar(Grammar grammar, [MaybeNullWhen(false)] out LL1Grammar lL1Grammar, out string errorMsg)
        {
            var (S, P) = (grammar.S, grammar.P);
            var mapFirst = CalcFirsts(P);
            var mapFollow = CalcFollows(P, mapFirst, S);
            StringBuilder stringBuilder = new();

            ///对于文法中每一个非终结符A的各个产生式的候选首符集两两不相交。
            ///即，若A->α1|α2|...|αn
            ///则 FIRST(αi) ∩ FIRST(αi)=∅ (i≠j)
            foreach (var pGroup in P.GroupBy(p => p.Left))
            {
                var pArray = pGroup.ToArray();
                for (int i = 0; i < pArray.Length; i++)
                {
                    var iFirst = CalcFirst(pArray[i].Right, mapFirst);
                    for (int j = i + 1; j < pArray.Length; j++)
                    {
                        var jFirst = CalcFirst(pArray[j].Right, mapFirst);
                        /// 如果产生的First交集中有ε，并不会造成冲突 
                        /// if (iFirst.Intersect(jFirst).Any())
                        if (iFirst.Intersect(jFirst).Any(t => t != Terminal.Epsilon))
                            stringBuilder.AppendLine($" {pArray[i]}右部的FIRST集与{pArray[j]}右部的FIRST集相交不为空，无法满足LL1文法。");
                    }
                }
            }

            ///对文法中的每一个终结符A，若它存在某个候选首符集包含ε，则
            ///FIRST(αi) ∩ FOLLOW(A)=∅，i=1,2,...,n
            foreach (var nonTerminal in grammar.Vn)
            {
                var first = mapFirst[nonTerminal];
                if (first.Contains(Terminal.Epsilon))
                {
                    var follow = mapFollow[nonTerminal];
                    if (first.Intersect(follow).Any())
                        stringBuilder.AppendLine($" {nonTerminal}的FIRST集含有ε，并且与它的FOLLOW集相交不为空，无法满足LL1文法。");
                }
            }

            errorMsg = stringBuilder.ToString();
            var result = string.IsNullOrWhiteSpace(errorMsg);
            if (result)
                lL1Grammar = new LL1Grammar(P, S, mapFirst, mapFollow);
            else lL1Grammar = null;
            return result;
        }
    }
}
