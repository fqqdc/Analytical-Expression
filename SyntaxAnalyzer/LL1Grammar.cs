using System;
using System.Collections.Generic;
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
            this.mapFirst = mapFirst;
            this.mapFollow = mapFollow;
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
            if (prods.Any(p => p.Right.SequenceEqual(Production.Epsilon)))
                throw new NotSupportedException($"不能含有以ε为右边的产生式");
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

                    int index = 1;
                    NonTerminal newLeft = new($"{group.Key.Name}{index}");
                    while (newSet.Any(p => p.Left == newLeft))
                    {
                        index += 1;
                        newLeft = new($"{group.Key.Name}{index}");
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

        public static bool CheckLL1Grammar(Grammar grammar, out string errorMsg)
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
            return string.IsNullOrWhiteSpace(errorMsg);
        }

        public static LL1Grammar CreateFrom(Grammar grammar)
        {
            var (S, P) = (grammar.S, grammar.P);
            var mapFirst = CalcFirsts(P);
            var mapFollow = CalcFollows(P, mapFirst, S);
            if (!CheckLL1Grammar(grammar, out var msg))
                throw new Exception(msg);

            return new LL1Grammar(P, S, mapFirst, mapFollow);
        }

        public static bool TryCreateFrom(Grammar grammar, out LL1Grammar? lL1Grammar)
        {
            try
            {
                lL1Grammar = CreateFrom(grammar);
                return true;
            }
            catch (Exception)
            {
                lL1Grammar = null;
                return false;
            }
        }

        /// <summary>
        /// 计算FIRST集
        /// </summary>
        private static Dictionary<NonTerminal, HashSet<Terminal>> CalcFirsts(IEnumerable<Production> P)
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

                    bool endWithEpsilon = true;
                    foreach (var symbol in production.Right)
                    {
                        if (symbol is Terminal terminal)
                        {
                            /// 若X∈Vt,则FIRST(X)={X}
                            /// 若X∈Vn,且有产生式X->a...，则把a加入到FIRST(X)中；
                            /// 若X->ε也是一条产生式，则把ε也加到FIRST(X)中。
                            first.Add(terminal);
                            endWithEpsilon = false;
                            break;
                        }

                        ///若X->Y1Y2...Yi-1Yi...Yk是一个产生式，Y1,...Yi-1都是非终结符                        
                        if (symbol is NonTerminal nonTerminal)
                        {
                            if (!mapFirst.TryGetValue(nonTerminal, out var firstN))
                            {
                                firstN = new();
                                mapFirst[nonTerminal] = firstN;
                                hasChanged = true;
                            }

                            ///对于任何j，1<=j<=i-1，FIRST(Yj)都含有ε(即Y1...Yi=>ε)，则把FIRST(Yi)中的所有非ε元素都加到FIRST(X)中
                            var firstN_Copy = firstN.ToHashSet();
                            firstN_Copy.Remove(Terminal.Epsilon);
                            first.UnionWith(firstN_Copy);

                            if (!firstN.Contains(Terminal.Epsilon))
                            {
                                endWithEpsilon = false;
                                break;
                            }
                        }
                    }
                    if (endWithEpsilon)
                    {
                        ///若所有的FIRST(Yj)均含有ε，j=1,2,3,...,k，则把ε加到FIRST(X)中
                        first.Add(Terminal.Epsilon);
                    }

                    hasChanged = hasChanged || !first.SetEquals(oldFirst);
                }
            }

            Console.WriteLine(mapFirst.ToString("First Sets:"));
            return mapFirst;
        }

        private static HashSet<Terminal> CalcFirst(IEnumerable<Symbol> alpha, Dictionary<NonTerminal, HashSet<Terminal>> mapFirst)
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

                    first.UnionWith(firstNonTerminal);
                    first.Remove(Terminal.Epsilon);

                    if (!firstNonTerminal.Contains(Terminal.Epsilon))
                        break;
                }
                first.Add(Terminal.Epsilon);
            }

            return first;
        }
        /// <summary>
        /// 计算FOLLOW集
        /// </summary>
        private static Dictionary<NonTerminal, HashSet<Terminal>> CalcFollows(IEnumerable<Production> P, Dictionary<NonTerminal, HashSet<Terminal>> mapFirst, NonTerminal S)
        {
            Dictionary<NonTerminal, HashSet<Terminal>> mapFollow = new();
            mapFollow[S] = new();
            mapFollow[S].Add(Terminal.EndTerminal); //对于文法开始符号S，要将#置于FOLLOW(S)中

            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;

                foreach (var production in P)
                {

                    Stack<Symbol> beta = new();

                    foreach (var symbol in production.Right.Reverse())
                    {
                        if (symbol is Terminal terminal)
                        {
                            beta.Push(terminal);
                            continue;
                        }

                        if (symbol is NonTerminal nonTerminal)
                        {
                            if (!mapFollow.TryGetValue(nonTerminal, out var follow))
                            {
                                follow = new();
                                mapFollow[nonTerminal] = follow;
                                hasChanged = true;
                            }

                            var oldFollow = follow.ToHashSet();
                            if (beta.Any())
                            {
                                var first = CalcFirst(beta, mapFirst);
                                //若A->αBβ是一个产生式，则把FIRST(β)\{ε}加入FOLLOW(B)中
                                bool containEpsilon = first.Remove(Terminal.Epsilon);
                                follow.UnionWith(first);

                                if (containEpsilon && mapFollow.TryGetValue(production.Left, out var followLeft))
                                {
                                    follow.UnionWith(followLeft); //若A->αBβ是一个产生式，而β=>ε(既ε∈FIRST(β))，则将FIRST(A)加入FIRST(B)
                                }
                            }
                            else
                            {
                                if (mapFollow.TryGetValue(production.Left, out var followLeft))
                                {
                                    follow.UnionWith(followLeft); //若A->αB是一个产生式，则将FIRST(A)加入FIRST(B)
                                }
                            }

                            hasChanged = hasChanged || !oldFollow.SetEquals(follow);
                            beta.Push(nonTerminal);
                        }
                    }
                }
            }

            Console.WriteLine(mapFollow.ToString("Follow Sets:"));
            return mapFollow;
        }
    }
}
