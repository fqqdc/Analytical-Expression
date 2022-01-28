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
            listProduction.AddRange(Production.Create("S", "iEtSeS|iEtS"));
            listProduction.AddRange(Production.Create("E", "b"));

            Grammar grammar = new Grammar(listProduction.AsEnumerable(), new("S"));

            Console.WriteLine(grammar);
            grammar = grammar.EliminateLeftRecursion();
            Console.WriteLine(grammar);
            grammar = grammar.ExtractLeftCommonfactor();
            Console.WriteLine(grammar);
        }

        static void Main2(string[] args)
        {
            int[] arr = { 1,2,3 };
            foreach (var item in arr.SkipLast(1).Concat(arr))
            {
                Console.WriteLine(item);
            }
        }
    }
}
