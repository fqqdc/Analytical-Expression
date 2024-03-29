﻿using LexicalAnalyzer;
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

        [Obsolete("TryCreate方法的旧版本备份")]
        public static bool TryCreate2(Grammar grammar, [MaybeNullWhen(false)] out LL1Grammar lL1Grammar, out string errorMsg)
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

        public static bool TryCreate(Grammar grammar, [MaybeNullWhen(false)] out LL1Grammar lL1Grammar, out string createMsg)
        {
            lL1Grammar = null;
            var (S, P) = (grammar.S, grammar.P);
            var mapFirst = CalcFirsts(P);
            var mapFollow = CalcFollows(P, mapFirst, S);

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
            var mapSelect = new Dictionary<Production, HashSet<Terminal>>();
            foreach (var p in pArray)
            {
                var set = CalcFirst(p.Right, mapFirst);
                if (set.Remove(Terminal.Epsilon))
                    set.UnionWith(mapFollow[p.Left]);

                mapSelect[p] = set;
            }

            ///对于文法中每一个非终结符A的各个产生式的SELECT集两两不相交。
            ///即，若A->α1|α2|...|αn
            ///则 SELECT(αi) ∩ SELECT(αi)=∅ (i≠j)
            for (int i = 0; i < pArray.Length; i++)
            {
                var iSelect = mapSelect[pArray[i]];
                for (int j = i + 1; j < pArray.Length; j++)
                {
                    var jSelect = mapSelect[pArray[j]];

                    if (pArray[i].Left == pArray[j].Left && iSelect.Intersect(jSelect).Any())
                    {
                        createMsgBuilder.AppendLine($"无法满足LL1文法：{pArray[i]}的SELECT集与{pArray[j]}的SELECT集相交不为空。");
                        createMsgBuilder.AppendLine($"  =>{pArray[i]}的SELECT集: {string.Join(", ", iSelect)}");
                        createMsgBuilder.AppendLine($"  =>{pArray[j]}的SELECT集:  {string.Join(", ", jSelect)}");
                    }
                }
            }

            createMsg = createMsgBuilder.ToString();
            var result = string.IsNullOrWhiteSpace(createMsg);

            if (result)
                lL1Grammar = new LL1Grammar(P, S, mapFirst, mapFollow);

            if (LL2Grammar.PrintTable || !result && LL2Grammar.PrintTableIfConflict)
            {
                Console.WriteLine(mapFirst.ToString("First Sets"));
                Console.WriteLine(mapFollow.ToString("Follow Sets"));
                Console.WriteLine(mapSelect.ToString("Select Sets"));
            }

            return result;
        }
    }
}
