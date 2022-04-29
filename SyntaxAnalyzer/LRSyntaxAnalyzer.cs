using LexicalAnalyzer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// LR1分析器
    /// </summary>
    public class LRSyntaxAnalyzer
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

    public static class LRSyntaxAnalyzerHelper
    {
        public static void Save(this Dictionary<(int state, Terminal t), List<ActionItem>> actionTable, BinaryWriter bw)
        {
            var listTerminal = actionTable.Select(i => i.Key.t).Distinct().Cast<Symbol>();
            var listNoTerminal = actionTable.SelectMany(i => i.Value)
                .SelectMany(i =>
                {
                    if (i is ReduceItem reduce) { return reduce.Production.Right.Append(reduce.Production.Left); }
                    else return Enumerable.Empty<Symbol>();
                }).Distinct();
            var listSymbol = listTerminal.Union(listNoTerminal).Distinct().ToList();

            var s2Id = new Dictionary<Symbol, int>();
            foreach (var s in listSymbol)
                s2Id[s] = s2Id.Count;

            int t2Id_size = s2Id.Count;
            bw.Write(t2Id_size);
            foreach (var s in listSymbol)
            {
                switch (s)
                {
                    case Terminal t:
                        bw.Write('t');
                        break;
                    case NonTerminal n:
                        bw.Write('n');
                        break;
                    default: throw new NotSupportedException();
                }
                bw.Write(s.Name);
            }

            int table_size = actionTable.Count;
            bw.Write(table_size);
            for (int i = 0; i < table_size; i++)
            {
                var item = actionTable.ElementAt(i);
                bw.Write(item.Key.state);
                bw.Write(s2Id[item.Key.t]);
                var actionItem_size = item.Value.Count;
                bw.Write(actionItem_size);
                for (int j = 0; j < actionItem_size; j++)
                {
                    var actionItem = item.Value[j];
                    switch (actionItem)
                    {
                        case ShiftItem shiftItem:
                            bw.Write('s');
                            bw.Write(shiftItem.State);
                            break;
                        case ReduceItem reduceItem:
                            bw.Write('r');
                            bw.Write(s2Id[reduceItem.Production.Left]);
                            var pRight = reduceItem.Production.Right.ToArray();
                            bw.Write(pRight.Length);
                            for (int k = 0; k < pRight.Length; k++)
                            {
                                switch (pRight[k])
                                {
                                    case Terminal terminal:
                                        bw.Write('t');
                                        bw.Write(s2Id[terminal]);
                                        break;
                                    case NonTerminal nonTerminal:
                                        bw.Write('n');
                                        bw.Write(s2Id[nonTerminal]);
                                        break;
                                    default: throw new NotSupportedException();
                                }
                            }
                            break;
                        case AcceptItem acceptItem:
                            bw.Write('a');
                            break;
                        default: throw new NotSupportedException();
                    }
                }
            }
        }

        public static Dictionary<(int state, Terminal t), List<ActionItem>> LoadActionTable(BinaryReader br)
        {
            int id2S_size = br.ReadInt32();
            var id2S = new Dictionary<int, Symbol>();
            for (int i = 0; i < id2S_size; i++)
            {
                var symbolType = br.ReadChar();
                Symbol symbol;
                switch (symbolType)
                {
                    case 't':
                        symbol = new Terminal(br.ReadString());
                        break;
                    case 'n':
                        symbol = new NonTerminal(br.ReadString());
                        break;
                    default: throw new NotSupportedException();
                }
                id2S[i] = symbol;
            }

            Dictionary<(int state, Terminal t), List<ActionItem>> actionTable = new();

            int table_size = br.ReadInt32();
            for (int i = 0; i < table_size; i++)
            {
                var key = (br.ReadInt32(), (Terminal)id2S[br.ReadInt32()]);
                var value = new List<ActionItem>();
                var actionItem_size = br.ReadInt32();
                for (int j = 0; j < actionItem_size; j++)
                {
                    var actionItemType = br.ReadChar();
                    ActionItem actionItem;
                    switch (actionItemType)
                    {
                        case 's':
                            actionItem = new ShiftItem(br.ReadInt32());
                            break;
                        case 'r':
                            NonTerminal left = (NonTerminal)id2S[br.ReadInt32()];
                            var pRight = new Symbol[br.ReadInt32()];
                            for (int k = 0; k < pRight.Length; k++)
                            {
                                var symbolType = br.ReadChar();
                                switch (symbolType)
                                {
                                    case 't':
                                        pRight[k] = id2S[br.ReadInt32()];
                                        break;
                                    case 'n':
                                        pRight[k] = id2S[br.ReadInt32()];
                                        break;
                                    default: throw new NotSupportedException();
                                }
                            }
                            actionItem = new ReduceItem(new(left, pRight));
                            break;
                        case 'a':
                            actionItem = new AcceptItem();
                            break;
                        default: throw new NotSupportedException();
                    }
                    value.Add(actionItem);
                }
                actionTable[key] = value;
            }
            return actionTable;
        }

        public static void Save(this Dictionary<(int state, NonTerminal t), int> gotoTable, BinaryWriter bw)
        {
            var listNoTerminal = gotoTable.Select(i => i.Key.t).Distinct().ToList();

            var s2Id = new Dictionary<NonTerminal, int>();
            foreach (var s in listNoTerminal)
                s2Id[s] = s2Id.Count;

            int t2Id_size = s2Id.Count;
            bw.Write(t2Id_size);
            foreach (var s in listNoTerminal)
                bw.Write(s.Name);

            int table_size = gotoTable.Count;
            bw.Write(table_size);
            for (int i = 0; i < table_size; i++)
            {
                var item = gotoTable.ElementAt(i);
                bw.Write(item.Key.state);
                bw.Write(s2Id[item.Key.t]);
                bw.Write(item.Value);
            }
        }

        public static Dictionary<(int state, NonTerminal t), int> LoadGotoTable(BinaryReader br)
        {
            int id2S_size = br.ReadInt32();
            var id2S = new Dictionary<int, NonTerminal>();
            for (int i = 0; i < id2S_size; i++)
            {
                id2S[i] = new NonTerminal(br.ReadString());
            }

            Dictionary<(int state, NonTerminal t), int> gotoTable = new();

            int table_size = br.ReadInt32();
            for (int i = 0; i < table_size; i++)
            {
                var key = (br.ReadInt32(), id2S[br.ReadInt32()]);
                var value = br.ReadInt32();
                gotoTable[key] = value;
            }
            return gotoTable;
        }
    }
}
