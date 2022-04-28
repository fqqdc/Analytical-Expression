using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LexicalAnalyzer;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// LR1分析器
    /// </summary>
    public class LRSyntaxAnalyzer : SyntaxAnalyzer
    {
        private IEnumerator<(Terminal sym, string symToken)> symEnumerator;
        private Terminal sym = Terminal.Epsilon;
        private string symToken = string.Empty;

        private Symbol[] symbolStack = new Symbol[0];
        private int[] stateStack = new int[0];
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
            var right = symbolStack.Skip(topStack + 1).Take(length).ToArray();

            if (!gotoTable.TryGetValue((state, p.Left), out var nextState))
                Error();
            else
            {
                //Console.WriteLine($"Reduce:{p}");
                Push(nextState, p.Left);
            }
        }
        protected virtual void OnShiftItem(Terminal terminal, String terminalToken) { }
        protected virtual void OnReduceItem(Production production) { }
        protected virtual void OnAcceptItem() { }

        public LRSyntaxAnalyzer(
            Dictionary<(int state, Terminal t), List<ActionItem>> actionTable,
            Dictionary<(int state, NonTerminal t), int> gotoTable,
            IEnumerator<(Terminal sym, string symToken)> symEnumerator)
        {
            this.symEnumerator = symEnumerator;
            this.actionTable = actionTable;
            this.gotoTable = gotoTable;
        }

        private void Advance()
        {
            if (symEnumerator.MoveNext())
            {
                var item = symEnumerator.Current;
                sym = item.sym;
                symToken = item.symToken;
            }
            else
            {
                sym = Terminal.EndTerminal;
                symToken = String.Empty;
            }
            Console.WriteLine($"input:{sym},{symToken}");
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

                actionTable.TryGetValue((stateStack[topStack], sym), out var actionItems);
                if (actionItems == null || actionItems.Count == 0)
                    Error();
                else
                {
                    if (actionItems[0] is ShiftItem shiftItem)
                    {
                        Push(shiftItem.State, sym);                        
                        OnShiftItem(sym, symToken);
                        Advance();
                    }
                    else if (actionItems[0] is ReduceItem reduceItem)
                    {
                        Reduce(reduceItem.Production);
                        OnReduceItem(reduceItem.Production);
                    }
                    else if (actionItems[0] is AcceptItem)
                    {
                        OnAcceptItem();
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
