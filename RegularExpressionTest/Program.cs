using LexicalAnalyzer;
using RegularExpression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegularExpressionTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var analyzer = RegularLRSyntaxTranslater.LoadFromFile();

            analyzer.Translate("\\d+(\\.\\d*)?|\\.\\d+");
            if (analyzer.RegularNFA != null)
            {
                var nfa = analyzer.RegularNFA;
                //Console.WriteLine(nfa);
                var dfa = DFA.CreateFrom(nfa);
                //Console.WriteLine(dfa);
                Console.WriteLine(dfa.Minimize());
                Console.WriteLine(dfa.Minimize().ToNFA());
                Console.WriteLine("OK");
            }
        }
    }
}
