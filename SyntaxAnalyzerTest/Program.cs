using SyntaxAnalyzer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LexicalAnalyzer;
using System.IO;
using System.Collections.Immutable;

namespace SyntaxAnalyzerTest
{
    public class Program
    {
        static void LL1GrammarExample()
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
            //Console.WriteLine(grammar);

            // 间接左递归文法例子
            listProduction.AddRange(Production.Create("S", "Q c|"));
            listProduction.AddRange(Production.Create("Q", "R b|b"));
            listProduction.AddRange(Production.Create("R", "S a|a"));
            Grammar grammar = new Grammar(listProduction, new("S"));
            Console.WriteLine(grammar);

            grammar = LL1Grammar.EliminateLeftRecursion(grammar); // 消除左递归
            if (!LL1Grammar.TryCreate(grammar, out var lL1Grammar, out var slrMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"Error:\n{slrMsg}");

                Console.WriteLine(grammar);
                return;
            }
            else Console.WriteLine(lL1Grammar);
        }

        static void LL2GrammarExample()
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

            //listProduction.AddRange(Production.Create("E", "T|E'"));
            //listProduction.AddRange(Production.Create("E'", "+ E|"));
            //listProduction.AddRange(Production.Create("T", "F T'"));
            //listProduction.AddRange(Production.Create("T'", "T|"));
            //listProduction.AddRange(Production.Create("F", "P F'"));
            //listProduction.AddRange(Production.Create("F'", "* F'|"));
            //listProduction.AddRange(Production.Create("P", "( E )|a|^"));
            //Grammar grammar = new Grammar(listProduction, new("E"));

            //grammar = LL1Grammar.EliminateLeftRecursion(grammar); //消除左递归
            //grammar = LL1Grammar.ExtractLeftCommonfactor(grammar); // 提取左公因式
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

            //LL1Grammar.PrintTable = true;
            //if (!LL1Grammar.TryCreate(grammar, out var lL1Grammar, out slrMsg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"Error:\n{slrMsg}");
            //}
            //else
            //{
            //    Console.WriteLine(lL1Grammar);
            //}

        }

        static void LR0GrammarExample()
        {
            var listProduction = new List<Production>();

            //listProduction.AddRange(Production.Create("ExpNumber", "number"));
            //listProduction.AddRange(Production.Create("ExpMulti", "ExpNumber|ExpMulti * ExpNumber")); // 乘法
            //listProduction.AddRange(Production.Create("ExpAdd", "ExpMulti|ExpAdd + ExpMulti")); // 加法
            //listProduction.AddRange(Production.Create("ExpArith", "ExpAdd")); // 算术表达式
            //Grammar grammar = new Grammar(listProduction, new("ExpArith"));

            listProduction.AddRange(Production.Create("S", "E"));
            listProduction.AddRange(Production.Create("E", "a A|b B"));
            listProduction.AddRange(Production.Create("A", "c A|d"));
            listProduction.AddRange(Production.Create("B", "c B|d"));
            Grammar grammar = new Grammar(listProduction, new("E"));

            Console.WriteLine(grammar);
            LR0Grammar.PrintStateItems = false;
            LR0Grammar.PrintTable = true;
            if (!LR0Grammar.TryCreate(grammar, out var lR0Grammar, out var slrMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"Error:\n{slrMsg}");

                return;
            }
            else Console.WriteLine(lR0Grammar);
        }

        static void SLRGrammarExample()
        {
            var listProduction = new List<Production>();

            listProduction.AddRange(Production.Create("ExpNumber", "number"));
            listProduction.AddRange(Production.Create("ExpMulti", "ExpNumber|ExpMulti * ExpNumber")); // 乘法
            listProduction.AddRange(Production.Create("ExpAdd", "ExpMulti|ExpAdd + ExpMulti")); // 加法
            listProduction.AddRange(Production.Create("ExpArith", "ExpAdd")); // 算术表达式
            Grammar grammar = new Grammar(listProduction, new("ExpArith"));

            //// 非SLR文法的例子
            //listProduction.AddRange(Production.Create("S", "L = R|R"));
            //listProduction.AddRange(Production.Create("L", "* R|i"));
            //listProduction.AddRange(Production.Create("R", "L"));
            //Grammar grammar = new Grammar(listProduction, new("S"));

            Console.WriteLine(grammar);
            string? createMsg = null;

            //LR0Grammar.PrintStateItems = false;
            //LR0Grammar.PrintTable = true;
            //if (!LR0Grammar.TryCreate(grammar, out var lR0Grammar, out createMsg))
            //{
            //    Console.WriteLine();
            //    Console.WriteLine($"Error:\n{createMsg}");
            //}
            //else Console.WriteLine(lR0Grammar);

            SLRGrammar.PrintStateItems = true;
            SLRGrammar.PrintTable = true;
            if (!SLRGrammar.TryCreate(grammar, out var sLRGrammar, out createMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"Error:\n{createMsg}");
            }
            else Console.WriteLine(sLRGrammar);
        }

        static void LR1GrammarExample()
        {
            var listProduction = new List<Production>();
            listProduction.AddRange(Production.Create("S", "A a A b|B b B a"));
            listProduction.AddRange(Production.Create("A", ""));
            listProduction.AddRange(Production.Create("B", ""));
            Grammar grammar = new Grammar(listProduction, new("S"));

            Console.WriteLine(grammar);
            string? createMsg = null;

            SLRGrammar.PrintStateItems = false;
            SLRGrammar.PrintTable = true;
            if (!SLRGrammar.TryCreate(grammar, out var sLRGrammar, out createMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"Error:\n{createMsg}");
            }
            else Console.WriteLine(sLRGrammar);

            LR1Grammar.PrintStateItems = false;
            LR1Grammar.PrintTable = true;
            if (!LR1Grammar.TryCreate(grammar, out var lR1Grammar, out createMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"Error:\n{createMsg}");
            }
            else Console.WriteLine(lR1Grammar);
        }

        static void LALRGrammarExample()
        {
            var listProduction = new List<Production>();

            listProduction.AddRange(Production.Create("S", "L = R|R"));
            listProduction.AddRange(Production.Create("L", "* R|i"));
            listProduction.AddRange(Production.Create("R", "L"));
            Grammar grammar = new Grammar(listProduction, new("S"));

            Console.WriteLine(grammar);
            string? createMsg = null;

            Grammar.PrintStateItems = false;
            Grammar.PrintTable = true;

            if (!SLRGrammar.TryCreate(grammar, out var sLRGrammar, out createMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"Error:\n{createMsg}");
            }
            else Console.WriteLine(sLRGrammar);

            if (!LR1Grammar.TryCreate(grammar, out var lR1Grammar, out createMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"Error:\n{createMsg}"); 
            }
            else Console.WriteLine(lR1Grammar);

            if (!LALRGrammar.TryCreate(grammar, out var lALRGrammar, out createMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"Error:\n{createMsg}");
            }
            else Console.WriteLine(lALRGrammar);            
        }

        static void Main(string[] args)
        {
            LALRGrammarExample();
        }
    }
}
