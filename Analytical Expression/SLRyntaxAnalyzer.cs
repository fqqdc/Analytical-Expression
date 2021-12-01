using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class SLRyntaxAnalyzer
    {
        Dictionary<(HashSet<Production>, Symbol), HashSet<Production>> actionTable;
        Dictionary<(HashSet<Production>, Symbol), HashSet<Production>> gotoTable;
        Dictionary<NonTerminal, HashSet<Terminal>> followTable;
        HashSet<Production> state_0;
        NonTerminal startSymbol;

        public SLRyntaxAnalyzer(IEnumerable<Production> allProduction, Production firstProduction)
        {
            var nullableSet = GetNullableSet(allProduction);
            var firstSetsByNonTerminal = GetFirstSetsByNonTerminal(allProduction, nullableSet);
            followTable = GetFollowSets(allProduction, nullableSet, firstSetsByNonTerminal);
            var comparer = new DictEqualityComparer();
            actionTable = new(comparer);
            gotoTable = new(comparer);

            Symbol[] symbols = allProduction.SelectMany(p => p.Right.Append(p.Left)).Distinct().ToArray();
            startSymbol = firstProduction.Left;
            state_0 = Closure(new Production[] { firstProduction with { Position = 0 } }, allProduction);
            var set = new HashSet<HashSet<Production>>(HashSetEqualityComparer<Production>.Default);
            set.Add(state_0);
            var workQueue = new Queue<HashSet<Production>>();
            workQueue.Enqueue(state_0);
            while (workQueue.Count > 0)
            {
                var state = workQueue.Dequeue();
                foreach (var symbol in symbols)
                {
                    var newState = Goto(state, symbol, allProduction);
                    if (newState.Count == 0) continue;
                    if (symbol is Terminal terminal)
                        actionTable[(state, symbol)] = newState;
                    else
                    {
                        gotoTable[(state, symbol)] = newState;
                    }
                    if (!set.Contains(newState))
                    {
                        set.Add(newState);
                        workQueue.Enqueue(newState);
                    }
                }
            }
            PrintTable(actionTable, "ActionTable");
            PrintTable(gotoTable, "GotoTable");
        }

        public bool TryMatch(Terminal[] tokens)
        {
            int indexTokens = 0;
            var state = state_0;
            Stack<HashSet<Production>> stack = new();
            stack.Push(state);
            do
            {
                var token = tokens[indexTokens];
                indexTokens += 1;
                state = stack.Peek();
                if (actionTable.TryGetValue((state, token), out var nextState))
                {
                    if (nextState.All(p => p.Position < p.Right.Length))
                    {
                        stack.Push(nextState);
                    }
                    else
                    {
                        stack.Push(nextState);
                        var reduceProduction = nextState.Single(p => p.Position == p.Right.Length);
                        if (reduceProduction.Left == startSymbol)
                            return true;
                        var nextToken = tokens[indexTokens];                        
                        bool needReduce = followTable[reduceProduction.Left].Contains(nextToken);
                        while (needReduce)
                        {
                            needReduce = false;
                            reduceProduction.Right.ToList().ForEach(s => stack.Pop());
                            var nonTerminal = reduceProduction.Left;
                            if (gotoTable.TryGetValue((stack.Peek(), nonTerminal), out nextState))
                            {
                                reduceProduction = nextState.SingleOrDefault(p => p.Position == p.Right.Length);
                                if (reduceProduction != null)
                                    needReduce = followTable[reduceProduction.Left].Contains(nextToken);
                                stack.Push(nextState);
                            }
                            else return false;
                        }
                    }
                }
                else return false;
            }
            while (tokens.Length > indexTokens);
            return false;
        }

        Dictionary<NonTerminal, HashSet<Terminal>> GetFollowSets(IEnumerable<Production> allProduction,
                HashSet<NonTerminal> nullableSet, Dictionary<NonTerminal, HashSet<Terminal>> firstSetsNonTerminal)
        {
            var nonTerminals = allProduction.Select(p => p.Left).Distinct();
            Dictionary<NonTerminal, HashSet<Terminal>> followSets = new();

            foreach (var nonTerminal in nonTerminals)
            {
                followSets[nonTerminal] = new();
            }

            bool isChanged = true;
            while (isChanged)
            {
                isChanged = false;
                foreach (var production in allProduction)
                {
                    var tempSet = new HashSet<Terminal>(followSets[production.Left]);
                    foreach (Symbol symbol in production.Right.Reverse())
                    {
                        if (symbol is Terminal terminal)
                        {
                            tempSet.Add(terminal);
                        }

                        if (symbol is NonTerminal nonTerminal)
                        {
                            var oldCount = followSets[nonTerminal].Count;
                            followSets[nonTerminal].UnionWith(tempSet);
                            isChanged = isChanged || oldCount != followSets[nonTerminal].Count;

                            if (!nullableSet.Contains(nonTerminal))
                                tempSet = new(firstSetsNonTerminal[nonTerminal]);
                            else
                                tempSet.UnionWith(firstSetsNonTerminal[nonTerminal]);
                        }
                    }
                }
            }
            Print(followSets, "FOLLOW TABLE");
            return followSets;
        }
        Dictionary<NonTerminal, HashSet<Terminal>> GetFirstSetsByNonTerminal(IEnumerable<Production> allProduction,
            HashSet<NonTerminal> nullableSet)
        {
            var nonTerminals = allProduction.Select(p => p.Left).Distinct();
            Dictionary<NonTerminal, HashSet<Terminal>> firstSets = new();
            foreach (var nonTerminal in nonTerminals)
            {
                firstSets[nonTerminal] = new();
            }
            bool isChanged = true;
            while (isChanged)
            {
                isChanged = false;
                foreach (var production in allProduction)
                {
                    if (production.Right[0] is Terminal terminal)
                    {
                        var oldCount = firstSets[production.Left].Count;
                        firstSets[production.Left].Add(terminal);
                        isChanged = isChanged || firstSets[production.Left].Count != oldCount;
                    }
                    if (production.Right[0] is NonTerminal)
                    {
                        var oldCount = firstSets[production.Left].Count;
                        foreach (Symbol symbol in production.Right)
                        {
                            if (symbol is NonTerminal nonTerminal)
                                firstSets[production.Left].UnionWith(firstSets[nonTerminal]);
                            if (!nullableSet.Contains(symbol as NonTerminal))
                                break;
                        }
                        isChanged = isChanged || firstSets[production.Left].Count != oldCount;
                    }
                }
            }
            Print(firstSets, "FIRST TABLE");
            return firstSets;
        }
        HashSet<NonTerminal> GetNullableSet(IEnumerable<Production> allProduction)
        {
            HashSet<NonTerminal> nullableSet = new();
            bool isChanged = true;
            while (isChanged)
            {
                isChanged = false;
                foreach (var production in allProduction)
                {
                    if (production.Right[0] is Terminal terminal)
                    {
                        if (terminal.Name != String.Empty)
                            continue;
                        var oldCount = nullableSet.Count;
                        nullableSet.Add(production.Left);
                        isChanged = isChanged || nullableSet.Count != oldCount;
                    }
                    else
                    {
                        if (production.Right
                            .All(symbol => nullableSet.Contains(symbol as NonTerminal)))
                        {
                            var oldCount = nullableSet.Count;
                            nullableSet.Add(production.Left);
                            isChanged = isChanged || nullableSet.Count != oldCount;
                        }
                    }
                }
            }
            Print(nullableSet, "NULLABLE SET");
            return nullableSet;
        }

        private HashSet<Production> Goto(IEnumerable<Production> c, Symbol symbol, IEnumerable<Production> allProduction)
        {
            HashSet<Production> temp = new();
            foreach (Production item1 in
                c.Where(p => p.Position < p.Right.Length && p.Right[p.Position] == symbol))
            {
                temp.Add(item1 with { Position = item1.Position + 1 });
            }
            return Closure(temp, allProduction);
        }
        private HashSet<Production> Closure(IEnumerable<Production> c, IEnumerable<Production> allProduction)
        {
            HashSet<Production> set = new(c);
            bool isChanged = true;
            while (isChanged)
            {
                isChanged = false;
                HashSet<Production> newSet = new(set);
                foreach (var itemP in set.Where(p => p.Position < p.Right.Length))
                {
                    if (itemP.Right[itemP.Position] is NonTerminal nonT)
                    {
                        foreach (var production in allProduction.Where(p => p.Left == nonT))
                        {
                            newSet.Add(production with { Position = 0 });
                        }
                    }
                }
                isChanged = !set.SetEquals(newSet);
                set = newSet;
            }

            return set;
        }


        Dictionary<HashSet<Production>, int> ObjectIdTable = new(new SetEqualityComparer());
        [Conditional("DEBUG")]
        void PrintTable(Dictionary<(HashSet<Production>, Symbol), HashSet<Production>> table, string tableName)
        {
            Console.WriteLine();
            Console.WriteLine($"==== ==== {tableName} ==== ====");
            Console.WriteLine();
            foreach (var pair in table)
            {
                var set = pair.Key.Item1;
                if (!ObjectIdTable.TryGetValue(set, out int setId))
                {
                    setId = ObjectIdTable.Count;
                    ObjectIdTable[set] = setId;
                }
                Console.WriteLine($"State {setId}");
                PrintTableItem(pair.Key.Item1);

                set = pair.Value;
                if (!ObjectIdTable.TryGetValue(set, out setId))
                {
                    setId = ObjectIdTable.Count;
                    ObjectIdTable[set] = setId;
                }
                Console.WriteLine($"  {pair.Key.Item2}===>State {setId}");
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    var t = pair.Value.ElementAt(i);
                    if (i == 0)
                        Console.WriteLine($"  ----->{t}");
                    else Console.WriteLine($"        {t}");

                }
                Console.WriteLine();
            }
        }

        void PrintTableItem<T1>(IEnumerable<T1> set)
        {
            foreach (var t1 in set)
            {
                Console.WriteLine(t1);
            }
        }

        void Print<T1>(IEnumerable<T1> set, string name = "")
        {
            Console.WriteLine();
            Console.WriteLine($"==== ==== {name} ==== ====");
            Console.WriteLine();
            foreach (var t1 in set)
            {
                Console.WriteLine(t1);
            }
        }

        [Conditional("DEBUG")]
        void Print<T1, T2>(Dictionary<T1, T2> sets, string name = "") where T2 : IEnumerable
        {
            Console.WriteLine();
            Console.WriteLine($"==== ==== {name} ==== ====");
            Console.WriteLine();
            foreach (var pair in sets)
            {
                Console.WriteLine(pair.Key);
                foreach (var t in pair.Value)
                {
                    Console.WriteLine($"  ---->{t}");
                }
            }
        }

        class DictEqualityComparer : IEqualityComparer<(HashSet<Production>, Symbol)>
        {
            bool IEqualityComparer<(HashSet<Production>, Symbol)>.Equals((HashSet<Production>, Symbol) x, (HashSet<Production>, Symbol) y)
            {
                return x.Item2 == y.Item2 && x.Item1.SetEquals(y.Item1);
            }

            int IEqualityComparer<(HashSet<Production>, Symbol)>.GetHashCode((HashSet<Production>, Symbol) obj)
            {
                return 0;
            }
        }

        class SetEqualityComparer : IEqualityComparer<HashSet<Production>>
        {
            bool IEqualityComparer<HashSet<Production>>.Equals(HashSet<Production>? x, HashSet<Production>? y)
            {
                return x.SetEquals(y);
            }

            int IEqualityComparer<HashSet<Production>>.GetHashCode(HashSet<Production> obj)
            {
                return 0;
            }
        }
    }
}
