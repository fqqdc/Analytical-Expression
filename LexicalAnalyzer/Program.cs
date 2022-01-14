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
            var nfa = a.Join(b);
            Console.WriteLine(nfa);
        }
    }
}
