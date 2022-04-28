using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LexicalAnalyzer;

namespace SyntaxAnalyzer
{
    public static class LRGrammarHelper
    {
        public static void PrintTable(Grammar grammar, Dictionary<(int state, Terminal t), HashSet<ActionItem>> Action, Dictionary<(int state, NonTerminal t), int> Goto)
        {
            var symbols = grammar.Vt.Cast<Symbol>()
                .Append(Terminal.EndTerminal)
                .Union(grammar.Vn)
                .ToArray();
            var states = Action.Keys.Select(key => key.state)
                .Union(Goto.Keys.Select(key => key.state))
                .Distinct().OrderBy(s => s).ToArray();

            var rows = states.Length + 1;
            var cols = symbols.Length + 1;
            int[] colWidth = new int[cols];

            var sbMatrix = new StringBuilder[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    sbMatrix[i, j] = new();

                    // 首行
                    if (i == 0)
                    {
                        // 首列
                        if (j == 0)
                            continue;
                        var symbol = symbols[j - 1];
                        var strSymbol = symbol.ToString();
                        if (symbol == Terminal.EndTerminal)
                            strSymbol = "#";
                        sbMatrix[i, j].Append(strSymbol);
                    }
                    else
                    {
                        // 首列
                        if (j == 0)
                        {
                            sbMatrix[i, j].Append(states[i - 1]);
                        }
                        else
                        {
                            var symbol = symbols[j - 1];
                            if (symbol is Terminal t)
                            {
                                if (Action.TryGetValue((states[i - 1], t), out var list))
                                {
                                    foreach (var item in list)
                                    {
                                        if (item is ShiftItem si)
                                        {
                                            sbMatrix[i, j].Append($"s{si.State} ");
                                        }
                                        else if (item is ReduceItem ri)
                                        {
                                            // N -> eps []
                                            // 如果产生式为空，则为归约项目
                                            if (ri.Production.Right.SequenceEqual(Production.Epsilon))
                                                sbMatrix[i, j].Append($"r0@{ri.Production.Left} ");
                                            else
                                                sbMatrix[i, j].Append($"r{ri.Production.Right.Count()}@{ri.Production.Left} ");
                                        }
                                        else if (item is AcceptItem ai)
                                        {
                                            sbMatrix[i, j].Append($"acc ");
                                        }
                                    }
                                }
                                else sbMatrix[i, j].Append("+");
                            }
                            else if (symbol is NonTerminal n)
                            {
                                if (Goto.TryGetValue((states[i - 1], n), out var state))
                                {
                                    sbMatrix[i, j].Append($"{state}");
                                }
                                else sbMatrix[i, j].Append("+");
                            }
                        }
                    }

                    colWidth[j] = Math.Max(colWidth[j], sbMatrix[i, j].Length);
                }
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if(i == 0)
                        sb.Append($"{sbMatrix[i, j].ToString().PadRight(colWidth[j])} ");
                    else sb.Append($"{sbMatrix[i, j].ToString().PadRight(colWidth[j], '-')} ");
                }
                if (i + 1 < rows) sb.AppendLine();
            }
            Console.WriteLine(sb.ToString());
        }
    }
}
