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
        }

        static void Main2(string[] args)
        {
            Stack<int> s = new();
            for (int i = 0; i < 10; i++)
            {
                s.Push(i);
            }

            foreach (var i in s)
            {
                Console.Write(i);
            }
            Console.WriteLine();
            List<int> list = new(s);
            foreach (var i in list)
            {
                Console.Write(i);
            }
            Console.WriteLine();
        }
    }
}
