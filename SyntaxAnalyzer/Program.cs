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
            listProduction.AddRange(Production.Create("S", "V1"));
            listProduction.AddRange(Production.Create("V1", "V2|V1 i V2"));
            listProduction.AddRange(Production.Create("V2", "V3|V2 + V3"));
            listProduction.AddRange(Production.Create("V3", ") V1 *|("));

            Grammar grammar = new Grammar(listProduction, new("S"));
            Console.WriteLine(grammar);

            if (!OPGrammar.TryCreate(grammar, out var opGrammar, out var msg))
            {
                Console.WriteLine(msg);
                return;
            }

            Console.WriteLine(opGrammar);

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

            OPGSyntaxAnalyzer analyzer = new(opGrammar, p);

            analyzer.Analyzer();
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
