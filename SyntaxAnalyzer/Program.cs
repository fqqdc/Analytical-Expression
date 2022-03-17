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
        static void Main(string[] args)
        {
            var listProduction = new List<Production>();
            listProduction.AddRange(Production.Create("S", "A a|b A c|B c|b B a"));
            listProduction.AddRange(Production.Create("A", "d"));
            listProduction.AddRange(Production.Create("B", "d"));

            Grammar grammar = new Grammar(listProduction, new("S"));
            Console.WriteLine(grammar);

            if (!LR1Grammar.TryCreate(grammar, out var lr1Grammar, out var lr1Msg))
            {
                Console.WriteLine();
                Console.WriteLine($"LR1Grammar Error:\n{lr1Msg}");
            }
            else Console.WriteLine(lr1Grammar);

            if (!LALRGrammar.TryCreate(grammar, out var lalrGrammar, out var lalrMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"LALRGrammar Error:\n{lalrMsg}");
            }
            else Console.WriteLine(lalrGrammar);

            //if (!LR0Grammar.TryCreate(grammar, out var rl0Grammar, out var rl0Msg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"LR0Grammar Error:\n{rl0Msg}");
            //}
            //else Console.WriteLine(rl0Grammar);

            return;

            string strInput = "var i , i : char";
            int index = 0;

            SyntaxAnalyzer.AdvanceProcedure p = (out Terminal sym) =>
            {
                var input = strInput.Split(' ');
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

            //OPGSyntaxAnalyzer analyzer = new(newGrammar, p);

            //analyzer.Analyzer();
            Console.WriteLine("OK");
        }

        static void Main2(string[] args)
        {
            Production p = Production.CreateSingle("S", "a B c");
            Console.WriteLine(p);
            foreach (var item in ProductionItem.CreateSet(p))
            {
                Console.WriteLine(item);
            }

            int[] arr = new int[3];
            Console.WriteLine(arr is ICollection);
        }
    }
}
