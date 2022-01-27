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
            listProduction.AddRange(Production.Create("S", "Qc|c"));
            listProduction.AddRange(Production.Create("Q", "Rb|b"));
            listProduction.AddRange(Production.Create("R", "Sa|a"));

            Grammar grammar = new Grammar(listProduction.AsEnumerable(), new("S"));

            Console.WriteLine(grammar);
            Console.WriteLine(grammar.EliminateLeftRecursion());
        }
    }
}
