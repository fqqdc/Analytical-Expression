using SyntaxAnalyzer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LexicalAnalyzer;
using System.IO;

namespace SyntaxAnalyzerTest
{
    public class Program
    {
        static void LL1GrammarExample()
        {
            var listProduction = new List<Production>();

            listProduction.AddRange(Production.Create("ExpNumber", "number"));
            //listProduction.AddRange(Production.Create("ExpMulti", "ExpNumber|ExpMulti * ExpNumber")); // 乘法 (存在直接左递归)
            listProduction.AddRange(Production.Create("ExpMulti", "number ExpMulti'")); // 乘法
            listProduction.AddRange(Production.Create("ExpMulti'", "* ExpNumber ExpMulti|")); // 乘法
            //listProduction.AddRange(Production.Create("ExpAdd", "ExpMulti|ExpAdd + ExpMulti")); // 加法 (存在直接左递归)
            listProduction.AddRange(Production.Create("ExpAdd", "number ExpMulti' ExpAdd'")); // 加法
            listProduction.AddRange(Production.Create("ExpAdd'", "+ ExpMulti ExpAdd'|")); // 加法
            listProduction.AddRange(Production.Create("ExpArith", "ExpAdd")); // 算术表达式
            Grammar grammar = new Grammar(listProduction, new("ExpArith"));
            Console.WriteLine(grammar);

            //// 间接左递归文法例子
            //listProduction.AddRange(Production.Create("S", "Q c|"));
            //listProduction.AddRange(Production.Create("Q", "R b|b"));
            //listProduction.AddRange(Production.Create("R", "S a|a"));
            //Grammar grammar = new Grammar(listProduction, new("S"));
            //Console.WriteLine(grammar);

            if (!LL1Grammar.TryCreate(grammar, out var lL1Grammar, out var slrMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"Error:\n{slrMsg}");

                grammar = LL1Grammar.EliminateLeftRecursion(grammar); // 消除左递归
                Console.WriteLine(grammar);
                return;
            }
            else Console.WriteLine(lL1Grammar);
        }

        static void LR0GrammarExample()
        {
            var listProduction = new List<Production>();

            listProduction.AddRange(Production.Create("ExpNumber", "number"));
            listProduction.AddRange(Production.Create("ExpMulti", "ExpNumber|ExpMulti * ExpNumber")); // 乘法
            listProduction.AddRange(Production.Create("ExpAdd", "ExpMulti|ExpAdd + ExpMulti")); // 加法
            listProduction.AddRange(Production.Create("ExpArith", "ExpAdd")); // 算术表达式
            Grammar grammar = new Grammar(listProduction, new("ExpArith"));

            Console.WriteLine(grammar);

            LR0Grammar.PrintItemsSet = false;
            if (!LR0Grammar.TryCreate(grammar, out var lR0Grammar, out var slrMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"Error:\n{slrMsg}");

                return;
            }
            else Console.WriteLine(lR0Grammar);
        }

        static void LL2GrammarExample()
        {
            var listProduction = new List<Production>();

            //listProduction.AddRange(Production.Create("ExpNumber", "number"));
            ////listProduction.AddRange(Production.Create("ExpMulti", "ExpNumber|ExpMulti * ExpNumber")); // 乘法 (存在直接左递归)
            //listProduction.AddRange(Production.Create("ExpMulti", "number ExpMulti'")); // 乘法
            //listProduction.AddRange(Production.Create("ExpMulti'", "* ExpNumber ExpMulti|")); // 乘法
            ////listProduction.AddRange(Production.Create("ExpAdd", "ExpMulti|ExpAdd + ExpMulti")); // 加法 (存在直接左递归)
            //listProduction.AddRange(Production.Create("ExpAdd", "number ExpMulti' ExpAdd'")); // 加法
            //listProduction.AddRange(Production.Create("ExpAdd'", "+ ExpMulti ExpAdd'|")); // 加法
            //listProduction.AddRange(Production.Create("ExpArith", "ExpAdd")); // 算术表达式
            //Grammar grammar = new Grammar(listProduction, new("ExpArith"));

            //listProduction.AddRange(Production.Create("S", "if E then S S'|other"));
            //listProduction.AddRange(Production.Create("S'", "else S|"));
            //listProduction.AddRange(Production.Create("E", "b"));
            //Grammar grammar = new Grammar(listProduction, new("S"));

            //listProduction.AddRange(Production.Create("S", "A"));
            //listProduction.AddRange(Production.Create("A", "a B|a C|A d|A e"));
            //listProduction.AddRange(Production.Create("B", "b B C|f"));
            //listProduction.AddRange(Production.Create("C", "c"));
            //Grammar grammar = new Grammar(listProduction, new("S"));

            //listProduction.AddRange(Production.Create("A", "a A b c|B C f"));
            //listProduction.AddRange(Production.Create("A", "c|"));
            //listProduction.AddRange(Production.Create("B", "C d|"));
            //listProduction.AddRange(Production.Create("C", "d f|"));
            //Grammar grammar = new Grammar(listProduction, new("A"));

            listProduction.AddRange(Production.Create("E", "T|E'"));
            listProduction.AddRange(Production.Create("E'", "+ E|"));
            listProduction.AddRange(Production.Create("T", "F T'"));
            listProduction.AddRange(Production.Create("T'", "T|"));
            listProduction.AddRange(Production.Create("F", "P F'"));
            listProduction.AddRange(Production.Create("F'", "* F'|"));
            listProduction.AddRange(Production.Create("P", "( E )|a|^"));
            Grammar grammar = new Grammar(listProduction, new("E"));

            //grammar = LL1Grammar.EliminateLeftRecursion(grammar);
            //grammar = LL1Grammar.ExtractLeftCommonfactor(grammar);
            Console.WriteLine(grammar);

            LL2Grammar.PrintTable = true;
            if (!LL2Grammar.TryCreate(grammar, out var lL2Grammar, out var slrMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"Error:\n{slrMsg}");
            }
            else
            {
                Console.WriteLine(lL2Grammar);
            }

            LL1Grammar.PrintTable = true;
            if (!LL1Grammar.TryCreate(grammar, out var lL1Grammar, out slrMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"Error:\n{slrMsg}");
            }
            else
            {
                Console.WriteLine(lL1Grammar);
            }

        }

        static void Main(string[] args)
        {

        }
    }
}
