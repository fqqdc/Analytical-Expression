using LexicalAnalyzer;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SyntaxAnalyzer
{
    public class LL1Grammar : LLGrammar
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

        public static bool TryCreate(Grammar grammar, [MaybeNullWhen(false)] out LL1Grammar lL1Grammar, out string errorMsg)
        {
            var (S, P) = (grammar.S, grammar.P);
            var mapFirst = CalcFirsts(P);
            var mapFollow = CalcFollows(P, mapFirst, S);
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
                lL1Grammar = new LL1Grammar(P, S, mapFirst, mapFollow);
            else
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(mapFirst.ToString("First Sets"));
                stringBuilder.AppendLine(mapFollow.ToString("Follow Sets"));
                errorMsg = stringBuilder.ToString();
                lL1Grammar = null;
            }
            return result;
        }
    }
}
