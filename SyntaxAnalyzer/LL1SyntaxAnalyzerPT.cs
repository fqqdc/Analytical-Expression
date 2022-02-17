using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// LL1分析器（构造预测分析表）
    /// </summary>
    public class LL1SyntaxAnalyzerPT
    {
        public delegate void AdvanceProcedure(out Terminal Sym);

        private LL1Grammar grammar;
        private AdvanceProcedure advanceProcedure;
        private Terminal sym = Terminal.Epsilon;
        private List<(NonTerminal n, Terminal t, Production p)> predictiveTable;
        private Stack<Symbol> stackAnalysis = new();
        public LL1SyntaxAnalyzerPT(LL1Grammar grammar, AdvanceProcedure advanceProcedure)
        {
            this.grammar = grammar;
            this.advanceProcedure = advanceProcedure;

            this.predictiveTable = ConstructingPredictiveAnalysisTable();
        }

        /// <summary>
        /// 预测分析表
        /// </summary>
        private List<(NonTerminal, Terminal, Production)> ConstructingPredictiveAnalysisTable()
        {
            var predictiveTable = new List<(NonTerminal, Terminal, Production)>();
            foreach (var p in grammar.P)
            {
                var first = grammar.CalcFirst(p.Right);
                foreach (var tFirst in first)
                {
                    if(tFirst != Terminal.Epsilon)
                        predictiveTable.Add((p.Left, tFirst, p));
                    else
                    {
                        var follow = grammar.GetFollow(p.Left);
                        foreach (var tFollow in follow)
                            predictiveTable.Add((p.Left, tFollow, new(p.Left, Production.Epsilon)));
                    }
                }
            }
            foreach (var row in predictiveTable)
            {
                Console.WriteLine(row);
            }
            return predictiveTable;
        }

        
        private Dictionary<Production, HashSet<Terminal>> rightFirst = new();
        private void Advance()
        {
            advanceProcedure(out this.sym);
            Console.WriteLine($"input:{this.sym}");
        }
        private void Error() { throw new Exception("语法分析错误"); }

        private void ProcedureInit()
        {
            stackAnalysis.Clear();
            stackAnalysis.Push(Terminal.EndTerminal);
            stackAnalysis.Push(grammar.S);
        }

        private void Procedure()
        {
            do
            {
                var symbolTop = stackAnalysis.Pop();
                if (symbolTop is Terminal terminal)
                {
                    if (terminal == sym)
                    {
                        if (terminal == Terminal.EndTerminal)
                            break;
                        Advance();
                    }
                    else Error();
                }
                else
                {
                    var item = predictiveTable.SingleOrDefault(i => i.n == symbolTop && i.t == sym);
                    var p = item.p;
                    if (p == null)
                        Error();
                    else
                    {
                        Console.WriteLine(p);
                        foreach (var symbol in p.Right.Reverse())
                        {
                            if (symbol == Terminal.Epsilon)
                                continue;
                            stackAnalysis.Push(symbol);
                        }
                    }
                }

            } while (true);
        }

        public void Analyzer()
        {
            ProcedureInit();
            Advance();
            Procedure();
        }
    }
}
