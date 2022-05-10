using LexicalAnalyzer;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SyntaxAnalyzer
{
    public class LL2Grammar : LLGrammar
    {
        private LL2Grammar(IEnumerable<Production> allProduction, NonTerminal startNonTerminal
            , Dictionary<NonTerminal, HashSet<Terminal>> mapFirst
            , Dictionary<NonTerminal, HashSet<Terminal>> mapFollow
            ) : base(allProduction, startNonTerminal)
        {
            this.mapFirst = mapFirst.ToDictionary(i => i.Key, i => i.Value.ToHashSet());
            this.mapFollow = mapFollow.ToDictionary(i => i.Key, i => i.Value.ToHashSet()); ;
        }

        private Dictionary<NonTerminal, HashSet<Terminal>> mapFirst;
        private Dictionary<NonTerminal, HashSet<Terminal>> mapFollow;

        private Dictionary<NonTerminal, HashSet<DoubleTerminal>> mapFirst2;
        private Dictionary<NonTerminal, HashSet<DoubleTerminal>> mapFollow2;

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

        /// <summary>
        /// 计算FIRST2集
        /// </summary>
        protected static Dictionary<NonTerminal, HashSet<DoubleTerminal>> CalcFirst2Sets(IEnumerable<Production> P)
        {
            Dictionary<NonTerminal, HashSet<DoubleTerminal>> mapFirst2 = new();
            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;

                foreach (var production in P)
                {
                    // 从头开始计算FIRST2(X)
                    var newFirst2 = new HashSet<DoubleTerminal>();
                    newFirst2.Add(DoubleTerminal.Epsilon);

                    foreach (var symbol in production.Right)
                    {
                        HashSet<DoubleTerminal> first2N = new();

                        if (symbol is Terminal terminal)
                        {
                            /// 若X∈Vt,则FIRST2(X)={(X, ε)}
                            /// 若X∈Vn,且有产生式X->a...，则把(a, ε)加入到FIRST(X)中；
                            /// 若X->ε也是一条产生式，则把(ε, ε)加到FIRST(X)中。
                            first2N.Add(new(terminal, Terminal.Epsilon));
                        }
                        ///若X->Y1Y2...Yi-1Yi...Yk是一个产生式，Y1,...Yi-1都是非终结符                        
                        else if (symbol is NonTerminal nonTerminal)
                        {
                            if (!mapFirst2.TryGetValue(nonTerminal, out first2N))
                            {
                                first2N = new() { DoubleTerminal.Epsilon };
                                mapFirst2[nonTerminal] = first2N;
                                hasChanged = true;
                            }
                        }

                        /// FIRST2(X) = FIRST2(X) X FIRST2(Yi)
                        newFirst2.ProductWith(first2N);

                        /// 如果FIRST(X)不含有类似(any, ε)的元素对
                        if (newFirst2.All(dt => dt.Second != Terminal.Epsilon))
                            break;
                    }

                    if (!mapFirst2.TryGetValue(production.Left, out var first2))
                    {
                        first2 = new();
                        mapFirst2[production.Left] = first2;
                        hasChanged = true;
                    }

                    var oldFirst2 = first2.ToHashSet();
                    first2.UnionWith(newFirst2);

                    hasChanged = hasChanged || !oldFirst2.SetEquals(first2);
                }
            }

            return mapFirst2;
        }

        protected static HashSet<DoubleTerminal> CalcFirst2(IEnumerable<Symbol> alpha, Dictionary<NonTerminal, HashSet<DoubleTerminal>> mapFirst2)
        {
            // 从头开始计算FIRST2(X)
            HashSet<DoubleTerminal> first2 = new();
            first2.Add(DoubleTerminal.Epsilon);

            foreach (var symbol in alpha)
            {
                HashSet<DoubleTerminal>? first2N = new();

                if (symbol is Terminal terminal)
                {
                    first2N.Add(new(terminal, Terminal.Epsilon));
                }

                if (symbol is NonTerminal nonTerminal)
                {
                    if (!mapFirst2.TryGetValue(nonTerminal, out first2N))
                    {
                        first2N = new();
                        first2N.Add(DoubleTerminal.Epsilon);
                    }
                }

                /// FIRST2(X) = FIRST2(X) X FIRST2(Yi)
                first2.ProductWith(first2N);

                /// 如果FIRST(X)不含有类似(any, ε)的元素对
                if (first2.All(dt => dt.Second != Terminal.Epsilon))
                    break;
            }

            return first2;
        }

        /// <summary>
        /// 计算FOLLOW集
        /// </summary>
        protected static Dictionary<NonTerminal, HashSet<DoubleTerminal>> CalcFollow2Sets(IEnumerable<Production> P, Dictionary<NonTerminal, HashSet<DoubleTerminal>> mapFirst2, NonTerminal S)
        {
            Dictionary<NonTerminal, HashSet<DoubleTerminal>> mapFollow2 = new();
            mapFollow2[S] = new();
            mapFollow2[S].Add(DoubleTerminal.EndTerminal); //对于文法开始符号S，要将(#,ε)置于FOLLOW2(S)中

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
                            if (!mapFollow2.TryGetValue(nonTerminal, out var follow2N))
                            {
                                follow2N = new();
                                follow2N.Add(DoubleTerminal.Epsilon);
                                mapFollow2[nonTerminal] = follow2N;
                                hasChanged = true;
                            }

                            var oldFollow2 = follow2N.ToHashSet();

                            //FOLLOW2(B) = {(ε,ε)}
                            HashSet<DoubleTerminal> newFollow2 = new();
                            newFollow2.Add(DoubleTerminal.Epsilon);

                            if (beta.Any())
                            {
                                var first2 = CalcFirst2(beta, mapFirst2);
                                //若A->αBβ是一个产生式，FOLLOW2(B) = FOLLOW2(B) X FIRST2(β)
                                newFollow2.ProductWith(first2);

                                /// 如果FIRST2(β)含有类似(any, ε)的元素对
                                bool containEpsilon = first2.Any(dt => dt.Second == Terminal.Epsilon);

                                if (containEpsilon && mapFollow2.TryGetValue(production.Left, out var follow2Left))
                                {
                                    //若A->αBβ是一个产生式，而(any,ε)∈FIRST2(β)，则将FOLLOW2(B) = FOLLOW2(B) X FOLLOW2(A)
                                    newFollow2.ProductWith(follow2Left);
                                }
                                follow2N.UnionWith(newFollow2);
                            }
                            else
                            {
                                if (mapFollow2.TryGetValue(production.Left, out var follow2Left))
                                {
                                    //newFollow2.ProductWith(follow2Left);

                                    //follow2N.UnionWith(newFollow2); //若A->αB是一个产生式，则将{(ε,ε)} X FOLLOW2(A)加入FOLLOW2(B)
                                    follow2N.ProductWith(follow2Left);
                                }
                            }

                            hasChanged = hasChanged || !oldFollow2.SetEquals(follow2N);
                            beta.Push(nonTerminal);
                        }
                    }
                }
            }

            return mapFollow2;
        }

        public static bool TryCreate(Grammar grammar, [MaybeNullWhen(false)] out LL2Grammar lL2Grammar, out string errorMsg)
        {
            lL2Grammar = null;

            var (S, P) = (grammar.S, grammar.P);
            var mapFirst = CalcFirsts(P);
            var mapFirst2 = CalcFirst2Sets(P);

            var mapFollow = CalcFollows(P, mapFirst, S);
            var mapFollow2 = CalcFollow2Sets(P, mapFirst2, S);
            StringBuilder stringBuilder = new();

            if (HasLeftRecursion(grammar, out var msg))
            {
                stringBuilder.AppendLine(msg);
            }

            ///对于文法中每一个非终结符A的各个产生式的候选首符集两两不相交。
            ///即，若A->α1|α2|...|αn
            ///则 FIRST(αi) ∩ FIRST(αi)=∅ (i≠j)
            ///
            ///对于每个非终结符A的两个不同产生式，A->α,A->β，α，β不能同时推导到ε。
            ///如果存在某条产生式能推导到ε，则 其他产生式A->α1|α2|...|αn
            ///FIRST(αi) ∩ FOLLOW(A) = ∅
            foreach (var pGroup in P.GroupBy(p => p.Left))
            {
                var pArray = pGroup.ToArray();
                for (int i = 0; i < pArray.Length; i++)
                {
                    var iFirst = CalcFirst(pArray[i].Right, mapFirst);
                    for (int j = i + 1; j < pArray.Length; j++)
                    {
                        var jFirst = CalcFirst(pArray[j].Right, mapFirst);
                        if (iFirst.Intersect(jFirst).Any())
                        {
                            stringBuilder.AppendLine($"{pArray[i]}右部的FIRST集与{pArray[j]}右部的FIRST集相交不为空，无法满足LL1文法。");
                            stringBuilder.AppendLine($"    {pArray[i]}右部的FIRST集:{string.Join(", ", iFirst)}");
                            stringBuilder.AppendLine($"    {pArray[j]}右部的FIRST集:{string.Join(", ", jFirst)}");
                        }
                    }

                    if (iFirst.Any(t => t == Terminal.Epsilon))
                    {
                        var follow = mapFollow[pArray[i].Left];
                        for (int j = 0; j < pArray.Length; j++)
                        {
                            if (i == j) continue;
                            var jFirst = CalcFirst(pArray[j].Right, mapFirst);
                            if (jFirst.Intersect(follow).Any())
                            {
                                stringBuilder.AppendLine($"{pArray[i]}右部的FIRST集含有ε，{pArray[i].Left}的FOLLOW集与{pArray[j]}右部的FIRST集相交不为空，无法满足LL1文法。");
                                stringBuilder.AppendLine($"    {pArray[i].Left}的FOLLOW集: {string.Join(", ", follow)}");
                                stringBuilder.AppendLine($"    {pArray[j]}右部的FIRST集: {string.Join(", ", jFirst)}");
                            }
                        }
                    }
                }
            }

            errorMsg = stringBuilder.ToString();
            var result = string.IsNullOrWhiteSpace(errorMsg);

            if (result)
                lL2Grammar = new LL2Grammar(P, S, mapFirst, mapFollow);

            if (LL2Grammar.PrintTable || !result && LL2Grammar.PrintTableIfConflict)
            {
                Console.WriteLine(mapFirst.ToString("First Sets"));
                Console.WriteLine(mapFollow.ToString("Follow Sets"));
                Console.WriteLine(mapFirst2.ToString("First2 Sets"));
                Console.WriteLine(mapFollow2.ToString("Follow2 Sets"));
            }

            errorMsg = stringBuilder.ToString();
            return result;
        }
    }
}
