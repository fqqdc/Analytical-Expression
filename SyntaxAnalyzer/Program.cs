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
            listProduction.AddRange(Production.Create("A", "aB"));
            listProduction.AddRange(Production.Create("B", "bC"));
            listProduction.AddRange(Production.Create("C", "cD|cE"));
            listProduction.AddRange(Production.Create("D", "dE"));
            listProduction.AddRange(Production.Create("E", "eD"));

            Grammar grammar = new Grammar(listProduction.AsEnumerable(), new("A"));

            Console.WriteLine(grammar);
            grammar = grammar.EliminateLeftRecursion();
            Console.WriteLine(grammar);
            //grammar = grammar.ExtractLeftCommonfactor();
            //Console.WriteLine(grammar);
        }

        static void Main2(string[] args)
        {
            int[] arr = { };
            Console.WriteLine(arr.Skip(1).Any());
        }
    }
}
