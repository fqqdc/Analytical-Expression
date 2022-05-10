using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LexicalAnalyzer;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// LL1分析器（递归下降分析法）
    /// </summary>
    public class LL1SyntaxAnalyzerRD
    {
        public delegate void AdvanceProcedure(out Terminal Sym);

        private LL2Grammar grammar;
        private AdvanceProcedure advanceProcedure;
        private Terminal? sym;
        public LL1SyntaxAnalyzerRD(LL2Grammar grammar, AdvanceProcedure advanceProcedure)
        {
            this.grammar = grammar;
            this.advanceProcedure = advanceProcedure;
        }

        private Terminal Sym { get { return this.sym ?? Terminal.Epsilon; } }
        private Dictionary<Production, HashSet<Terminal>> rightFirst = new();
        private void Advance()
        {
            advanceProcedure(out this.sym);
            Console.WriteLine($"input:{this.sym}");
        }
        private void Error() { throw new Exception("语法分析错误"); }

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
                    if (Sym == terminal)
                    {
                        Advance();
                        continue;
                    }
                    else Error();
                }

                if (symbol is NonTerminal nonTerminal)
                {
                    bool matched = false;
                    foreach (var p in grammar.P.Where(p => p.Left == nonTerminal))
                    {
                        if (!rightFirst.TryGetValue(p, out var first))
                        {
                            first = grammar.CalcFirst(p.Right);
                            rightFirst[p] = first;
                        }

                        if (first.Contains(Sym))
                        {
                            Console.WriteLine(p);
                            Procedure(p.Right);
                            matched = true;
                            break;
                        }
                    }
                    if (!matched)
                    {
                        if (!grammar.GetFirst(nonTerminal).Contains(Terminal.Epsilon))
                            Error();
                        else
                            Console.WriteLine(new Production(nonTerminal, Production.Epsilon));
                    }
                }
            }
        }

        public void Analyzer()
        {
            Advance();
            ProcedureStart();
            if (Sym != Terminal.EndTerminal)
                Error();
        }
    }
}
