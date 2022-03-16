using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxAnalyzer
{
    public static class LRGrammarHelper
    {
        public static void PrintTable(Grammar grammar, Dictionary<(int state, Terminal t), List<ActionItem>> Action, Dictionary<(int state, NonTerminal t), int> Goto)
        {
            StringBuilder sbMatrix = new StringBuilder();
            var symbols = grammar.Vt.Cast<Symbol>()
                .Append(Terminal.EndTerminal)
                .Union(grammar.Vn)
                .ToArray();
            var states = Action.Keys.Select(key => key.state)
                .Union(Goto.Keys.Select(key => key.state))
                .Distinct().OrderBy(s => s).ToArray();

            for (int i = -1; i < states.Length; i++)
            {
                // 打印列首
                if (i == -1)
                {
                    sbMatrix.Append("\t");
                }
                else
                {
                    sbMatrix.Append($"{states[i]}\t");
                }

                // 打印每一列
                for (int j = 0; j < symbols.Length; j++)
                {
                    var symbol = symbols[j];
                    if (i == -1)
                    {
                        // 行首
                        var strSymbol = symbol.ToString();
                        if (symbol == Terminal.EndTerminal)
                            strSymbol = "#";
                        sbMatrix.Append($"{strSymbol}\t");
                    }
                    else
                    {
                        var sbItem = new StringBuilder();
                        if (symbol is Terminal t)
                        {
                            if (Action.TryGetValue((i, t), out var list))
                            {
                                foreach (var item in list)
                                {
                                    if (item is ShiftItem si)
                                    {
                                        sbItem.Append($"s{si.State} ");
                                    }
                                    else if (item is ReduceItem ri)
                                    {
                                        // N -> eps []
                                        // 如果产生式为空，则为归约项目
                                        if (ri.Production.Right == Production.Epsilon)
                                            sbItem.Append($"r0 ");
                                        else
                                            sbItem.Append($"r{ri.Production.Right.Count()} ");
                                    }
                                    else if (item is AcceptItem ai)
                                    {
                                        sbItem.Append($"acc ");
                                    }
                                }
                            }
                            if (sbItem.Length == 0)
                                sbItem.Append("");
                            sbItem.Append($"\t");
                            sbMatrix.Append(sbItem);
                        }
                        else if (symbol is NonTerminal n)
                        {
                            if (Goto.TryGetValue((i, n), out var state))
                            {
                                sbItem.Append($"{state}");
                            }
                            if (sbItem.Length == 0)
                                sbItem.Append("");
                            sbItem.Append($"\t");
                            sbMatrix.Append(sbItem);
                        }
                    }
                }
                sbMatrix.AppendLine();
            }
            Console.WriteLine(sbMatrix.ToString());
        }
    }
}
