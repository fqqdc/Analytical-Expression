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
            listProduction.AddRange(Production.Create("S", "if Eb then E else E"));
            listProduction.AddRange(Production.Create("E", "E + T|T"));
            listProduction.AddRange(Production.Create("T", "T * F|F"));
            listProduction.AddRange(Production.Create("F", "i"));
            listProduction.AddRange(Production.Create("Eb", "b"));

            Grammar grammar = new Grammar(listProduction, new("S"));
            Console.WriteLine(grammar);

            if (!OPGrammar.TryCreate(grammar, out var opGrammar, out var msg))
            {
                Console.WriteLine(msg);
                return;
            }

            Console.WriteLine(opGrammar);

            return;

            string input = "cce";
            int index = 0;

            SyntaxAnalyzer.AdvanceProcedure p = (out Terminal sym) =>
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

            //SPGSyntaxAnalyzer analyzer = new(spGrammar, p);

            //analyzer.Analyzer();
            Console.WriteLine("OK");
        }

        static void Main1(string[] args)
        {
            Stack<int> stack = new Stack<int>();
            for (int i = 0; i < 10; i++)
            {
                stack.Push(i);
            }

            foreach (var item in stack.TakeWhile((i, index) => index < 3))
            {
                Console.WriteLine(item);
            }
        }
    }
}
