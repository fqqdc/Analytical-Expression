using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// 算法优先文法
    /// </summary>
    public class OPGrammar : Grammar
    {
        private OPGrammar(IEnumerable<Production> allProduction, NonTerminal startNonTerminal
            , Dictionary<(Symbol, Symbol), char> predictiveTable
            ) : base(allProduction, startNonTerminal)
        {
            this.predictiveTable = predictiveTable.ToDictionary(i => i.Key, i => i.Value);
        }

        private Dictionary<(Symbol, Symbol), char> predictiveTable;

        public Dictionary<(Symbol, Symbol), char> GetPredictiveTable()
        {
            return predictiveTable.ToDictionary(i => i.Key, i => i.Value);
        }

        public static bool TryCreate(Grammar grammar, [MaybeNullWhen(false)] out OPGrammar oPGrammar, [MaybeNullWhen(true)] out string errorMsg)
        {
            oPGrammar = null;
            errorMsg = null;
            StringBuilder sbErrorMsg = new();

            var (S, P) = (grammar.S, grammar.P);

            foreach (var p in P)
            {
                var fst = p.Right.ElementAt(0);
                if (fst == Terminal.Epsilon)
                    sbErrorMsg.AppendLine($"不能包含空串产生式：{p}");
                if (fst is Terminal)
                    continue;
                var snd = p.Right.ElementAtOrDefault(1);
                if (snd == null || snd is Terminal)
                    continue;
                else sbErrorMsg.AppendLine($"不能包含连续的非终结符：{p}");
            }
            if (sbErrorMsg.Length > 0)
            {
                errorMsg = sbErrorMsg.ToString();
                return false;
            }

            P = P.Append(new(new($"_{S}_"), new Symbol[] { Terminal.EndTerminal, S, Terminal.EndTerminal }));

            var mapFirstVT = CalcFirstVT(P);
            var mapLastVT = CalcLastVT(P);

            var pTable = GetPredictiveTable(P, mapFirstVT, mapLastVT);
            foreach (var ((t1, t2), list) in pTable)
            {
                if (list.Count > 1)
                    sbErrorMsg.AppendLine($"优先表出现重定义：{t1} {string.Join(',', list)} {t2}");
            }
            if (sbErrorMsg.Length > 0)
            {

                errorMsg = sbErrorMsg.ToString();
                return false;
            }
            var predictiveTable = pTable.ToDictionary(i => i.Key, i => i.Value[0]);
            oPGrammar = new(P, S, predictiveTable);
            return true;
        }

        /// <summary>
        /// 计算FIRSTVT集
        /// </summary>
        private static Dictionary<NonTerminal, HashSet<Terminal>> CalcFirstVT(IEnumerable<Production> P)
        {
            Dictionary<NonTerminal, HashSet<Terminal>> mapFirstVT = new();
            Stack<(NonTerminal p, Terminal t)> stack = new();

            void Insert(NonTerminal n, Terminal t)
            {
                if (!mapFirstVT.TryGetValue(n, out var set))
                {
                    set = new();
                    mapFirstVT[n] = set;
                }

                if (!set.Contains(t))
                {
                    set.Add(t);
                    stack.Push((n, t));
                }
            }

            foreach (var p in P)
            {
                var fst = p.Right.ElementAt(0);
                if (fst is Terminal t_0)
                {
                    Insert(p.Left, t_0);
                    continue;
                }

                var snd = p.Right.ElementAtOrDefault(1);
                if (snd != null && snd is Terminal t_1)
                {
                    Insert(p.Left, t_1);
                    continue;
                }

                if (snd != null)
                    throw new Exception($"不能包含连续的非终结符：{p}");
            }

            while (stack.Count > 0)
            {
                var (n, t) = stack.Pop();
                foreach (var p in P.Where(p => p.Right.First() == n))
                {
                    Insert(p.Left, t);
                }
            }

            Console.WriteLine(mapFirstVT.ToString("FirstVT Sets:"));
            return mapFirstVT;
        }

        /// <summary>
        /// 计算LASTVT集
        /// </summary>
        private static Dictionary<NonTerminal, HashSet<Terminal>> CalcLastVT(IEnumerable<Production> P)
        {
            Dictionary<NonTerminal, HashSet<Terminal>> mapLastVT = new();
            Stack<(NonTerminal p, Terminal t)> stack = new();

            void Insert(NonTerminal n, Terminal t)
            {
                if (!mapLastVT.TryGetValue(n, out var set))
                {
                    set = new();
                    mapLastVT[n] = set;
                }

                if (!set.Contains(t))
                {
                    set.Add(t);
                    stack.Push((n, t));
                }
            }

            foreach (var p in P)
            {
                var revRight = p.Right.Reverse();
                var fst = revRight.ElementAt(0);
                if (fst is Terminal t_0)
                {
                    Insert(p.Left, t_0);
                    continue;
                }

                var snd = revRight.ElementAtOrDefault(1);
                if (snd != null && snd is Terminal t_1)
                {
                    Insert(p.Left, t_1);
                    continue;
                }

                if (snd != null) 
                    throw new Exception($"不能包含连续的非终结符：{p}");
            }

            while (stack.Count > 0)
            {
                var (n, t) = stack.Pop();
                foreach (var p in P.Where(p => p.Right.Reverse().First() == n))
                {
                    Insert(p.Left, t);
                }
            }

            Console.WriteLine(mapLastVT.ToString("LastVT Sets:"));
            return mapLastVT;
        }

        private static Dictionary<(Symbol, Symbol), List<char>> GetPredictiveTable(IEnumerable<Production> P
            , Dictionary<NonTerminal, HashSet<Terminal>> mapFirstVT
            , Dictionary<NonTerminal, HashSet<Terminal>> mapLastVT)
        {
            var predictiveTable = new Dictionary<(Symbol, Symbol), List<char>>();
            void setPredictiveTable((Symbol, Symbol) key, char value)
            {
                if (!predictiveTable.TryGetValue(key, out var list))
                {
                    list = new();
                    predictiveTable[key] = list;
                }
                list.Add(value);
            }

            foreach (var p in P)
            {
                var right = p.Right.ToArray();
                for (int i = 0; i < right.Length - 1; i++)
                {
                    var x_i = right[i];
                    var x_i1 = right[i + 1];

                    if (x_i is Terminal && x_i1 is Terminal)
                        setPredictiveTable((x_i, x_i1), '=');

                    if (i < right.Length - 2)
                    {
                        var x_i2 = right[i + 2];
                        if (x_i is Terminal && x_i2 is Terminal)
                            setPredictiveTable((x_i, x_i2), '=');
                    }

                    if (x_i is Terminal && x_i1 is NonTerminal)
                    {
                        var n = (NonTerminal)x_i1;
                        foreach (var t in mapFirstVT[n])
                            setPredictiveTable((x_i, t), '<');
                    }

                    if (x_i is NonTerminal && x_i1 is Terminal)
                    {
                        var n = (NonTerminal)x_i;
                        foreach (var t in mapLastVT[n])
                            setPredictiveTable((t, x_i1), '>');
                    }
                }
            }

            // ===== Print

            StringBuilder sbMatrix = new StringBuilder();
            var symbols = predictiveTable.Keys
                .SelectMany(key => Enumerable.Empty<Symbol>().Append(key.Item1).Append(key.Item2))
                .Distinct().ToArray();
            for (int i = -1; i < symbols.Length; i++)
            {

                if (i == -1)
                {
                    // 行首
                    sbMatrix.Append("\t");
                }
                else
                {
                    sbMatrix.Append($"{symbols[i]}\t");
                }


                for (int j = 0; j < symbols.Length; j++)
                {

                    if (i == -1)
                    {
                        // 行首
                        sbMatrix.Append($"{symbols[j]}\t");
                    }
                    else
                    {
                        if (!predictiveTable.TryGetValue((symbols[i], symbols[j]), out var list))
                            list = new();
                        sbMatrix.Append($"{string.Join(',', list)}\t");
                    }
                }
                sbMatrix.AppendLine();
            }
            Console.WriteLine(sbMatrix.ToString());

            return predictiveTable;
        }
    }
}
