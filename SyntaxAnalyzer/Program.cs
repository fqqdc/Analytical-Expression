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
            listProduction.AddRange(Production.Create("S", "A a A b|B b B a"));
            listProduction.AddRange(Production.Create("A", ""));
            listProduction.AddRange(Production.Create("B", ""));

            Grammar grammar = new Grammar(listProduction, new("S"));
            Console.WriteLine(grammar);

            if (!LR0Grammar.TryCreate(grammar, out var newGrammar, out var msg))
            {
                Console.WriteLine($"LR1Grammar:\n{msg}");
            }
            else Console.WriteLine(newGrammar);

            if (!LR1Grammar.TryCreate(grammar, out var newGrammar2, out var msg1))
            {
                Console.WriteLine($"LR1Grammar:\n{msg1}");
            }
            else Console.WriteLine(newGrammar2);

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
