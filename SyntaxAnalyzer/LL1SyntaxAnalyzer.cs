using LexicalAnalyzer;
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
    public class LL1SyntaxAnalyzer
    {
        public delegate void AdvanceProcedure(out Terminal Sym, out string terminalToken);

        private LL1Grammar grammar;
        protected AdvanceProcedure _advanceProcedure;
        private Terminal sym = Terminal.Epsilon;
        private string symToken = string.Empty;
        private List<(NonTerminal n, Terminal t, Production p)> predictiveTable;
        private Stack<Symbol> stackAnalysis = new();
        public LL1SyntaxAnalyzer(LL1Grammar grammar, AdvanceProcedure advanceProcedure)
        {
            this.grammar = grammar;
            this._advanceProcedure = advanceProcedure;

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
            _advanceProcedure(out this.sym, out this.symToken);
            Console.WriteLine($"input:{this.sym}: {this.symToken}");
        }
        private void Error() { throw new Exception("语法分析错误"); }

        private void ProcedureInit()
        {
            stackAnalysis.Clear();
            stackAnalysis.Push(Terminal.EndTerminal);
            stackAnalysis.Push(grammar.S);
        }
        protected virtual void OnTerminalFinish(Terminal terminal, string terminalToken) { }
        protected virtual void OnProcedureFinish(Production production) { }
        protected virtual void OnAnalyzerFinish() { }

        private void ProcedureStart()
        {
            Procedure(new Symbol[] { grammar.S });
        }

        private void Procedure(IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                if (symbol is Terminal terminal)
                {
                    if (symbol == Terminal.Epsilon)
                        continue;

                    if (sym == terminal)
                    {
                        OnTerminalFinish(terminal, symToken);
                        Advance();
                        continue;
                    }
                    else Error();
                    continue;
                }

                if (symbol is NonTerminal nonTerminal)
                {
                    var item = predictiveTable.SingleOrDefault(i => i.n == symbol && i.t == sym);
                    var p = item.p;
                    if (p == null)
                        Error();
                    else
                    {
                        Console.WriteLine(p);
                        Procedure(p.Right);
                        OnProcedureFinish(p);
                    }
                    continue;
                }

                throw new NotSupportedException();
            }

            
        }

        public void Analyzer()
        {
            ProcedureInit();
            Advance();
            ProcedureStart();
            OnAnalyzerFinish();
        }
    }
}
