using LexicalAnalyzer;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace LexicalAnalyzerTest
{
    public class Program
    {
        static void Main(string[] args)
        {
            var a = NFA.CreateFrom('a');
            var b = NFA.CreateFrom('b');
            var c = NFA.CreateFrom('c');

            // a(b|c)*
            // var nfa = a.Join(b.Or(c).Closure());

            var aob = a.Or(b);
            var aa = a.Join(a);
            var bb = b.Join(b);
            var cc = c.Join(c);
            var aaobb = aa.Or(bb);
            // (a | b) * (aa | bb)(a | b) *
            var nfa1 = aob.Closure().Join(aa.Or(bb)).Join(aob.Closure());

            var digit = NFA.CreateRange('0', '9');
            var letter = NFA.CreateRange('a', 'z').Or(NFA.CreateRange('A', 'Z'));
            char[] opts = { '|', '?', '*', '+', '(', ')', '[', ']', '-' };
            var escape = NFA.CreateFromString("\\\\",
                "\\|", "\\?", "\\*", "\\+", "\\.", "\\(", "\\)", "\\[", "\\]", "\\-");
            var escapeCharGroup = NFA.CreateFromString(".", "\\w", "\\s", "\\d");
            // [a-c]([a-c]|[0-9])*
            var nfa2 = letter.Join(letter.Or(digit).Closure());

            nfa1 = digit.Or(letter).Or(escape);

            var nfaSkip = NFA.CreateFrom(' ').Or(NFA.CreateFrom('\r')).Or(NFA.CreateFrom('\n'));

            //var nfa = escape;

            //Console.WriteLine(nfa);
            //var dfa = DFA.CreateFrom(nfa);
            //Console.WriteLine(dfa);
            //dfa = dfa.MinimizeByNfaFinal();
            //Console.WriteLine(dfa);
            //dfa = dfa.Minimize();
            //Console.WriteLine(dfa);


            List<(NFA, Terminal)> list = new();
            list.Add((nfa1, new Terminal("char")));
            list.Add((escapeCharGroup, new Terminal("charGroup")));
            list.Add((nfaSkip, new Terminal("skip")));
            List<Terminal> skipTerminals = new();
            skipTerminals.Add(new Terminal("skip"));

            foreach (var cOpt in opts)
            {
                list.Add((NFA.CreateFrom(cOpt), new Terminal(cOpt.ToString())));
            }

            StringBuilder stringToRead = new StringBuilder();
            stringToRead.AppendLine("[abc]+\\d*\\s?.*");
            using var reader = new StringReader(stringToRead.ToString());
            LexicalAnalyzer.LexicalAnalyzer analyzer = new(list, skipTerminals);
            foreach (var item in analyzer.GetEnumerator(reader))
            {
                Console.WriteLine($"({item.terminal}, {item.token.Escape()})");
            }


        }
    }
}
