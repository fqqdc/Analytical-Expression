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
            Console.WriteLine(b);
            var ab = a.Join(b);
            Console.WriteLine(ab);
            var ab2 = a.Or(b);
            Console.WriteLine(ab2);
            var c3 = c.Closure();
            Console.WriteLine(c3);
            var nfa = ab.Or(ab2).Or(c3);
            Console.WriteLine(nfa);
            nfa = ab.Union(ab2).Union(c3);
            Console.WriteLine(nfa);
        }
    }
}
