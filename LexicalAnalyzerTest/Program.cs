using LexicalAnalyzer;
using System;
using System.Linq;

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

            var digit = NFA.CreateRange('0', '3');
            var letter = NFA.CreateRange('a', 'c');
            // [a-c]([a-c]|[0-9])*
            var nfa2 = letter.Join(letter.Or(digit).Closure());

            nfa1 = aob.Closure().Join(c).Join(aob.Closure());
            nfa2 = aob.Closure().Join(cc).Join(aob.Closure());
            var nfa3 = aob.Closure().Join(c.Closure()).Join(aob.Closure());

            var dfa1 = DFA.CreateFrom(nfa1);
            var minDfa1 = dfa1.Minimize();
            nfa1 = minDfa1.ToNFA();
            var dfa2 = DFA.CreateFrom(nfa2);
            var minDfa2 = dfa2.Minimize();
            nfa2 = minDfa2.ToNFA();
            var dfa3 = DFA.CreateFrom(nfa3);
            var minDfa3 = dfa3.Minimize();
            nfa3 = minDfa3.ToNFA();


            Console.WriteLine(nfa1);
            Console.WriteLine(nfa2);
            Console.WriteLine(nfa3);

            var nfa = nfa1.UnionNFA(nfa2).UnionNFA(nfa3);
            //var nfa = nfa3;

            Console.WriteLine(nfa);
            var dfa = DFA.CreateFrom(nfa);
            Console.WriteLine(dfa);
            dfa = dfa.MinimizeByNfaFinal();
            Console.WriteLine(dfa);
            dfa = dfa.Minimize();
            Console.WriteLine(dfa);

        }
    }
}
