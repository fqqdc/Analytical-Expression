using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxAnalyzer
{
    public class Program
    {
        static void Main2(string[] args)
        {
            var listProduction = new List<Production>();
            listProduction.AddRange(Production.Create("A", "B C c|g D B"));
            listProduction.AddRange(Production.Create("B", "b C D E|"));
            listProduction.AddRange(Production.Create("C", "D a B|c a"));
            listProduction.AddRange(Production.Create("D", "d D|"));
            listProduction.AddRange(Production.Create("E", "g A f|c"));

            return;

            Grammar grammar = new Grammar(listProduction, new("E"));
            Console.WriteLine(grammar);

            Console.WriteLine();
            Console.WriteLine("消除左递归");
            grammar = LL1Grammar.EliminateLeftRecursion(grammar);
            Console.WriteLine(grammar);

            Console.WriteLine();
            Console.WriteLine("提取左公因式");
            grammar = LL1Grammar.ExtractLeftCommonfactor(grammar);
            Console.WriteLine(grammar);

            if (!LL1Grammar.TryCreateLL1Grammar(grammar, out var lL1Grammar, out var msg))
            {
                Console.WriteLine(msg);
                return;
            }



            Console.WriteLine(lL1Grammar);


            string input = "i+i";
            int index = 0;

            LL1SyntaxAnalyzerPT.AdvanceProcedure p = (out Terminal sym) =>
            {
                if (index < input.Length)
                {
                    var c = input[index];
                    sym = new Terminal(c.ToString());
                    index = index + 1;
                }
                else
                {
                    sym = Terminal.EndTerminal;
                }
            };

            LL1SyntaxAnalyzerPT analyzer = new(lL1Grammar, p);


            analyzer.Analyzer();
            Console.WriteLine("OK");
        }

    }
}