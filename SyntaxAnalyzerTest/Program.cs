﻿using SyntaxAnalyzer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LexicalAnalyzer;
using System.IO;

namespace SyntaxAnalyzerTest
{
    public class Program
    {
        static void Main(string[] args)
        {
            var listProduction = new List<Production>();
            listProduction.AddRange(Production.Create("Optional", "char - char|char|Optional char - char|Optional char"));
            listProduction.AddRange(Production.Create("Group", "( Exp )|char|charGroup|[ Optional ]"));
            listProduction.AddRange(Production.Create("Array", "Group ?|Group *|Group +"));
            listProduction.AddRange(Production.Create("JoinExp", "Group|Array|JoinExp Group|JoinExp Array"));
            listProduction.AddRange(Production.Create("OrExp", "JoinExp|OrExp or JoinExp"));
            listProduction.AddRange(Production.Create("Exp", "OrExp"));

            Grammar grammar = new Grammar(listProduction, new("Exp"));
            Console.WriteLine(grammar);


            //if (!LR0Grammar.TryCreate(grammar, out var rl0Grammar, out var rl0Msg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"LR0Grammar Error:\n{rl0Msg}");
            //}
            //else Console.WriteLine(rl0Grammar);

            //if (!SLRGrammar.TryCreate(grammar, out var slrGrammar, out var slrMsg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"SLRGrammar Error:\n{slrMsg}");
            //    return;
            //}
            //else Console.WriteLine(slrGrammar);

            Dictionary<(int state, Terminal t), List<ActionItem>>? actionTable = null;
            Dictionary<(int state, NonTerminal t), int>? gotoTable = null;
            FileInfo fileInfo = new("Regular.syntax");

            //actionTable = slrGrammar.GetAction();
            //gotoTable = slrGrammar.GetGoto();

            //using (var fs = fileInfo.Open(FileMode.Create))
            //using (var bw = new BinaryWriter(fs))
            //{
            //    actionTable.Save(bw);
            //    gotoTable.Save(bw);
            //}

            if (fileInfo.Exists)
            {
                using (var fs = fileInfo.Open(FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    actionTable = LRSyntaxAnalyzerHelper.LoadActionTable(br);
                    Console.WriteLine(actionTable.ToFullString());
                    gotoTable = LRSyntaxAnalyzerHelper.LoadGotoTable(br);
                    Console.WriteLine(gotoTable.ToFullString());
                }
            }
            else
            {
                throw new FileNotFoundException("找不到语法数据", "Regular.syntax");
            }

            //var clr1Grammar = CLR1Grammar.Create(grammar);

            //Console.WriteLine();
            //Console.WriteLine($"CLR1Grammar Conflict:\n{clr1Grammar.ConflictMessage}");

            //Console.WriteLine(clr1Grammar);

            //if (!LR1Grammar.TryCreate(grammar, out var lr1Grammar, out var lr1Msg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"LR1Grammar Error:\n{lr1Msg}");
            //    return;
            //}
            //else Console.WriteLine(lr1Grammar);

            //if (!LALRGrammar.TryCreate(grammar, out var lalrGrammar, out var lalrMsg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"LALRGrammar Error:\n{lalrMsg}");
            //}
            //else Console.WriteLine(lalrGrammar);


            //var digit = NFA.CreateRange('0', '9');
            //var letter = NFA.CreateRange('a', 'z').Or(NFA.CreateRange('A', 'Z'));
            //var escape = NFA.CreateFromString("\\\\",
            //    "\\|", "\\?", "\\*", "\\+", "\\.", "\\(", "\\)", "\\[", "\\]", "\\-");
            //var nfaChar = digit.Or(letter).Or(escape);

            //char[] opts = { '|', '?', '*', '+', '(', ')', '[', ']', '-' };
            
            //var nfaCharGroup = NFA.CreateFromString(".", "\\w", "\\s", "\\d");
            
            //var nfaSkip = NFA.CreateFrom(' ').Or(NFA.CreateFrom('\r')).Or(NFA.CreateFrom('\n'));

            //List<(NFA, Terminal)> list = new();
            //list.Add((nfaChar, new Terminal("char")));
            //list.Add((nfaCharGroup, new Terminal("charGroup")));
            //list.Add((nfaSkip, new Terminal("skip")));

            //List<Terminal> skipTerminals = new();
            //skipTerminals.Add(new Terminal("skip"));

            //foreach (var cOpt in opts)
            //{
            //    list.Add((NFA.CreateFrom(cOpt), new Terminal(cOpt.ToString())));
            //}

            fileInfo = new FileInfo("Regular.lexical");
            LexicalAnalyzer.LexicalAnalyzer? lexicalAnalyzer = null;

            //lexicalAnalyzer = new(list, skipTerminals);
            //using (var fs = fileInfo.Open(FileMode.Create))
            //using (var bw = new BinaryWriter(fs))
            //{
            //    lexicalAnalyzer.Save(bw);
            //}
            //lexicalAnalyzer = null;

            if (fileInfo.Exists)
            {
                using (var fs = fileInfo.Open(FileMode.Open))
                using (var br = new BinaryReader(fs))
                {
                    lexicalAnalyzer = new(br);
                }
            }

            StringBuilder stringToRead = new();
            stringToRead.AppendLine("[a-cA-C]+\\d*(ef)?\\*");
            using var reader = new StringReader(stringToRead.ToString());

            if (lexicalAnalyzer != null)
            {
                RegularLRSyntaxAnalyzer analyzer = new(actionTable, gotoTable, lexicalAnalyzer.GetEnumerator(reader));

                analyzer.Analyzer();
                if (analyzer.RegularNFA != null)
                {
                    var nfa = analyzer.RegularNFA;
                    Console.WriteLine(nfa);
                    var dfa = DFA.CreateFrom(nfa);
                    Console.WriteLine(dfa);
                    Console.WriteLine(dfa.Minimize());
                    Console.WriteLine("OK");
                }
            }
        }

        static void Main2(string[] args)
        {
            Production p = Production.CreateSingle("S", "a B c");
            Console.WriteLine(p);
            foreach (var item in ProductionItem.CreateSet(p))
            {
                Console.WriteLine(item);
            }

            int[] arr = new int[3];
            Console.WriteLine(arr is ICollection);
        }
    }
}
