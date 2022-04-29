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
            //}
            //else Console.WriteLine(slrGrammar);

            //var clr1Grammar = CLR1Grammar.Create(grammar);

            //Console.WriteLine();
            //Console.WriteLine($"CLR1Grammar Conflict:\n{clr1Grammar.ConflictMessage}");

            //Console.WriteLine(clr1Grammar);

            if (!LR1Grammar.TryCreate(grammar, out var lr1Grammar, out var lr1Msg))
            {
                Console.WriteLine();
                Console.WriteLine($"LR1Grammar Error:\n{lr1Msg}");
                return;
            }
            else Console.WriteLine(lr1Grammar);

            //if (!LALRGrammar.TryCreate(grammar, out var lalrGrammar, out var lalrMsg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"LALRGrammar Error:\n{lalrMsg}");
            //}
            //else Console.WriteLine(lalrGrammar);

            var digit = NFA.CreateRange('0', '9');
            var letter = NFA.CreateRange('a', 'z').Or(NFA.CreateRange('A', 'Z'));
            char[] opts = { '|', '?', '*', '+', '(', ')', '[', ']', '-' };
            var escape = NFA.CreateFromString("\\\\",
                "\\|", "\\?", "\\*", "\\+", "\\.", "\\(", "\\)", "\\[", "\\]", "\\-");
            var escapeCharGroup = NFA.CreateFromString(".", "\\w", "\\s", "\\d");

            var nfaChar = digit.Or(letter).Or(escape);
            var nfaSkip = NFA.CreateFrom(' ').Or(NFA.CreateFrom('\r')).Or(NFA.CreateFrom('\n'));

            List<(NFA, Terminal)> list = new();
            list.Add((nfaChar, new Terminal("char")));
            list.Add((escapeCharGroup, new Terminal("charGroup")));
            list.Add((nfaSkip, new Terminal("skip")));

            List<Terminal> skipTerminals = new();
            skipTerminals.Add(new Terminal("skip"));

            foreach (var cOpt in opts)
            {
                list.Add((NFA.CreateFrom(cOpt), new Terminal(cOpt.ToString())));
            }

            LexicalAnalyzer.LexicalAnalyzer lexicalAnalyzer = new(list, skipTerminals);

            StringBuilder stringToRead = new();
            stringToRead.AppendLine("[a-cA-C]+\\d*(ef)?\\*");
            using var reader = new StringReader(stringToRead.ToString());

            RegularLRSyntaxAnalyzer analyzer = new(lr1Grammar.GetAction(), lr1Grammar.GetGoto(), lexicalAnalyzer.GetEnumerator(reader));

            analyzer.Analyzer();
            Console.WriteLine(DFA.CreateFrom(analyzer.RegularNFA).Minimize());
            Console.WriteLine("OK");
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
