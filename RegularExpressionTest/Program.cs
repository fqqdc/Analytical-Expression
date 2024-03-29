﻿using LexicalAnalyzer;
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

            //analyzer.Translate("\\d+(\\.\\d*)?|\\.\\d+");
            //analyzer.Translate("[123]+(\\.[123]*)?|\\.[123]+");
            analyzer.Translate("[abcd]*c|[abcd]*bc|[abcd]*bcd|[abcd]*abcd");
            if (analyzer.RegularNFA != null)
            {
                var nfa = analyzer.RegularNFA;
                Console.WriteLine(nfa);
                var dfa = DFA.CreateFrom(nfa);
                Console.WriteLine(dfa);
                Console.WriteLine(dfa.Minimize());
                Console.WriteLine(dfa.MinimizeByZ());
                //Console.WriteLine(dfa.Minimize().ToNFA());
                Console.WriteLine("OK");
                
            }
        }
    }
}
