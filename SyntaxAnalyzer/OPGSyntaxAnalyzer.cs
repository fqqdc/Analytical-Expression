using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// 通用算法优先分析器
    /// </summary>
    public class OPGSyntaxAnalyzer : SyntaxAnalyzer
    {
        private OPGrammar grammar;
        private AdvanceProcedure advanceProcedure;
        private Terminal sym = Terminal.Epsilon;
        private Dictionary<Symbol, int> symbolIndex = new();
        private List<Symbol> stackAnalysis = new();
        private Dictionary<(Symbol, Symbol), char> predictiveTable;
        public OPGSyntaxAnalyzer(OPGrammar grammar, AdvanceProcedure advanceProcedure)
        {
            this.grammar = grammar;
            this.advanceProcedure = advanceProcedure;
            this.predictiveTable = grammar.GetPredictiveTable();
        }

        private char GetPredictiveChar(Symbol left, Symbol right)
        {
            if (!predictiveTable.TryGetValue((left, right), out var value))
                value = '\0';
            return value;
        }


        private void Advance()
        {
            advanceProcedure(out this.sym);
            Console.WriteLine($"input:{this.sym}");
        }
        private void Error() { throw new Exception("语法分析错误"); }

        private void ProcedureInit()
        {
            stackAnalysis.Clear();
            stackAnalysis.Add(Terminal.EndTerminal);
        }

        private void Procedure()
        {
            do
            {
                Advance();
                int indexT = stackAnalysis.Count - 1;
                if (stackAnalysis[indexT] is not Terminal)
                    indexT = indexT - 1;

                while (GetPredictiveChar(stackAnalysis[indexT], sym) == '>') // 素短语归约可能做多次
                {
                    // 寻找素短语的头部
                    Symbol symbol;
                    do
                    {
                        symbol = stackAnalysis[indexT];
                        if (stackAnalysis[indexT - 1] is Terminal)
                            indexT = indexT - 1;
                        else
                            indexT = indexT - 2;
                    } while (GetPredictiveChar(stackAnalysis[indexT], symbol) != '<');

                    //寻找对应的产生式进行规约
                    var pPhrase = stackAnalysis.Skip(indexT + 1).ToArray();
                    bool hasReduced = false; // 是否成功归约
                    foreach (var p in grammar.P)
                    {
                        // 素短语与等长产生式右部比较
                        var pRight = p.Right.ToArray();
                        if (pPhrase.Length != pRight.Length)
                            continue;
                        bool isEqual = false;
                        for (int i = 0; i < pPhrase.Length; i++)
                        {
                            if (pPhrase[i] is Terminal && pRight[i] is Terminal
                                && pPhrase[i] == pRight[i])
                            {
                                if (i + 1 == pPhrase.Length)
                                    isEqual = true;
                                continue;
                            }

                            if (pPhrase[i] is NonTerminal && pRight[i] is NonTerminal)
                            {
                                if (i + 1 == pPhrase.Length)
                                    isEqual = true;
                                continue;
                            }

                            break;
                        }
                        if (isEqual)
                        {
                            // 找到对应产生式后进行规约
                            stackAnalysis.RemoveRange(indexT + 1, pPhrase.Length);
                            stackAnalysis.Add(p.Left);
                            hasReduced = true;
                            Console.WriteLine(p);
                            break;
                        }
                    }

                    if (!hasReduced)
                        Error(); // 找不到相应的产生式归约，则出错
                }

                // 移进                
                if (GetPredictiveChar(stackAnalysis[indexT], sym) == '\0')
                    Error();
                else stackAnalysis.Add(sym);

            } while (sym != Terminal.EndTerminal);

            // 识别成功
        }

        public void Analyzer()
        {
            ProcedureInit();
            Procedure();
        }
    }
}
