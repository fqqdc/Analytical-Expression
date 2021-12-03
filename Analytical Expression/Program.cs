#define DEBUG_PRINT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Collections;


namespace Analytical_Expression
{
    public class Program
    {
        static void LexicalAnalyzer_Analyze()
        {
            //n = [0-9]
            //nn *| nn *.|.nn *| nn *.nn *
            var exp_n = NfaDigraphCreater.CreateCharacterRange('0', '9');
            var exp_dot = NfaDigraphCreater.CreateSingleCharacter('.');
            var exp_nns = exp_n.Join(exp_n.Closure());
            var nfa = exp_nns.Join(exp_dot).Join(exp_nns);
            nfa = nfa.Union(exp_nns);
            nfa = nfa.Union(exp_nns.Join(exp_dot));
            nfa = nfa.Union(exp_dot.Join(nfa));
            DfaDigraphNode dfa = DfaDigraphCreater.CreateFrom(nfa);
            var dfaNumber = dfa.Minimize();
            DfaDigraphCreater.PrintDigraph(dfaNumber, false);
            StateMachine smNumber = new(dfaNumber) { Name = "Number" };

            //n = [0-9]
            //c = [a-z][A-Z]
            //c(c|n)*
            exp_n = NfaDigraphCreater.CreateCharacterRange('0', '9');
            var exp_c = NfaDigraphCreater.CreateCharacterRange('a', 'z');
            exp_c = exp_c.Union(NfaDigraphCreater.CreateCharacterRange('A', 'Z'));
            nfa = exp_c.Union(exp_n).Closure();
            nfa = exp_c.Join(nfa);
            dfa = DfaDigraphCreater.CreateFrom(nfa);
            var dfaId = dfa.Minimize();
            DfaDigraphCreater.PrintDigraph(dfaId, false);
            StateMachine smId = new(dfaId) { Name = "ID" };


            // +|-|*|/|<|<=|==|>=|>
            nfa = NfaDigraphCreater.CreateSingleCharacter('+'); // +
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('-')); // -
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('*')); // *
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('/')); // /
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('<')); // <
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('>')); // >
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('>').Join(NfaDigraphCreater.CreateSingleCharacter('='))); // >=
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('<').Join(NfaDigraphCreater.CreateSingleCharacter('='))); // <=
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('=').Join(NfaDigraphCreater.CreateSingleCharacter('='))); // ==
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('=')); // =
            dfa = DfaDigraphCreater.CreateFrom(nfa);
            var dfaSymbol = dfa.Minimize();
            DfaDigraphCreater.PrintDigraph(dfaSymbol, false);
            StateMachine smSymbol = new(dfaSymbol) { Name = "Symbol" };

            // (
            nfa = NfaDigraphCreater.CreateSingleCharacter('('); // (
            dfa = DfaDigraphCreater.CreateFrom(nfa);
            var dfaLeft = dfa.Minimize();
            StateMachine smLeft = new(dfaLeft) { Name = "L" };

            // )
            nfa = NfaDigraphCreater.CreateSingleCharacter(')'); // )
            dfa = DfaDigraphCreater.CreateFrom(nfa);
            var dfaRight = dfa.Minimize();
            StateMachine smRight = new(dfaRight) { Name = "R" };

            List<StateMachine> listSM = new() { smNumber, smId, smSymbol, smLeft, smRight };

            LexicalAnalyzer_Old analyzer = new(listSM);
            string txt = " 2 *(  3+(4-5) ) / 666>= ccc233  ";
            analyzer.Analyze(txt); ;




        }

        static void NFa_Dfa()
        {
            // a(b|c) *
            var nfa = NfaDigraphCreater.CreateSingleCharacter('b') // b
                .Union(NfaDigraphCreater.CreateSingleCharacter('c')) // b|c
                .Closure(); // (b|c)*
            nfa = NfaDigraphCreater.CreateSingleCharacter('a').Join(nfa); // a(b|c) *

            // fee | fie
            //var nfa = NfaDigraphCreater.CreateSingleCharacter('f').Join(NfaDigraphCreater.CreateSingleCharacter('e')).Join(NfaDigraphCreater.CreateSingleCharacter('e'))
            //    .Union(NfaDigraphCreater.CreateSingleCharacter('f').Join(NfaDigraphCreater.CreateSingleCharacter('i')).Join(NfaDigraphCreater.CreateSingleCharacter('e'))
            //    );

            // [a-z]([a-z])*
            //var nfa = NfaDigraphCreater.CreateCharacterRange('a', 'z').Join(NfaDigraphCreater.CreateCharacterRange('a', 'z').Closure());

            // a|aa|aaa
            //var exp_a = NfaDigraphCreater.CreateSingleCharacter('a');
            //var exp_aa = exp_a.Join(exp_a);
            //var exp_aaa = exp_a.Join(exp_a).Join(exp_a);
            //var nfa = exp_a.Union(exp_aa).Union(exp_aaa);

            //n = [0-9]
            //nn*|nn*.|.nn*|nn*.nn*
            //var exp_n = nfadigraphcreater.createcharacterrange('0', '9');
            //var exp_dot = nfadigraphcreater.createsinglecharacter('.');
            //var exp_nns = exp_n.join(exp_n.closure());
            //var nfa = exp_nns.join(exp_dot).join(exp_nns);
            //nfa = nfa.union(exp_nns);
            //nfa = nfa.union(exp_nns.join(exp_dot));
            //nfa = nfa.union(exp_dot.join(nfa));

            NfaDigraphCreater.PrintDigraph(nfa);

            Console.WriteLine("=============");
            DfaDigraphNode dfa = DfaDigraphCreater.CreateFrom(nfa);
            DfaDigraphCreater.PrintDigraph(dfa, false);

            Console.WriteLine("=============");
            var dmin = dfa.Minimize();
            DfaDigraphCreater.PrintDigraph(dmin, false);
        }

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

        static void MainDigraph(string[] args)
        {
            var listProduction = new List<Production>();
            listProduction.Add(CreateProduction("S'", "S"));
            //listProduction.Add(CreateProduction("S", "L = R"));
            //listProduction.Add(CreateProduction("S", "R"));
            //listProduction.Add(CreateProduction("L", "* F"));
            //listProduction.Add(CreateProduction("L", "i"));
            //listProduction.Add(CreateProduction("R", "L"));

            listProduction.Add(CreateProduction("S", "a S b"));
            listProduction.Add(CreateProduction("S", "a b"));

            var nfaNodeList = new List<DigraphNode<Production, Symbol>>();
            var epsSymbol = new Terminal("eps");
            foreach (var production in listProduction)
            {
                DigraphNode<Production, Symbol>? lastNode = null;
                for (int i = 0; i <= production.Right.Length; i++)
                {
                    var subProduction = production with { Position = i };
                    var newNode = new DigraphNode<Production, Symbol>() { Content = subProduction };
                    nfaNodeList.Add(newNode);
                    if (lastNode != null)
                        lastNode.Edges.Add((production.Right[i - 1], newNode));
                    lastNode = newNode;
                }
            }

            foreach (var node in nfaNodeList)
            {
                var production = node.Content;
                if (production.Right.Length > production.Position)
                {
                    if (production.Right[production.Position] is NonTerminal nonTerminal)
                    {
                        foreach (var node2 in nfaNodeList.Where(n => n.Content.Left == nonTerminal && n.Content.Position == 0))
                        {
                            node.Edges.Add((epsSymbol, node2));
                        }
                    }
                }
            }

            var n0 = nfaNodeList[0];
            Console.WriteLine(n0.GetDigraphString());

            Console.WriteLine("=========");
            var dfa_0 = n0.CreateDFA(epsSymbol, listProduction.SelectMany(p => p.Right.Append(p.Left)));
            Console.WriteLine(dfa_0.GetDigraphString());
        }

        static void Main(string[] args)
        {
            NFA f = NFA.CreateFrom(new("f"));
            NFA e = NFA.CreateFrom(new("e"));
            NFA i = NFA.CreateFrom(new("i"));

            var nfa = f.Join(e).Join(e).Union(f.Join(i).Join(e));
            Console.WriteLine(nfa);

            DFA dfa = DFA.CreateFrom(nfa);
            Console.WriteLine(dfa);

            DFA dfaMin = dfa.Minimize();
            Console.WriteLine(dfaMin);

            nfa = dfaMin.ToNFA();
            Console.WriteLine(nfa);
        }

    }

}