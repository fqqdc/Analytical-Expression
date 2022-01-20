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
            Console.WriteLine(a);
            var b = NFA.CreateFrom('b');
            Console.WriteLine(b);
            var c = NFA.CreateFrom('c');
            Console.WriteLine(c);
            var ab = a.Join(b);
            Console.WriteLine(ab);
            var ab2 = a.Or(b);
            Console.WriteLine(ab2);
            var c3 = c.Closure();
            Console.WriteLine(c3);

            var nfa = ab.Join(ab);
            Console.WriteLine(nfa);
        }
    }
}
