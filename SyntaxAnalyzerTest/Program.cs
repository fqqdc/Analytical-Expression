using SyntaxAnalyzer;
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
        static void Main2(string[] args)
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

            if (!SLRGrammar.TryCreate(grammar, out var slrGrammar, out var slrMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"SLRGrammar Error:\n{slrMsg}");
                return;
            }
            else Console.WriteLine(slrGrammar);

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

            Dictionary<(int state, Terminal t), List<ActionItem>>? actionTable = null;
            Dictionary<(int state, NonTerminal t), int>? gotoTable = null;
            FileInfo syntaxFile = new("Regular.syntax");
            FileInfo lexicalFile = new FileInfo("Regular.lexical");
            LexicalAnalyzer.LexicalAnalyzer? lexicalAnalyzer = null;

            actionTable = slrGrammar.GetAction();
            gotoTable = slrGrammar.GetGoto();

            using (var fs = syntaxFile.Open(FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                actionTable.Save(bw);
                gotoTable.Save(bw);
            }

            var digit = NFA.CreateRange('0', '9');
            var letter = NFA.CreateRange('a', 'z').Or(NFA.CreateRange('A', 'Z'));
            var escape = NFA.CreateFromString("\\\\",
                "\\|", "\\?", "\\*", "\\+", "\\.", "\\(", "\\)", "\\[", "\\]", "\\-");

            List<(NFA, Terminal)> list = new();

            var nfaChar = digit.Or(letter).Or(escape);
            list.Add((nfaChar, new Terminal("char")));

            var nfaCharGroup = NFA.CreateFromString(".", "\\w", "\\s", "\\d");
            list.Add((nfaCharGroup, new Terminal("charGroup")));

            var nfaSkip = NFA.CreateFrom(' ').Or(NFA.CreateFrom('\r')).Or(NFA.CreateFrom('\n'));
            list.Add((nfaSkip, new Terminal("skip")));

            List<Terminal> skipTerminals = new();
            skipTerminals.Add(new Terminal("skip"));

            char[] opts = { '|', '?', '*', '+', '(', ')', '[', ']', '-' };
            foreach (var cOpt in opts)
            {
                list.Add((NFA.CreateFrom(cOpt), new Terminal(cOpt.ToString())));
            }

            lexicalAnalyzer = new(list, skipTerminals);
            using (var fs = lexicalFile.Open(FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                lexicalAnalyzer.Save(bw);
            }


        }

        static void Main(string[] args)
        {
            StringBuilder stringToRead = new();
            stringToRead.AppendLine("[a-cA-C]+\\d*(ef)?\\*");

            var analyzer = RegularLRSyntaxAnalyzer.Create();
            analyzer.Analyzer(stringToRead.ToString());
            if (analyzer.RegularNFA != null)
            {
                var nfa = analyzer.RegularNFA;
                //Console.WriteLine(nfa);
                var dfa = DFA.CreateFrom(nfa);
                //Console.WriteLine(dfa);
                Console.WriteLine(dfa.Minimize());
                Console.WriteLine("OK");
            }
        }
    }
}
