﻿using System;
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
            //listProduction.AddRange(Production.Create("S", "a A d|b B d|a B e|b A e"));
            //listProduction.AddRange(Production.Create("A", "c"));
            //listProduction.AddRange(Production.Create("B", "c"));
            listProduction.AddRange(Production.Create("S", "L = R|R"));
            listProduction.AddRange(Production.Create("L", "* R|i"));
            listProduction.AddRange(Production.Create("R", "L"));

            Grammar grammar = new Grammar(listProduction, new("S"));
            Console.WriteLine(grammar);

            if (!LR1Grammar.TryCreate(grammar, out var newGrammar2, out var msg1))
            {
                Console.WriteLine($"LR1Grammar:\n{msg1}");
            }
            else Console.WriteLine(newGrammar2);

            if (!LALRGrammar.TryCreate(grammar, out var newGrammar, out var msg))
            {
                Console.WriteLine($"LALRGrammar:\n{msg}");
            }
            else Console.WriteLine(newGrammar);

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
