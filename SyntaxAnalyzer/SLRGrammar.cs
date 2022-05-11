using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using LexicalAnalyzer;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// SLR文法
    /// </summary>
    public class SLRGrammar : Grammar
    {
        private SLRGrammar(IEnumerable<Production> allProduction, NonTerminal startNonTerminal,
            Dictionary<(int state, Terminal t), HashSet<ActionItem>> mapAction,
            Dictionary<(int state, NonTerminal t), int> mapGoto
            ) : base(allProduction, startNonTerminal)
        {
            this.mapAction = mapAction.ToDictionary(i => i.Key, i => i.Value.ToList());
            this.mapGoto = mapGoto.ToDictionary(i => i.Key, i => i.Value);
        }
        private Dictionary<(int state, Terminal t), List<ActionItem>> mapAction;
        private Dictionary<(int state, NonTerminal t), int> mapGoto;

        public Dictionary<(int state, Terminal t), List<ActionItem>> GetAction()
        {
            return mapAction.ToDictionary(i => i.Key, i => i.Value);
        }
        public Dictionary<(int state, NonTerminal t), int> GetGoto()
        {
            return mapGoto.ToDictionary(i => i.Key, i => i.Value);
        }

        public static bool TryCreate(Grammar grammar, [MaybeNullWhen(false)] out SLRGrammar slrGrammar, out string errorMsg)
        {
            slrGrammar = null;
            errorMsg = string.Empty;
            StringBuilder sbErrorMsg = new();

            var S = grammar.S;
            var P = grammar.P.ToHashSet();
            var Vn = grammar.Vn.ToHashSet();

            // 扩展文法
            var S_Ex = new NonTerminal($"{S.Name}_Ex");
            int i = 0;
            while (Vn.Contains(S_Ex))
            {
                i++;
                S_Ex = new NonTerminal($"{S.Name}_Ex_{i}");
            }
            Vn.Add(S_Ex);
            P.Add(new Production(S_Ex, S));
            var (Action, Goto) = CreateItemSets(P, grammar.Vt, Vn, S_Ex);

            foreach (var item in Action)
            {
                if (item.Value.Count() > 1)
                    sbErrorMsg.AppendLine($"无法满足SLR文法：ACTION {item.Key} 有多重入口：{string.Join("; ", item.Value)}");
            }

            errorMsg = sbErrorMsg.ToString();
            var result = string.IsNullOrWhiteSpace(errorMsg);

            if (SLRGrammar.PrintTable || !result && SLRGrammar.PrintTableIfConflict)
            {
                Console.WriteLine(LRGrammarHelper.GetTableFullString(grammar, Action, Goto));
            }

            if (result)
                slrGrammar = new(P, S_Ex, Action, Goto);
            return result;
        }

        private static (Dictionary<(int state, Terminal t), HashSet<ActionItem>> Action, Dictionary<(int state, NonTerminal t), int> Goto)
            CreateItemSets(IEnumerable<Production> P, IEnumerable<Terminal> Vt, IEnumerable<NonTerminal> Vn, NonTerminal S)
        {
            var V = Vn.Cast<Symbol>().Union(Vt);
            var startProduction = P.Single(p => p.Left == S);

            #region inter methods

            FixHashSet<ProductionItem> Closure(IEnumerable<ProductionItem> I)
            {
                HashSet<ProductionItem> closure = new(I);
                Queue<ProductionItem> queueWork = new(I);

                while (queueWork.Count > 0)
                {
                    var item = queueWork.Dequeue();
                    var symbol = item.Production.Right.ElementAtOrDefault(item.Position);
                    if (symbol != null && symbol is NonTerminal nonTerminal)
                    {
                        foreach (var p in P.Where(p => p.Left == nonTerminal))
                        {
                            var newItem = new ProductionItem(p, 0);
                            // N -> eps  =>  N -> eps []
                            // 如果产生式为空，则生成归约项目
                            if (p.Right.SequenceEqual(Production.Epsilon))
                                newItem = newItem with { Position = 1 };
                            if (closure.Add(newItem))
                                queueWork.Enqueue(newItem);
                        }
                    }
                }

                return new(closure);
            }

            Dictionary<(FixHashSet<ProductionItem> I, Symbol X), FixHashSet<ProductionItem>> GoCache = new(FixHashSetComparer<ProductionItem, Symbol>.Default);

            FixHashSet<ProductionItem> Go(FixHashSet<ProductionItem> I, Symbol X)
            {
                if (!GoCache.TryGetValue((I, X), out var closureJ))
                {
                    var J = new HashSet<ProductionItem>();
                    foreach (var item in I)
                    {
                        var symbol = item.Production.Right.ElementAtOrDefault(item.Position);
                        if (symbol != null && symbol == X)
                        {
                            J.Add(item with { Position = item.Position + 1 });
                        }
                    }
                    closureJ = Closure(J);
                    GoCache[(I, X)] = closureJ;
                }
                return closureJ;
            }

            #endregion

            HashSet<FixHashSet<ProductionItem>> C = new(FixHashSetComparer<ProductionItem>.Default);
            Queue<FixHashSet<ProductionItem>> queueWork = new();

            var I_0 = Closure(new ProductionItem[] { new(startProduction, 0) }); // 初态项目集

            C.Add(I_0);
            queueWork.Enqueue(I_0);

            // 构造SLR项目集规范族
            while (queueWork.Count > 0)
            {
                var I = queueWork.Dequeue();
                foreach (var item in I)
                {
                    foreach (var symbol in V)
                    {
                        var newI = Go(I, symbol);
                        if (newI.Count != 0 && !C.Contains(newI))
                        {
                            C.Add(newI);
                            queueWork.Enqueue(newI);
                        }
                    }
                }
            }

            Dictionary<FixHashSet<ProductionItem>, int> stateTable = new(FixHashSetComparer<ProductionItem>.Default); // ID 表
            stateTable[I_0] = 0;

            Dictionary<(int state, Terminal t), HashSet<ActionItem>> actionTable = new(); // ACTION 表
            #region ACTION 方法
            HashSet<ActionItem> GetActionItemList((int state, Terminal t) key)
            {
                if (!actionTable.TryGetValue(key, out var list))
                {
                    list = new HashSet<ActionItem>();
                    actionTable[key] = list;
                }
                return list;
            }
            #endregion

            Dictionary<(int state, NonTerminal t), int> gotoTable = new(); // GOTO表

            var ACCEPT = new ProductionItem(startProduction, 1);
            var mapFirst = Grammar.CalcFirsts(P);
            var mapFollow = Grammar.CalcFollows(P, mapFirst, S);

            queueWork = new();
            HashSet<FixHashSet<ProductionItem>> visited = new(FixHashSetComparer<ProductionItem>.Default);

            queueWork.Enqueue(I_0);
            visited.Add(I_0);

            // 构造分析表
            while (queueWork.Count > 0)
            {
                var I = queueWork.Dequeue();
                foreach (var item in I)
                {
                    if (item == ACCEPT)
                    {
                        var list = GetActionItemList((stateTable[I], Terminal.EndTerminal));
                        list.Add(new AcceptItem());
                    }
                    else if (item.Production.Right.Count() == item.Position)
                    {
                        var follow = mapFollow[item.Production.Left];
                        foreach (var t in follow)
                        {
                            var list = GetActionItemList((stateTable[I], t));
                            list.Add(new ReduceItem(item.Production));
                        }
                    }
                    else
                    {
                        var symbol = item.Production.Right.ElementAt(item.Position);
                        var J = Go(I, symbol);
                        Debug.Assert(J.Count != 0);

                        if (!stateTable.TryGetValue(J, out var id_J))
                        {
                            id_J = stateTable.Count;
                            stateTable[J] = id_J;
                        }

                        if (!visited.Contains(J))
                        {
                            visited.Add(J);
                            queueWork.Enqueue(J);
                        }

                        if (symbol is Terminal terminal)
                        {
                            var list = GetActionItemList((stateTable[I], terminal));
                            list.Add(new ShiftItem(id_J));
                        }
                        else if (symbol is NonTerminal nonTerminal)
                        {
                            gotoTable[(stateTable[I], nonTerminal)] = id_J;
                        }
                    }
                }
            }

            if (PrintStateItems)
            {
                // 打印项目集
                var list = stateTable.OrderBy(i => i.Value).Select(i => i.Key);
                foreach (var I in list)
                {
                    var id_I = stateTable[I];
                    Console.WriteLine($"I_{id_I}");
                    foreach (var item in I)
                    {
                        Console.WriteLine(item);
                    }
                    foreach (var symbol in V)
                    {
                        var J = Go(I, symbol);
                        if (J.Count > 0)
                        {
                            var id_J = stateTable[J];
                            Console.WriteLine($"{symbol}->{id_J}");
                        }
                    }
                    Console.WriteLine();
                }
            }

            return (actionTable, gotoTable);
        }
    }
}