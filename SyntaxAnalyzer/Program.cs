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
            listProduction.AddRange(Production.Create("Group", "( Exp )|char|[ char - char ]"));
            listProduction.AddRange(Production.Create("Array", "Group ?|Group *|Group +"));
            listProduction.AddRange(Production.Create("JoinExp", "Group|Array|JoinExp Group|JoinExp Array"));
            listProduction.AddRange(Production.Create("OrExp", "JoinExp|OrExp or JoinExp"));
            listProduction.AddRange(Production.Create("Exp", "OrExp"));

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

            //var clr1Grammar = CLR1Grammar.Create(grammar);

            //Console.WriteLine();
            //Console.WriteLine($"CLR1Grammar Conflict:\n{clr1Grammar.ConflictMessage}");

            //Console.WriteLine(clr1Grammar);

            if (!LR1Grammar.TryCreate(grammar, out var lr1Grammar, out var lr1Msg))
            {
                Console.WriteLine();
                Console.WriteLine($"LR1Grammar Error:\n{lr1Msg}");
                return;
            }
            else Console.WriteLine(lr1Grammar);

            //if (!LALRGrammar.TryCreate(grammar, out var lalrGrammar, out var lalrMsg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"LALRGrammar Error:\n{lalrMsg}");
            //}
            //else Console.WriteLine(lalrGrammar);

            string strInput = "[ a - z ] * ( b | c ) z z z ( c * d ) ? | a b c";
            int index = 0;

            SyntaxAnalyzer.AdvanceProcedure p = (out Terminal sym) =>
            {
                var input = strInput.Split(' ');
                if (index < input.Length)
                {
                    var c = input[index];

                    switch (c)
                    {
                        case "(":
                        case ")":
                        case "?":
                        case "+":
                        case "*":
                        case "[":
                        case "-":
                        case "]":
                            sym = new Terminal(c.ToString()); ;
                            break;
                        case "|":
                            sym = new Terminal("or"); ;
                            break;
                        default:
                            sym = new CharTerminal(Char.Parse(c));
                            break;
                    }

                    index = index + 1;
                }
                else
                {
                    sym = Terminal.EndTerminal;
                }
            };

            LR1SyntaxAnalyzer analyzer = new(lr1Grammar, p);

            Console.WriteLine(strInput);
            analyzer.Analyzer();
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
