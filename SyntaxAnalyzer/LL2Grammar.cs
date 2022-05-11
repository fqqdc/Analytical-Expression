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
            , Dictionary<NonTerminal, HashSet<DoubleTerminal>> mapFirst2
            , Dictionary<NonTerminal, HashSet<DoubleTerminal>> mapFollow2
            ) : base(allProduction, startNonTerminal)
        {
            this.mapFirst2 = mapFirst2.ToDictionary(i => i.Key, i => i.Value.ToHashSet());
            this.mapFollow2 = mapFollow2.ToDictionary(i => i.Key, i => i.Value.ToHashSet()); ;
        }

        private Dictionary<NonTerminal, HashSet<Terminal>> mapFirst = new();
        private Dictionary<NonTerminal, HashSet<Terminal>> mapFollow = new();

        private Dictionary<NonTerminal, HashSet<DoubleTerminal>> mapFirst2;
        private Dictionary<NonTerminal, HashSet<DoubleTerminal>> mapFollow2;

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
                        HashSet<DoubleTerminal>? first2N = new();

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
                                first2N = new();
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

                /// 如果FIRST(X)为空或FIRST(X)不含有类似(any, ε)的符号对，则推导结束
                if (first2.All(dt => dt.Second != Terminal.Epsilon))
                    break;
            }

            return first2;
        }

        /// <summary>
        /// 计算FOLLOW2集
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
                                mapFollow2[nonTerminal] = follow2N;
                                hasChanged = true;
                            }

                            var oldFollow2 = follow2N.ToHashSet();

                            if (beta.Any())
                            {
                                var first2 = CalcFirst2(beta, mapFirst2);
                                //若A->αBβ是一个产生式，FOLLOW2(B) = FIRST2(β) X FOLLOW2(A)
                                if (mapFollow2.TryGetValue(production.Left, out var follow2Left))
                                {
                                    if (follow2Left.Count > 0)
                                        first2.ProductWith(follow2Left);
                                }

                                /// 去除所有类似(any,ε)的元素对，(end,ε)除外
                                var follow2 = first2.Where(dt => dt.Second != Terminal.Epsilon || dt == DoubleTerminal.EndTerminal);

                                follow2N.UnionWith(follow2);
                            }
                            else
                            {
                                if (mapFollow2.TryGetValue(production.Left, out var follow2Left))
                                {
                                    /// 去除所有类似(any,ε)的元素对，(end,ε)除外
                                    var follow2 = follow2Left.Where(dt => dt.Second != Terminal.Epsilon || dt == DoubleTerminal.EndTerminal);

                                    follow2N.UnionWith(follow2);
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

        public static bool TryCreate(Grammar grammar, [MaybeNullWhen(false)] out LL2Grammar lL2Grammar, out string createMsg)
        {
            lL2Grammar = null;

            var (S, P) = (grammar.S, grammar.P);
            var mapFirst2 = CalcFirst2Sets(P);
            var mapFollow2 = CalcFollow2Sets(P, mapFirst2, S);

            StringBuilder createMsgBuilder = new();
            if (HasLeftRecursion(grammar, out var recMsg))
            {
                createMsgBuilder.AppendLine("无法满足LL1文法：");
                foreach (var msg in recMsg.Split("\n"))
                {
                    if (!string.IsNullOrWhiteSpace(msg))
                        createMsgBuilder.AppendLine($"  =>{msg}");
                }

                createMsg = createMsgBuilder.ToString();
                return false;
            }

            var pArray = P.ToArray();
            var mapSelect2 = new Dictionary<Production, HashSet<DoubleTerminal>>();
            foreach (var p in pArray)
            {
                mapSelect2[p] = CalcFirst2(p.Right, mapFirst2).Product(mapFollow2[p.Left]);
            }

            for (int i = 0; i < pArray.Length; i++)
            {
                var iSelect2 = mapSelect2[pArray[i]];
                for (int j = i + 1; j < pArray.Length; j++)
                {
                    var jSelect2 = mapSelect2[pArray[j]];

                    if (pArray[i].Left == pArray[j].Left && iSelect2.Intersect(jSelect2).Any())
                    {
                        createMsgBuilder.AppendLine($"无法满足LL2文法：{pArray[i]}的SELECT2集与{pArray[j]}的SELECT2集相交不为空。");
                        createMsgBuilder.AppendLine($"  =>{pArray[i]}的SELECT2集: {string.Join(", ", iSelect2)}");
                        createMsgBuilder.AppendLine($"  =>{pArray[j]}的SELECT2集: {string.Join(", ", jSelect2)}");
                    }
                }
            }

            createMsg = createMsgBuilder.ToString();
            var result = string.IsNullOrWhiteSpace(createMsg);

            if (result)
                lL2Grammar = new LL2Grammar(P, S, mapFirst2, mapFollow2);

            if (LL2Grammar.PrintTable || !result && LL2Grammar.PrintTableIfConflict)
            {
                Console.WriteLine(mapFirst2.ToString("First2 Sets"));
                Console.WriteLine(mapFollow2.ToString("Follow2 Sets"));
                Console.WriteLine(mapSelect2.ToString("Select2 Sets"));
                Console.WriteLine($"LL2前看符号合计：{mapSelect2.SelectMany(i=>i.Value).Distinct().Count()}");
            }

            createMsg = createMsgBuilder.ToString();
            return result;
        }
    }
}
