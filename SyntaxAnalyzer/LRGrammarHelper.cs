using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            int[] lengthCol = new int[cols];

            var sbMatrix = new StringBuilder[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    sbMatrix[i, j] = new();
                    var symbol = symbols[j - 1];

                    // 首行
                    if (i == 0)
                    {
                        // 首列
                        if (j == 0)
                            continue;                        
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
                            if (symbol is Terminal t)
                            {
                                if (Action.TryGetValue((i, t), out var list))
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
                            }
                            else if (symbol is NonTerminal n)
                            {
                                if (Goto.TryGetValue((i, n), out var state))
                                {
                                    sbMatrix[i, j].Append($"{state}");
                                }
                            }
                        }
                    }

                    lengthCol[j] = Math.Max(lengthCol[j], sbMatrix[i, j].Length);
                }
            }

            
           


            Console.WriteLine(sbMatrix.ToString());
        }
    }
}
