using LexicalAnalyzer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// LALR文法
    /// </summary>
    public class LALRGrammar : Grammar
    {
        private LALRGrammar(IEnumerable<Production> allProduction, NonTerminal startNonTerminal,
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
            return mapAction.ToDictionary(i => i.Key, i => i.Value.ToList());
        }
        public Dictionary<(int state, NonTerminal t), int> GetGoto()
        {
            return mapGoto.ToDictionary(i => i.Key, i => i.Value);
        }

        public static bool TryCreate(LR1Grammar grammar, [MaybeNullWhen(false)] out LALRGrammar lalrGrammar, out string errorMsg)
        {
            lalrGrammar = null;
            errorMsg = string.Empty;
            StringBuilder sbErrorMsg = new();

            var S = grammar.S;
            var P = grammar.P.ToHashSet();
            var Vn = grammar.Vn.ToHashSet();

            var (Action, Goto) = CreateItemSets(P, grammar.Vt, Vn, S);

            foreach (var item in Action)
            {
                if (item.Value.Count() > 1)
                    sbErrorMsg.AppendLine($"无法满足LALR文法：ACTION {item.Key} 有多重入口：({string.Join(",", item.Value)})");
            }

            errorMsg = sbErrorMsg.ToString();
            var result = string.IsNullOrWhiteSpace(errorMsg);

            if (LALRGrammar.PrintTable || !result && LALRGrammar.PrintTableIfConflict)
            {
                Console.WriteLine(LRGrammarHelper.GetTableFullString(grammar, Action, Goto));
            }

            if (result)
                lalrGrammar = new(P, S, Action, Goto);
            return result;
        }

        public static bool TryCreate(Grammar grammar, [MaybeNullWhen(false)] out LALRGrammar lalrGrammar, out string errorMsg)
        {
            lalrGrammar = null;
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
                    sbErrorMsg.AppendLine($"无法满足LALR文法：ACTION {item.Key} 有多重入口：({string.Join(",", item.Value)})");
            }

            errorMsg = sbErrorMsg.ToString();
            var result = string.IsNullOrWhiteSpace(errorMsg);

            if (LALRGrammar.PrintTable || !result && LALRGrammar.PrintTableIfConflict)
            {
                Console.WriteLine(LRGrammarHelper.GetTableFullString(grammar, Action, Goto));
            }

            if (result)
                lalrGrammar = new(P, S_Ex, Action, Goto);
            return result;
        }

        private static (Dictionary<(int state, Terminal t), HashSet<ActionItem>> Action, Dictionary<(int state, NonTerminal t), int> Goto)
            CreateItemSets(IEnumerable<Production> P, IEnumerable<Terminal> Vt, IEnumerable<NonTerminal> Vn, NonTerminal S)
        {
            var V = Vn.Cast<Symbol>().Union(Vt).Append(Terminal.EndTerminal);
            var startProduction = P.Single(p => p.Left == S);
            var mapFirst = Grammar.CalcFirsts(P);

            #region inter methods

            FixHashSet<ProductionItem_1> Closure(IEnumerable<ProductionItem_1> I)
            {
                HashSet<ProductionItem_1> closure = new(I);
                Queue<ProductionItem_1> queueWork = new(I);

                while (queueWork.Count > 0)
                {
                    var item = queueWork.Dequeue();
                    var symbol = item.Production.Right.ElementAtOrDefault(item.Position);
                    if (symbol != null && symbol is NonTerminal nonTerminal)
                    {
                        var tail = item.Production.Right.Skip(item.Position + 1).Append(item.Follow);
                        var fisrtSet = Grammar.CalcFirst(tail, mapFirst);
                        foreach (var first in fisrtSet)
                        {
                            foreach (var p in P.Where(p => p.Left == nonTerminal))
                            {
                                var newItem = new ProductionItem_1(p, 0, first);
                                // N -> eps  =>  N -> eps []
                                // 如果产生式为空，则生成归约项目
                                if (p.Right.SequenceEqual(Production.Epsilon))
                                    newItem = newItem with { Position = 1 };
                                if (closure.Add(newItem))
                                    queueWork.Enqueue(newItem);
                            }
                        }
                    }
                }

                return new(closure);
            }

            Dictionary<(FixHashSet<ProductionItem_1> I, Symbol X), FixHashSet<ProductionItem_1>> GoCache =
                new(FixHashSetComparer<ProductionItem_1, Symbol>.Default);
            FixHashSet<ProductionItem_1> Go(FixHashSet<ProductionItem_1> I, Symbol X)
            {
                if (!GoCache.TryGetValue((I, X), out var closureJ))
                {
                    var J = new HashSet<ProductionItem_1>();
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

            HashSet<FixHashSet<ProductionItem_1>> C = new(FixHashSetComparer<ProductionItem_1>.Default);

            Dictionary<(FixHashSet<ProductionItem_1> I, Symbol X), FixHashSet<ProductionItem_1>> GoConcentricCache =
                new(FixHashSetComparer<ProductionItem_1, Symbol>.Default);
            FixHashSet<ProductionItem_1> GoConcentric(FixHashSet<ProductionItem_1> I, Symbol X)
            {
                if (!GoConcentricCache.TryGetValue((I, X), out var concentricJ))
                {
                    var J = Go(I, X);
                    if (J.Count == 0) return J;
                    
                    concentricJ = C.First(set =>
                    {
                        // 查找同心项集合
                        IEqualityComparer<FixHashSet<ProductionItem_1>> comparer = LALRGrammarComparer.Default;
                        return comparer.Equals(set, J);
                    });
                    GoConcentricCache[(I, X)] = concentricJ;
                }
                return concentricJ;
            }

            #endregion

            // 项目集队列及初始化            
            Queue<FixHashSet<ProductionItem_1>> queueWork = new();
            var initItem = new ProductionItem_1(startProduction, 0, Terminal.EndTerminal); // 开始项目
            var I_0 = Closure(new ProductionItem_1[] { initItem }); // 初态项目集

            C.Add(I_0);
            queueWork.Enqueue(I_0);

            // 构造LR(1)项目集规范族
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

            // 合并同心集
            var groups = C.GroupBy(set => set, LALRGrammarComparer.Default);
            var newC = new HashSet<FixHashSet<ProductionItem_1>>(FixHashSetComparer<ProductionItem_1>.Default);
            foreach (var group in groups)
            {
                var newSet = group.SelectMany(set => set).ToHashSet();
                newC.Add(new(newSet));
            }
            C = newC;

            // 开始集
            I_0 = C.Single(I => I.Contains(initItem));

            // 分配集合ID
            Dictionary<FixHashSet<ProductionItem_1>, int> stateTable = new(FixHashSetComparer<ProductionItem_1>.Default); // ID 表
            stateTable[I_0] = 0;

            // Action
            Dictionary<(int state, Terminal t), HashSet<ActionItem>> actionTable = new(); // ACTION 表
            #region ACTION 方法
            HashSet<ActionItem> GetActionItemSet((int state, Terminal t) key)
            {
                if (!actionTable.TryGetValue(key, out var set))
                {
                    set = new();
                    actionTable[key] = set;
                }
                return set;
            }
            #endregion

            // Goto
            Dictionary<(int state, NonTerminal t), int> gotoTable = new(); // GOTO表

            // 接受项目
            var accept = new ProductionItem_1(startProduction, 1, Terminal.EndTerminal);

            // 遍历队列
            queueWork = new();
            // 已遍历集合
            HashSet<FixHashSet<ProductionItem_1>> visited = new(FixHashSetComparer<ProductionItem_1>.Default);

            // 初始化遍历队列
            queueWork.Enqueue(I_0);
            visited.Add(I_0);

            // 构造分析表
            while (queueWork.Count > 0)
            {
                var I = queueWork.Dequeue();
                foreach (var item in I)
                {
                    if (item == accept)
                    {
                        var list = GetActionItemSet((stateTable[I], Terminal.EndTerminal));
                        list.Add(new AcceptItem());
                    }
                    else if (item.Production.Right.Count() == item.Position)
                    {
                        var list = GetActionItemSet((stateTable[I], item.Follow));
                        list.Add(new ReduceItem(item.Production));
                    }
                    else
                    {
                        var symbol = item.Production.Right.ElementAt(item.Position);
                        var J = GoConcentric(I, symbol);
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
                            var list = GetActionItemSet((stateTable[I], terminal));
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
                        var J = GoConcentric(I, symbol);
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

    internal class LALRGrammarComparer : IEqualityComparer<FixHashSet<ProductionItem_1>>
    {
        static LALRGrammarComparer() => Default = new();
        public static LALRGrammarComparer Default { get; private set; }
        bool IEqualityComparer<FixHashSet<ProductionItem_1>>.Equals(FixHashSet<ProductionItem_1>? x, FixHashSet<ProductionItem_1>? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x != null && y != null)
            {
                var xSet = x.Select(i => new ProductionItem(i.Production, i.Position)).ToHashSet();
                var ySet = y.Select(i => new ProductionItem(i.Production, i.Position)).ToHashSet();
                return xSet.SetEquals(ySet);
            }

            return false;
        }
        int IEqualityComparer<FixHashSet<ProductionItem_1>>.GetHashCode(FixHashSet<ProductionItem_1> obj)
        {
            var code = 0;
            var set = obj.Select(i => new ProductionItem(i.Production, i.Position)).ToHashSet();
            foreach (var item in set)
            {
                if (item == null)
                    continue;
                code = code ^ item.GetHashCode();
            }
            return obj.GetHashCode();
        }
    }
}