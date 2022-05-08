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
        private Terminal sym = Terminal.Epsilon;
        private string symToken = string.Empty;

        private Symbol[] symbolStack = new Symbol[0];
        private int[] stateStack = new int[0];
        private int topStack = 0;

        private Dictionary<(int state, Terminal t), List<ActionItem>> actionTable;
        private Dictionary<(int state, NonTerminal t), int> gotoTable;

        private IEnumerator<(Terminal sym, string symToken)> symEnumerator;

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

        protected virtual void OnProcedureInit() { }
        protected virtual void OnShiftItem(Terminal terminal, String terminalToken) { }
        protected virtual void OnReduceItem(Production production) { }
        protected virtual void OnAcceptItem() { }

        public LRSyntaxAnalyzer(
            Dictionary<(int state, Terminal t), List<ActionItem>> actionTable,
            Dictionary<(int state, NonTerminal t), int> gotoTable)
        {
            this.actionTable = actionTable;
            this.gotoTable = gotoTable;

            this.symEnumerator = Enumerable.Empty<(Terminal sym, string symToken)>().GetEnumerator();
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
            //Console.WriteLine($"input:{sym},{symToken}");
        }
        private void Error() { throw new Exception("语法分析错误"); }

        private void ProcedureInit()
        {
            OnProcedureInit();
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
                //Console.WriteLine($"{strState}, {strSymbol}, {sym}");

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
        public void Analyzer(IEnumerator<(Terminal sym, string symToken)> symEnumerator)
        {
            this.symEnumerator = symEnumerator;

            ProcedureInit();
            Procedure();
        }
    }

    public static class LRSyntaxAnalyzerHelper
    {
        class Writer
        {
            private BinaryWriter bw;
            private int maxValue;
            public Writer(BinaryWriter bw, int maxValue)
            {
                this.bw = bw;
                this.maxValue = maxValue;
            }

            public void Write(int value)
            {
                switch (maxValue)
                {
                    case int x when x <= byte.MaxValue:
                        bw.Write((byte)value);
                        break;
                    case int x when x <= UInt16.MaxValue:
                        bw.Write((UInt16)value);
                        break;
                    default:
                        bw.Write(value);
                        break;
                }
            }
        }

        class Reader
        {
            private BinaryReader br;
            private int maxValue;
            public Reader(BinaryReader br, int maxValue)
            {
                this.br = br;
                this.maxValue = maxValue;
            }

            public int Read()
            {
                switch (maxValue)
                {
                    case int x when x <= byte.MaxValue:
                        return br.ReadByte();
                    case int x when x <= UInt16.MaxValue:
                        return br.ReadUInt16();
                    default:
                        return br.Read();
                }
            }
        }

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
            var symbolWriter = new Writer(bw, t2Id_size);
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
            var maxState = actionTable.Keys.Select(i => i.state)
                .Union(actionTable.Values
                    .SelectMany(lst => lst.Select(i => i is ShiftItem si ? si.State : 0)))
                .Max();
            bw.Write(maxState);
            var stateWriter = new Writer(bw, maxState);
            var maxActionItemSize = actionTable.Select(i => i.Value.Count).Max();
            bw.Write(maxActionItemSize);
            var actionItemSizeWriter = new Writer(bw, maxActionItemSize);

            for (int i = 0; i < table_size; i++)
            {
                var item = actionTable.ElementAt(i);
                stateWriter.Write(item.Key.state);
                symbolWriter.Write(s2Id[item.Key.t]);
                var actionItem_size = item.Value.Count;
                actionItemSizeWriter.Write(actionItem_size);
                for (int j = 0; j < actionItem_size; j++)
                {
                    var actionItem = item.Value[j];
                    switch (actionItem)
                    {
                        case ShiftItem shiftItem:
                            bw.Write('s');
                            stateWriter.Write(shiftItem.State);
                            break;
                        case ReduceItem reduceItem:
                            bw.Write('r');
                            symbolWriter.Write(s2Id[reduceItem.Production.Left]);
                            var pRight = reduceItem.Production.Right.ToArray();
                            bw.Write(pRight.Length);
                            for (int k = 0; k < pRight.Length; k++)
                            {
                                switch (pRight[k])
                                {
                                    case Terminal terminal:
                                        bw.Write('t');
                                        symbolWriter.Write(s2Id[terminal]);
                                        break;
                                    case NonTerminal nonTerminal:
                                        bw.Write('n');
                                        symbolWriter.Write(s2Id[nonTerminal]);
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
            var symbolReader = new Reader(br, id2S_size);
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
            var maxState = br.ReadInt32();
            var stateReader = new Reader(br, maxState);
            var maxActionItemSize = br.ReadInt32();
            var actionItemSizeReader = new Reader(br, maxActionItemSize);

            for (int i = 0; i < table_size; i++)
            {
                var key = (stateReader.Read(), (Terminal)id2S[symbolReader.Read()]);
                var value = new List<ActionItem>();
                var actionItem_size = actionItemSizeReader.Read();
                for (int j = 0; j < actionItem_size; j++)
                {
                    var actionItemType = br.ReadChar();
                    ActionItem actionItem;
                    switch (actionItemType)
                    {
                        case 's':
                            actionItem = new ShiftItem(stateReader.Read());
                            break;
                        case 'r':
                            NonTerminal left = (NonTerminal)id2S[symbolReader.Read()];
                            var pRight = new Symbol[br.ReadInt32()];
                            for (int k = 0; k < pRight.Length; k++)
                            {
                                var symbolType = br.ReadChar();
                                switch (symbolType)
                                {
                                    case 't':
                                        pRight[k] = id2S[symbolReader.Read()];
                                        break;
                                    case 'n':
                                        pRight[k] = id2S[symbolReader.Read()];
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
            var symbolWriter = new Writer(bw, t2Id_size);

            foreach (var s in listNoTerminal)
                bw.Write(s.Name);

            int table_size = gotoTable.Count;
            bw.Write(table_size);

            var maxState = gotoTable.Keys.Select(i => i.state)
                .Union(gotoTable.Values)
                .Max();
            bw.Write(maxState);
            var stateWriter = new Writer(bw, maxState);

            for (int i = 0; i < table_size; i++)
            {
                var item = gotoTable.ElementAt(i);
                stateWriter.Write(item.Key.state);
                symbolWriter.Write(s2Id[item.Key.t]);
                stateWriter.Write(item.Value);
            }
        }

        public static Dictionary<(int state, NonTerminal t), int> LoadGotoTable(BinaryReader br)
        {
            int id2S_size = br.ReadInt32();
            var symbolReader = new Reader(br, id2S_size);
            var id2S = new Dictionary<int, NonTerminal>();
            for (int i = 0; i < id2S_size; i++)
            {
                id2S[i] = new NonTerminal(br.ReadString());
            }

            Dictionary<(int state, NonTerminal t), int> gotoTable = new();

            int table_size = br.ReadInt32();
            var maxState = br.ReadInt32();
            var stateReader = new Reader(br, maxState);
            for (int i = 0; i < table_size; i++)
            {
                var key = (stateReader.Read(), id2S[symbolReader.Read()]);
                var value = stateReader.Read();
                gotoTable[key] = value;
            }
            return gotoTable;
        }
    }
}
