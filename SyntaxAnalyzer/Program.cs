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
            listProduction.AddRange(Production.Create("S", "W a"));
            listProduction.AddRange(Production.Create("W", "a"));
            listProduction.AddRange(Production.Create("W", "W b"));
            listProduction.AddRange(Production.Create("W", "W S"));
            //listProduction.AddRange(Production.Create("A", "A f|B"));
            //listProduction.AddRange(Production.Create("B", "D d e|D e"));
            //listProduction.AddRange(Production.Create("C", "e"));
            //listProduction.AddRange(Production.Create("D", "B f"));



            Grammar grammar = new Grammar(listProduction, new("S"));
            Console.WriteLine(grammar);

            var arr = SPGrammar.GetSymbolOrderArray(grammar);

            var equalMatrix = SPGrammar.GetEqualMatrix(grammar, arr);
            Console.WriteLine("=矩阵");
            Console.WriteLine(SPGrammar.FormatMatrix(equalMatrix, arr));

            var fisrtMatrix = SPGrammar.GetFirstMatrix(grammar, arr);
            Console.WriteLine("fisrt矩阵");
            Console.WriteLine(SPGrammar.FormatMatrix(fisrtMatrix, arr));

            var fisrtPlusMatrix = SPGrammar.ClosureMatrix(fisrtMatrix);
            Console.WriteLine("fisrt+矩阵");
            Console.WriteLine(SPGrammar.FormatMatrix(fisrtPlusMatrix, arr));


            return;

            if (!LL1Grammar.TryCreateLL1Grammar(grammar, out var lL1Grammar, out var msg))
            {
                Console.WriteLine(msg);
                return;
            }



            Console.WriteLine(lL1Grammar);


            string input = "i+i";
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

        static void Main1(string[] args)
        {
            int[,] arr = new int[2, 3];
            Console.WriteLine(arr.Rank);
        }
    }
}