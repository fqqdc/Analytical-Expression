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

            var digit = NFA.CreateRange('0', '6');
            var letter = NFA.CreateRange('a', 'f');
            // [a-c]([a-c]|[0-9])*
            var nfa2 = letter.Join(letter.Or(digit).Closure());

            nfa1 = letter.Join(digit.Or(letter).Closure());
            nfa2 = digit.Join(digit.Closure());
            var nfa3 = digit.Or(letter).Join(digit.Or(letter).Closure());
            var nfaSkip = NFA.CreateFrom(' ').Or(NFA.CreateFrom('\r')).Or(NFA.CreateFrom('\n'));

            //var dfa1 = DFA.CreateFrom(nfa1);
            //var minDfa1 = dfa1.Minimize();
            //nfa1 = minDfa1.ToNFA();
            //var dfa2 = DFA.CreateFrom(nfa2);
            //var minDfa2 = dfa2.Minimize();
            //nfa2 = minDfa2.ToNFA();
            //var dfa3 = DFA.CreateFrom(nfa3);
            //var minDfa3 = dfa3.Minimize();
            //nfa3 = minDfa3.ToNFA();


            //Console.WriteLine(nfa1);
            //Console.WriteLine(nfa2);
            //Console.WriteLine(nfa3);

            //var nfa = nfa1.UnionNFA(nfa2).UnionNFA(nfaSpace);
            ////var nfa = nfa3;

            //Console.WriteLine(nfa);
            //var dfa = DFA.CreateFrom(nfa);
            //Console.WriteLine(dfa);
            //dfa = dfa.MinimizeByNfaFinal();
            //Console.WriteLine(dfa);
            //dfa = dfa.Minimize();
            //Console.WriteLine(dfa);


            List<(NFA, Terminal)> list = new();
            //list.Add((nfa1, new Terminal("id")));
            list.Add((nfa2, new Terminal("int")));
            list.Add((nfa3, new Terminal("other")));
            list.Add((nfaSkip, new Terminal("skip")));
            List<Terminal> skipTerminals = new();
            skipTerminals.Add(new Terminal("skip"));

            StringBuilder stringToRead = new StringBuilder();
            stringToRead.AppendLine("abc abc123");
            stringToRead.AppendLine("456 456edf");
            using var reader = new StringReader(stringToRead.ToString());
            LexicalAnalyzer.LexicalAnalyzer analyzer = new(list, skipTerminals);
            foreach (var item in analyzer.GetEnumerator(reader))
            {
                Console.WriteLine(item);
            }


        }
    }
}
