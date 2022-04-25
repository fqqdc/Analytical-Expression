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
            var aaobb = aa.Or(bb);
            // (a | b) * (aa | bb)(a | b) *
            var nfa1 = aob.Closure().Join(aa.Or(bb)).Join(aob.Closure());

            var digit = NFA.CreateRange('0', '3');
            var letter = NFA.CreateRange('a', 'c');
            // [a-z]([a-z]|[0-9])*
            var nfa2 = letter.UnionNFA(letter.Or(digit));

            //nfa1 = aob.Closure().Join(a).Join(aob.Closure());
            //nfa2 = aob.Closure().Join(b).Join(aob.Closure());

            var dfa1 = DFA.CreateFrom(nfa1);
            var minDfa1 = dfa1.Minimize();
            var dfa2 = DFA.CreateFrom(nfa2);
            var minDfa2 = dfa2.Minimize();

            var nfa = minDfa1.ToNFA().UnionNFA(minDfa2.ToNFA());
            

            //Console.WriteLine(nfa); 
            var dfa = DFA.CreateFrom(nfa);
            Console.WriteLine(dfa);
            dfa = dfa.Minimize();
            Console.WriteLine(dfa);

        }
    }
}
