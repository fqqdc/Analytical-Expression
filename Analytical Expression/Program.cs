#define DEBUG_PRINT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Collections;
using System.IO;
using System.Text;

namespace Analytical_Expression
{
    public class Program
    {
        static List<Production> AllProduction = new();
        static int _count = 0;
        static void MainLL1(string[] args)
        {
            //Productions.Add(CreateProduction("E", "E + T"));
            //Productions.Add(CreateProduction("E", "T"));
            //Productions.Add(CreateProduction("T", "T * F"));
            //Productions.Add(CreateProduction("T", "F"));
            //Productions.Add(CreateProduction("F", "n"));

            AllProduction.Add(CreateProduction("E", "T E'"));
            AllProduction.Add(CreateProduction("E'", "+ T E'"));
            AllProduction.Add(CreateProduction("E'", ""));
            AllProduction.Add(CreateProduction("T", "F T'"));
            AllProduction.Add(CreateProduction("T'", "* F T'"));
            AllProduction.Add(CreateProduction("T'", ""));
            AllProduction.Add(CreateProduction("F", "n"));

            //Productions.Add(CreateProduction("Z", "d"));
            //Productions.Add(CreateProduction("Z", "X Y Z"));
            //Productions.Add(CreateProduction("Y", "c"));
            //Productions.Add(CreateProduction("Y", ""));
            //Productions.Add(CreateProduction("X", "Y"));
            //Productions.Add(CreateProduction("X", "a"));

            var dict = GetLL1Table(AllProduction.Select(p => p.Left).Distinct(), AllProduction);

            Terminal[] tokens = CreateSymbols("n + n * n + n * n + n * n + n").Cast<Terminal>().ToArray();
            int indexTokens = 0;

            Stack<Symbol> workStack = new(new Symbol[] { CreateSymbol("E") });
            _count = 0;
            if (TryMatchByLL1(tokens, ref indexTokens, workStack, dict))
                //if (TryMatch(tokens, ref indexTokens, workStack))
                Console.WriteLine("ok");
            else Console.WriteLine("error");
            Console.WriteLine($"count:{_count}");


        }

        static void MainSLR(string[] args)
        {
            AllProduction.Add(CreateProduction("S'", "S $"));
            AllProduction.Add(CreateProduction("S", "L = R"));
            AllProduction.Add(CreateProduction("S", "R"));
            AllProduction.Add(CreateProduction("L", "* R"));
            AllProduction.Add(CreateProduction("L", "id"));
            AllProduction.Add(CreateProduction("R", "L"));

            Terminal[] tokens = CreateSymbols("x x x y y$").Cast<Terminal>().ToArray();

            SLRyntaxAnalyzer parser = new(AllProduction, AllProduction.Single(p => p.Left == new NonTerminal("S'")));

            //if (parser.TryMatch(tokens))
            //    Console.WriteLine("ok");
            //else Console.WriteLine("error");
        }

        static Dictionary<(NonTerminal, Terminal), IEnumerable<Production>> GetLL1Table(IEnumerable<NonTerminal> nonTerminals, IEnumerable<Production> allProduction)
        {
            var nullableSet = GetNullableSet(allProduction);
            var firstSetsByNonTerminal = GetFirstSetsByNonTerminal(allProduction, nullableSet);
            var followSets = GetFollowSets(allProduction, nullableSet, firstSetsByNonTerminal);
            var firstSetsByProduction = CreateFirstSetsByProduction(allProduction, nullableSet, firstSetsByNonTerminal, followSets);

            return BuildLL1Table(allProduction, firstSetsByProduction);

            static Dictionary<(NonTerminal, Terminal), IEnumerable<Production>> BuildLL1Table(IEnumerable<Production> productions,
                Dictionary<Production, HashSet<Terminal>> firstSetsByProduction)
            {
                Dictionary<(NonTerminal, Terminal), IEnumerable<Production>> returnTable = new();
                foreach (var production in productions)
                {
                    var nonTerminal = production.Left;
                    var set = firstSetsByProduction[production];
                    foreach (var terminal in set)
                    {
                        if (!returnTable.TryGetValue((nonTerminal, terminal), out var enumerator))
                        {
                            enumerator = new HashSet<Production>();
                            returnTable[(nonTerminal, terminal)] = enumerator;
                        }

                        var list = (HashSet<Production>)enumerator;
                        list.Add(production);
                    }
                }
                Print(returnTable, "LL(1)");
                return returnTable;
            }
            static Dictionary<Production, HashSet<Terminal>> CreateFirstSetsByProduction(IEnumerable<Production> productions,
                HashSet<NonTerminal> nullableSet, Dictionary<NonTerminal, HashSet<Terminal>> firstSets,
                 Dictionary<NonTerminal, HashSet<Terminal>> followSets)
            {
                Dictionary<Production, HashSet<Terminal>> pFirstSets = new();
                foreach (var production in productions)
                {
                    pFirstSets[production] = new();
                }

                foreach (var p in productions)
                {
                    bool allNullable = true;
                    foreach (var symbol in p.Right)
                    {
                        if (symbol is Terminal terminal)
                        {
                            pFirstSets[p].Add(terminal);
                            if (symbol.Name != string.Empty)
                            {
                                allNullable = false;
                                break;
                            }

                        }

                        if (symbol is NonTerminal nonTerminal)
                        {
                            pFirstSets[p].UnionWith(firstSets[nonTerminal]);

                            if (!nullableSet.Contains(nonTerminal))
                            {
                                allNullable = false;
                                break;
                            }
                        }
                    }
                    if (allNullable)
                        pFirstSets[p].UnionWith(followSets[p.Left]);
                }
                Print(pFirstSets, "FIRST_S");
                return pFirstSets;
            }
            static Dictionary<NonTerminal, HashSet<Terminal>> GetFollowSets(IEnumerable<Production> allProduction,
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
                Print(followSets, "FOLLOW");
                return followSets;
            }
            static Dictionary<NonTerminal, HashSet<Terminal>> GetFirstSetsByNonTerminal(IEnumerable<Production> allProduction,
                HashSet<NonTerminal> nullableSet)
            {
                var nonTerminals = AllProduction.Select(p => p.Left).Distinct();
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
                Print(firstSets, "FIRST");
                return firstSets;
            }
            static HashSet<NonTerminal> GetNullableSet(IEnumerable<Production> allProduction)
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
                Print(nullableSet, "NULLABLE");
                return nullableSet;
            }
        }
        static bool TryMatch(Terminal[] tokens, ref int indexTokens, Stack<Symbol> workStack)
        {
            _count += 1;
            Console.WriteLine("===========");

            while (workStack.Count > 0)
            {
                Console.WriteLine(string.Join(",", workStack.Select(s => s.Name)));
                Console.WriteLine("tokens: " + String.Join("", tokens.Take(indexTokens + 1).Select(t => t.Name).ToArray()));

                var symbol = workStack.Pop();

                Console.WriteLine(symbol);
                if (symbol is Terminal)
                {
                    if (symbol.Name == "")
                    {
                        continue;
                    }

                    var token = new Terminal(string.Empty);
                    if (indexTokens < tokens.Length)
                        token = tokens[indexTokens];

                    if (symbol == token)
                    {
                        indexTokens += 1;
                    }
                    else return false;
                }
                else
                {
                    var productions = AllProduction.Where(p => p.Left == symbol).ToList();
                    var r = false;
                    foreach (var subProduction in productions)
                    {
                        Stack<Symbol> subWorkStack = new(workStack.Reverse());
                        foreach (var subRightSymbol in subProduction.Right.Reverse())
                        {
                            subWorkStack.Push(subRightSymbol);
                        }
                        int oldIndexTokens = indexTokens;

                        r = TryMatch(tokens, ref indexTokens, subWorkStack);
                        if (r) return true;

                        indexTokens = oldIndexTokens;
                    }
                    return false;
                }
            }
            return indexTokens >= tokens.Length;
        }
        static bool TryMatchByLL1(Terminal[] tokens, ref int indexTokens, Stack<Symbol> workStack, Dictionary<(NonTerminal, Terminal), IEnumerable<Production>> lL1Table)
        {
            _count += 1;
            Console.WriteLine("===========");

            while (workStack.Count > 0)
            {
                Console.WriteLine(string.Join(",", workStack.Select(s => s.Name)));
                Console.WriteLine("tokens: " + String.Join("", tokens.Take(indexTokens + 1).Select(t => t.Name).ToArray()));

                var symbol = workStack.Pop();

                Console.WriteLine(symbol);
                if (symbol is Terminal terminal)
                {
                    if (terminal.Name == string.Empty)
                        continue;

                    var token = new Terminal(string.Empty);
                    if (indexTokens < tokens.Length)
                        token = tokens[indexTokens];

                    if (terminal == token)
                    {
                        indexTokens += 1;
                    }
                    else return false;
                }
                else
                {
                    var token = new Terminal(string.Empty);
                    if (indexTokens < tokens.Length)
                        token = tokens[indexTokens];

                    if (!lL1Table.TryGetValue(((NonTerminal)symbol, token), out var productions))
                        productions = new Production[0];
                    var r = false;
                    foreach (var subProduction in productions)
                    {
                        Stack<Symbol> subWorkStack = new(workStack.Reverse());
                        foreach (var subRightSymbol in subProduction.Right.Reverse())
                        {
                            subWorkStack.Push(subRightSymbol);
                        }
                        int oldIndexTokens = indexTokens;

                        r = TryMatchByLL1(tokens, ref indexTokens, subWorkStack, lL1Table);
                        if (r) return true;

                        indexTokens = oldIndexTokens;
                    }
                    return false;
                }
            }
            return true;
        }
        static Production CreateProduction(string left, string right)
        {
            var sLeft = new NonTerminal(left);
            var strRight = right.Split(' ', StringSplitOptions.TrimEntries);
            var sRight = strRight.Select(str => str.Length > 0 && char.IsUpper(str[0]) ? (Symbol)new NonTerminal(str) : (Symbol)new Terminal(str)).ToArray();
            return new Production(sLeft, sRight);
        }
        static Symbol[] CreateSymbols(string strInput)
        {
            return strInput.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(str => str.Trim())
                .Select(str => char.IsUpper(str[0]) ? (Symbol)new NonTerminal(str) : (Symbol)new Terminal(str))
                .ToArray();
        }
        static Symbol CreateSymbol(string strInput) => char.IsUpper(strInput[0]) ? (Symbol)new NonTerminal(strInput) : (Symbol)new Terminal(strInput);


        [Conditional("DEBUG_PRINT")]
        static void Print<T1, T2>(Dictionary<T1, T2> sets, string name = "") where T2 : IEnumerable
        {
            Console.WriteLine();
            Console.WriteLine($"==== ==== Print {name} ==== ====");
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
        [Conditional("DEBUG_PRINT")]
        static void Print<T1>(IEnumerable<T1> set, string name = "")
        {
            Console.WriteLine();
            Console.WriteLine($"==== ==== Print {name} ==== ====");
            Console.WriteLine();
            foreach (var t1 in set)
            {
                Console.WriteLine(t1);
            }
        }

        static void NFa_Dfa()
        {
            var I = NFA.CreateFrom("I");
            var N = NFA.CreateFrom("N");
            var T = NFA.CreateFrom("T");
            var O = NFA.CreateFrom("O");
            var IN = I.Join(N);
            var INTO = IN.Join(T).Join(O);
            var TO = T.Join(O);
            var NO = N.Join(O);
            var NOT = N.Join(O).Join(T);
            var ON = O.Join(N);

            var nfa = IN.Or(INTO).Or(TO);
            Console.WriteLine(nfa);
            var nfa2 = NO.Or(NOT).Or(ON);
            Console.WriteLine(nfa);
            Console.WriteLine(nfa2);

            var dfa = DFA.CreateFrom(nfa);
            Console.WriteLine(dfa);
            var dfa2 = DFA.CreateFrom(nfa2);
            Console.WriteLine(dfa2);

            var dfa_min = dfa.Minimize();
            Console.WriteLine(dfa_min);
            var dfa_min2 = dfa2.Minimize();
            Console.WriteLine(dfa_min2);

            var tfa = new TreeFA();
            tfa.Union(dfa_min);
            tfa.Union(dfa_min2);
            Console.WriteLine(tfa);

            Console.WriteLine(DFA.CreateFrom(tfa.ToNFA()));
        }

        static void Test()
        {
            List<Production> lstProduction = new();
            lstProduction.Add(("E", "E + T"));
            lstProduction.Add(("E", "T"));
            lstProduction.Add(("T", "T * F"));
            lstProduction.Add(("T", "F"));
            lstProduction.Add(("F", "( E )"));
            lstProduction.Add(("F", "i"));
            //lstProduction.Add(("S", "Q c"));
            //lstProduction.Add(("S", "c"));
            //lstProduction.Add(("Q", "R b"));
            //lstProduction.Add(("Q", "b"));
            //lstProduction.Add(("R", "S a"));
            //lstProduction.Add(("R", "a"));

            var g = new Grammar(lstProduction, new("E"));
            Console.WriteLine(g);

            g = EliminateLeftRecursion2(g);

            Console.WriteLine(g);
        }

        static Grammar EliminateLeftRecursion2(Grammar grammar)
        {
            var comparer = new ProductionComparer();
            var newSet = grammar.P.ToHashSet(comparer);
            var oldSet = newSet.ToHashSet();
            do
            {
                oldSet = newSet.ToHashSet();
                Queue<NonTerminal> queue = new();
                HashSet<NonTerminal> visited = new();
                queue.Enqueue(grammar.S);
                visited.Add(grammar.S);
                List<Production> list = new();
                while (queue.Count > 0)
                {
                    var nLeft = queue.Dequeue();
                    foreach (var p in oldSet.Where(p => p.Left == nLeft))
                    {
                        list.Add(p);
                        foreach (var n in p.Right.Where(n => n is NonTerminal).Cast<NonTerminal>())
                        {
                            if (!visited.Contains(n))
                            {
                                visited.Add(n);
                                queue.Enqueue(n);
                            }
                        }
                    }
                }
                list.Reverse();

                HashSet<Production> exceptSet = new();
                HashSet<Production> unionSet = new();
                for (int i = 0; i < list.Count; i++)
                {
                    var p1 = list[i];
                    if (p1.Right[0] is NonTerminal nonTerminal
                        && p1.Left != nonTerminal)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            var p2 = list[j];
                            if (p2.Left == nonTerminal)
                            {
                                exceptSet.Add(p1);
                                var newRight = p2.Right.Union(p1.Right.Skip(1)).ToArray();
                                unionSet.Add(new(p1.Left, newRight));
                            }
                        }
                    }
                }

                newSet = new(list, comparer);
                newSet.ExceptWith(exceptSet);
                newSet.UnionWith(unionSet);
                Print(newSet);

            } while (!newSet.SetEquals(oldSet));

            return EliminateLeftRecursion(newSet, grammar.S);
        }

        static Grammar EliminateLeftRecursion(HashSet<Production> set, NonTerminal S)
        {
            var newP = set.ToHashSet();
            bool hasChanged = true;
            while (hasChanged)
            {
                hasChanged = false;
                var p1 = newP.Where(p => p.Right.Length > 0)
                    .Where(p => p.Left == p.Right[0]).FirstOrDefault();
                if (p1 != null)
                {
                    hasChanged = true;

                    HashSet<Production> exceptSet = new();
                    HashSet<Production> unionSet = new();
                    foreach (var p2 in newP.Where(p => p.Left == p1.Left))
                    {
                        var newLeft = new NonTerminal(p1.Left.Name + "'");
                        exceptSet.Add(p2);
                        if (p2.Right[0].Name == String.Empty)
                            continue;

                        if (p2.Left == p2.Right[0])
                        {
                            var newRight = p2.Right.Skip(1).Append(newLeft).ToArray();
                            unionSet.Add(new(newLeft, newRight));
                            unionSet.Add(new(newLeft, new Symbol[0]));
                        }
                        else
                        {
                            var newRight = p2.Right.Append(newLeft).ToArray();
                            unionSet.Add(new(p1.Left, newRight));
                        }
                    }
                    newP.ExceptWith(exceptSet);
                    newP.UnionWith(unionSet);
                }
            }

            return new(newP, S);
        }

        static void Main(string[] args)
        {
            Test();
        }

    }

    class ProductionComparer : IEqualityComparer<Production>
    {
        bool IEqualityComparer<Production>.Equals(Production? x, Production? y)
        {
            var b = x.Left == y.Left && x.Right.Length == y.Right.Length;
            if (b)
            {
                for (int i = 0; i < x.Right.Length; i++)
                {
                    if (x.Right[i] != y.Right[i])
                        return false;
                }
                return true;
            }
            return false;
        }

        int IEqualityComparer<Production>.GetHashCode(Production obj)
        {
            return 0;
        }
    }
}