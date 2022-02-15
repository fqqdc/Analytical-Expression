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
            //listProduction.AddRange(Production.Create("S", "iEtSeS|iEtS"));
            //listProduction.AddRange(Production.Create("E", "b"));
            listProduction.AddRange(Production.Create("E", "T E'"));
            listProduction.AddRange(Production.Create("E'", "+ T E'|"));
            listProduction.AddRange(Production.Create("T", "F T'"));
            listProduction.AddRange(Production.Create("T'", "* F T'|"));
            listProduction.AddRange(Production.Create("F", "( E )|i"));

            Grammar grammar = new Grammar(listProduction.AsEnumerable(), new("E"));
            Console.WriteLine(grammar);

            var lL1Grammar = LL1Grammar.CreateFrom(grammar);
            Console.WriteLine(lL1Grammar);

            string input = "ii+i*i";
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
