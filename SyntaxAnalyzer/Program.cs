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
            listProduction.AddRange(Production.Create("E", "a A|b B"));
            listProduction.AddRange(Production.Create("A", "c A|d"));
            listProduction.AddRange(Production.Create("B", "c B|d"));

            Grammar grammar = new Grammar(listProduction, new("E"));
            Console.WriteLine(grammar);

            if (!LR0Grammar.TryCreate(grammar, out var newGrammar, out var msg))
            {
                Console.WriteLine(msg);
                return;
            }

            Console.WriteLine(newGrammar);

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
