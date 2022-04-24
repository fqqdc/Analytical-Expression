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
            listProduction.AddRange(Production.Create("Word", "char|char ?|char *"));
            listProduction.AddRange(Production.Create("Phrase", "Word|Phrase Word"));
            listProduction.AddRange(Production.Create("Complex", "Phrase|( Phrase ) ?|( Phrase ) *"));
            listProduction.AddRange(Production.Create("Exp", "Complex|Exp Complex"));

            Grammar grammar = new Grammar(listProduction, new("Exp"));
            Console.WriteLine(grammar);

            //if (!LR0Grammar.TryCreate(grammar, out var rl0Grammar, out var rl0Msg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"LR0Grammar Error:\n{rl0Msg}");
            //}
            //else Console.WriteLine(rl0Grammar);

            //if (!SLRGrammar.TryCreate(grammar, out var slrGrammar, out var slrMsg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"SLRGrammar Error:\n{slrMsg}");
            //}
            //else Console.WriteLine(slrGrammar);

            var clr1Grammar = CLR1Grammar.Create(grammar);

            Console.WriteLine();
            Console.WriteLine($"CLR1Grammar Conflict:\n{clr1Grammar.ConflictMessage}");

            Console.WriteLine(clr1Grammar);

            //if (!LR1Grammar.TryCreate(grammar, out var lr1Grammar, out var lr1Msg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"LR1Grammar Error:\n{lr1Msg}");
            //    return;
            //}
            //else Console.WriteLine(lr1Grammar);

            //if (!LALRGrammar.TryCreate(grammar, out var lalrGrammar, out var lalrMsg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"LALRGrammar Error:\n{lalrMsg}");
            //}
            //else Console.WriteLine(lalrGrammar);


            return;

            string strInput = "a b ( c * d ) ?";
            int index = 0;

            SyntaxAnalyzer.AdvanceProcedure p = (out Terminal sym) =>
            {
                var input = strInput.Split(' ');
                if (index < input.Length)
                {
                    var c = input[index];
                    if (c != "*" && c != "?" && c != "(" && c != ")")
                    {
                        sym = new Terminal("char");
                    }
                    else
                    {
                        sym = new Terminal(c.ToString()); ;
                    }
                    index = index + 1;
                }
                else
                {
                    sym = Terminal.EndTerminal;
                }
            };

            //LR1SyntaxAnalyzer analyzer = new(lr1Grammar, p);

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
