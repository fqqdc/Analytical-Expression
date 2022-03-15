using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// LR(0)文法
    /// </summary>
    public class LR0Grammar : Grammar
    {
        private LR0Grammar(IEnumerable<Production> allProduction, NonTerminal startNonTerminal,
            Dictionary<(int state, Terminal t), List<ActionItem>> mapAction,
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

        public static bool TryCreate(Grammar grammar, [MaybeNullWhen(false)] out LR0Grammar oPGrammar, [MaybeNullWhen(true)] out string errorMsg)
        {
            oPGrammar = null;
            errorMsg = null;
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

            // ===== Print Table

            StringBuilder sbMatrix = new StringBuilder();
            var symbols = grammar.Vt.Cast<Symbol>()
                .Append(Terminal.EndTerminal)
                .Union(grammar.Vn)
                .ToArray();
            var states = Action.Keys.Select(key => key.state)
                .Union(Goto.Keys.Select(key => key.state))
                .Distinct().OrderBy(s => s).ToArray();

            for (i = -1; i < states.Length; i++)
            {
                // 打印列首
                if (i == -1)
                {
                    sbMatrix.Append("\t");
                }
                else
                {
                    sbMatrix.Append($"{states[i]}\t");
                }

                // 打印每一列
                for (int j = 0; j < symbols.Length; j++)
                {
                    var symbol = symbols[j];
                    if (i == -1)
                    {
                        // 行首
                        var strSymbol = symbol.ToString();
                        if (symbol == Terminal.EndTerminal)
                            strSymbol = "#";
                        sbMatrix.Append($"{strSymbol}\t");
                    }
                    else
                    {
                        var sbItem = new StringBuilder();
                        if (symbol is Terminal t)
                        {
                            if (Action.TryGetValue((i, t), out var list))
                            {
                                foreach (var item in list)
                                {
                                    if (item is ShiftItem si)
                                    {
                                        sbItem.Append($"s{si.State} ");
                                    }
                                    else if (item is ReduceItem ri)
                                    {
                                        sbItem.Append($"{ri.Production.Left}=>{string.Join("", ri.Production.Right)}");
                                    }
                                    else if (item is AcceptItem ai)
                                    {
                                        sbItem.Append($"acc ");
                                    }
                                }
                            }
                            if (sbItem.Length == 0)
                                sbItem.Append("");
                            sbItem.Append($"\t");
                            sbMatrix.Append(sbItem);
                        }
                        else if (symbol is NonTerminal n)
                        {
                            if (Goto.TryGetValue((i, n), out var state))
                            {
                                sbItem.Append($"{state}");
                            }
                            if (sbItem.Length == 0)
                                sbItem.Append("");
                            sbItem.Append($"\t");
                            sbMatrix.Append(sbItem);
                        }
                    }
                }
                sbMatrix.AppendLine();
            }
            Console.WriteLine(sbMatrix.ToString());

            foreach (var item in Action)
            {
                if (item.Value.Count > 1)
                    sbErrorMsg.AppendLine($"ACTION {item.Key} 有多重入口：({string.Join(",", item.Value)})");
            }

            errorMsg = sbErrorMsg.ToString();
            var result = string.IsNullOrWhiteSpace(errorMsg);
            if (result)
                oPGrammar = new(P, S_Ex, Action, Goto);
            return result;
        }

        private static (Dictionary<(int state, Terminal t), List<ActionItem>> Action, Dictionary<(int state, NonTerminal t), int> Goto)
            CreateItemSets(IEnumerable<Production> P, IEnumerable<Terminal> Vt, IEnumerable<NonTerminal> Vn, NonTerminal S)
        {
            var V = Vn.Cast<Symbol>().Union(Vt);
            var startProduction = P.Single(p => p.Left == S);

            // ================

            HashSet<ProductionItem> Closure(IEnumerable<ProductionItem> I)
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
                            if (closure.Add(newItem))
                                queueWork.Enqueue(newItem);
                        }
                    }
                }

                return closure;
            }

            Dictionary<(HashSet<ProductionItem> I, Symbol X), HashSet<ProductionItem>> GoCache = new(HashSetComparer<ProductionItem, Symbol>.Default);

            HashSet<ProductionItem> Go(HashSet<ProductionItem> I, Symbol X)
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

            // =================

            HashSet<HashSet<ProductionItem>> C = new(HashSetComparer<ProductionItem>.Default);
            Queue<HashSet<ProductionItem>> queueWork = new();

            var I_0 = Closure(new ProductionItem[] { new(startProduction, 0) }); // 初态项目集

            C.Add(I_0);
            queueWork.Enqueue(I_0);

            // 构造LR(0)项目集规范族
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

            // =================
            Dictionary<HashSet<ProductionItem>, int> IdTable = new(HashSetComparer<ProductionItem>.Default); // ID 表
            IdTable[I_0] = 0;

            Dictionary<(int state, Terminal t), List<ActionItem>> Action = new(); // ACTION 表
            List<ActionItem> GetActionItemList((int state, Terminal t) key)
            {
                if (!Action.TryGetValue(key, out var list))
                {
                    list = new List<ActionItem>();
                    Action[key] = list;
                }
                return list;
            }
            Dictionary<(int state, NonTerminal t), int> Goto = new(); // GOTO表

            var accept = new ProductionItem(startProduction, 1);

            queueWork = new();
            HashSet<HashSet<ProductionItem>> visited = new(HashSetComparer<ProductionItem>.Default);

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
                        var list = GetActionItemList((IdTable[I], Terminal.EndTerminal));
                        list.Add(new AcceptItem());
                    }
                    else if (item.Production.Right.Count() == item.Position)
                    {
                        foreach (var t in Vt)
                        {
                            var list = GetActionItemList((IdTable[I], t));
                            list.Add(new ReduceItem(item.Production));
                        }
                    }
                    else
                    {
                        var symbol = item.Production.Right.ElementAt(item.Position);
                        var J = Go(I, symbol);
                        if (!C.Contains(J))
                            continue;

                        if (!IdTable.TryGetValue(J, out var id_J))
                        {
                            id_J = IdTable.Count;
                            IdTable[J] = id_J;
                        }

                        if (!visited.Contains(J))
                        {
                            visited.Add(J);
                            queueWork.Enqueue(J);
                        }

                        if (symbol is Terminal terminal)
                        {
                            var list = GetActionItemList((IdTable[I], terminal));
                            list.Add(new ShiftItem(id_J));
                        }
                        else if (symbol is NonTerminal nonTerminal)
                        {
                            Goto[(IdTable[I], nonTerminal)] = id_J;
                        }
                    }
                }

                foreach (var n in Vn)
                {
                    var J = Go(I, n);
                    if (J.Count > 0)
                    {
                        if (!IdTable.TryGetValue(J, out var id_J))
                            id_J = IdTable.Count;
                        IdTable[J] = id_J;
                        Goto[(IdTable[I], n)] = id_J;
                    }
                }
            }

            // 打印项目集
            foreach (var I in C)
            {
                var id_I = IdTable[I];
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
                        var id_J = IdTable[J];
                        Console.WriteLine($"{symbol}->{id_J}");
                    }
                }
                Console.WriteLine();
            }

            return (Action, Goto);
        }
    }

    public abstract record ActionItem();
    public record ShiftItem(int State) : ActionItem;
    public record ReduceItem(Production Production) : ActionItem;
    public record AcceptItem() : ActionItem;
}