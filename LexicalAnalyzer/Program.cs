using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexicalAnalyzer
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
            // var nfa = aob.Closure().Join(aa.Or(bb)).Join(aob.Closure());

            
            var digit = NFA.CreateRange('0', '3');
            var letter = NFA.CreateRange('a', 'c');
            // [a-z]([a-z]|[0-9])*
            var nfa = letter.Join(letter.Or(digit));

            Console.WriteLine(nfa);
            var dfa = DFA.CreateFrom(nfa);
            Console.WriteLine(dfa);
            dfa = dfa.Minimize();
            Console.WriteLine(dfa);

        }
    }
}
