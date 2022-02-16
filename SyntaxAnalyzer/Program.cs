using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxAnalyzer
{
    public class Program
    {
        static void Main(string[] args)
        {
            var listProduction = new List<Production>();
            listProduction.AddRange(Production.Create("S", "V1"));
            listProduction.AddRange(Production.Create("V1", "V2|V1 i V2"));
            listProduction.AddRange(Production.Create("V2", "V3|V2 + V3"));
            listProduction.AddRange(Production.Create("V3", "V1 *|("));
            //listProduction.AddRange(Production.Create("S", "Q c|c"));
            //listProduction.AddRange(Production.Create("Q", "R b|b"));
            //listProduction.AddRange(Production.Create("R", "S a|a"));

            Grammar grammar = new Grammar(listProduction, new("S"));
            Console.WriteLine(grammar);

            Console.WriteLine();
            Console.WriteLine("消除左递归");
            grammar = LL1Grammar.EliminateLeftRecursion(grammar);
            Console.WriteLine(grammar);

            Console.WriteLine();
            Console.WriteLine("提取左公因式");
            grammar = LL1Grammar.ExtractLeftCommonfactor(grammar);
            Console.WriteLine(grammar);

            if (!LL1Grammar.CheckLL1Grammar(grammar, out var msg))
            {
                Console.WriteLine(msg);
                return;
            }

                var lL1Grammar = LL1Grammar.CreateFrom(grammar);
                Console.WriteLine(lL1Grammar);


            return;

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

        static void Main2(string[] args)
        {
        }

    }
}
