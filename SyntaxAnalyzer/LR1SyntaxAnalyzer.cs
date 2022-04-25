using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// LR1分析器
    /// </summary>
    public class LR1SyntaxAnalyzer : SyntaxAnalyzer
    {
        private LR1Grammar grammar;
        private AdvanceProcedure advanceProcedure;
        private Terminal sym = Terminal.Epsilon;
        private Dictionary<Symbol, int> symbolIndex = new();

        private Symbol[] symbolStack;
        private int[] stateStack;
        private int topStack = 0;

        private Dictionary<(int state, Terminal t), List<ActionItem>> actionTable;
        private Dictionary<(int state, NonTerminal t), int> gotoTable;

        private void InitStack()
        {
            symbolStack = new Symbol[8];
            stateStack = new int[8];
            topStack = -1;
        }

        private void Push(int state, Symbol symbol)
        {
            if (topStack + 1 == stateStack.Length)
            {
                int nextSize = stateStack.Length * 2;
                Array.Resize(ref stateStack, nextSize);
                Array.Resize(ref symbolStack, nextSize);
            }

            topStack++;
            stateStack[topStack] = state;
            symbolStack[topStack] = symbol;
        }

        private void Reduce(Production p)
        {
            var length = p.Right.Count();
            if (p.Right.SequenceEqual(Production.Epsilon))
                length = 0;
            topStack -= length;
            var state = stateStack[topStack];

            if (!gotoTable.TryGetValue((state, p.Left), out var nextState))
                Error();
            else
            {
                //Console.WriteLine($"Reduce:{p}");
                Push(nextState, p.Left);
            }
        }

        public LR1SyntaxAnalyzer(LR1Grammar grammar, AdvanceProcedure advanceProcedure)
        {
            this.grammar = grammar;
            this.advanceProcedure = advanceProcedure;
            this.actionTable = grammar.GetAction();
            this.gotoTable = grammar.GetGoto();
        }

        private void Advance()
        {
            advanceProcedure(out this.sym);
            //Console.WriteLine($"input:{this.sym}");
        }
        private void Error() { throw new Exception("语法分析错误"); }

        private void ProcedureInit()
        {
            InitStack();
            Push(0, Terminal.EndTerminal);
        }

        private void Procedure()
        {
            Advance();
            while (true)
            {
                var strState = string.Join(" ", stateStack.Take(topStack + 1));
                var strSymbol = string.Join(" ", symbolStack.Take(topStack + 1));
                Console.WriteLine($"{strState}, {strSymbol}, {sym}");

                var key = sym;
                if (sym is CharTerminal)
                    key = new Terminal(sym.Name);

                actionTable.TryGetValue((stateStack[topStack], key), out var actionItems);
                if (actionItems == null || actionItems.Count == 0)
                    Error();
                else
                {
                    if (actionItems[0] is ShiftItem shiftItem)
                    {
                        Push(shiftItem.State, sym);
                        Advance();
                    }
                    else if (actionItems[0] is ReduceItem reduceItem)
                    {
                        Reduce(reduceItem.Production);
                    }
                    else if (actionItems[0] is AcceptItem)
                    {
                        break;
                    }
                }
            }
            if (sym != Terminal.EndTerminal)
                Error();
        }

        public void Analyzer()
        {
            ProcedureInit();
            Procedure();
        }
    }
}
