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

        static void Main(string[] args)
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

            LL2Grammar.PrintTable = true;
            if (!LL2Grammar.TryCreate(grammar, out var lL1Grammar, out var slrMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"Error:\n{slrMsg}");

                return;
            }

            //Console.WriteLine(lL1Grammar);

        }
    }
}
