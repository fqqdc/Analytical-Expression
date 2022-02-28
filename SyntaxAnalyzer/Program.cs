using System;
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
            listProduction.AddRange(Production.Create("A", "B d|B e"));
            listProduction.AddRange(Production.Create("B", "c|B c"));


            Grammar grammar = new Grammar(listProduction, new("A"));
            Console.WriteLine(grammar);

            var arr = SPGrammar.GetSymbolOrderArray(grammar);

            var equalMatrix = SPGrammar.GetEqualMatrix(grammar, arr);
            Console.WriteLine("=矩阵");
            Console.WriteLine(SPGrammar.FormatMatrix(equalMatrix, arr));

            var fisrtMatrix = SPGrammar.GetFirstMatrix(grammar, arr);
            //Console.WriteLine("fisrt矩阵");
            //Console.WriteLine(SPGrammar.FormatMatrix(fisrtMatrix, arr));

            var fisrtPlusMatrix = SPGrammar.ClosureMatrix(fisrtMatrix);
            //Console.WriteLine("fisrt+矩阵");
            //Console.WriteLine(SPGrammar.FormatMatrix(fisrtPlusMatrix, arr));

            var lessMatrix = SPGrammar.ProductMatrix(equalMatrix, fisrtPlusMatrix);
            Console.WriteLine("<矩阵");
            Console.WriteLine(SPGrammar.FormatMatrix(lessMatrix, arr));

            var lastMatrix = SPGrammar.GetLastMatrix(grammar, arr);
            //Console.WriteLine("last矩阵");
            //Console.WriteLine(SPGrammar.FormatMatrix(lastMatrix, arr));

            var lastPlusMatrix = SPGrammar.ClosureMatrix(lastMatrix);
            //Console.WriteLine("last+矩阵");
            //Console.WriteLine(SPGrammar.FormatMatrix(lastPlusMatrix, arr));

            var trpLastPlusMatrix = SPGrammar.TransposeMatrix(lastPlusMatrix);
            //Console.WriteLine("TRP(last+)矩阵");
            //Console.WriteLine(SPGrammar.FormatMatrix(trpLastPlusMatrix, arr));

            var fisrtStarMatrix = SPGrammar.UnionMatrix(fisrtPlusMatrix, SPGrammar.IdentityMatrix(fisrtPlusMatrix.GetLength(0)));
            //Console.WriteLine("fisrt*矩阵");
            //Console.WriteLine(SPGrammar.FormatMatrix(fisrtStarMatrix, arr));

            var greaterMatrix = SPGrammar.ProductMatrix(trpLastPlusMatrix, equalMatrix);
            greaterMatrix = SPGrammar.ProductMatrix(greaterMatrix, fisrtStarMatrix);
            Console.WriteLine(">矩阵");
            Console.WriteLine(SPGrammar.FormatMatrix(greaterMatrix, arr));

            //var matrix1 = SPGrammar.IntersectMatrix(equalMatrix, lessMatrix);
            //Console.WriteLine($"M(=) ∩ M(<) : {matrix1.Cast<bool>().All(b => !b)}");
            //Console.WriteLine(SPGrammar.FormatMatrix(matrix1, arr));
            //var matrix2 = SPGrammar.IntersectMatrix(equalMatrix, greaterMatrix);
            //Console.WriteLine($"M(=) ∩ M(>) : {matrix2.Cast<bool>().All(b => !b)}");
            //Console.WriteLine(SPGrammar.FormatMatrix(matrix2, arr));
            //var matrix3 = SPGrammar.IntersectMatrix(lessMatrix, greaterMatrix);
            //Console.WriteLine($"M(<) ∩ M(>) : {matrix3.Cast<bool>().All(b => !b)}");
            //Console.WriteLine(SPGrammar.FormatMatrix(matrix3, arr));
            //Console.WriteLine(matrix1.Cast<bool>().Union(matrix2.Cast<bool>()).Union(matrix3.Cast<bool>()).All(b => !b));

            if (!SPGrammar.TryCreateSPGrammar(grammar, out var spGrammar, out var msg))
            {
                Console.WriteLine(msg);
                return;
            }

            Console.WriteLine(spGrammar);

            string input = "ccd";
            int index = 0;

            SPGSyntaxAnalyzer.AdvanceProcedure p = (out Terminal sym) =>
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

            SPGSyntaxAnalyzer analyzer = new(spGrammar, p);

            analyzer.Analyzer();
            Console.WriteLine("OK");
        }

        static void Main1(string[] args)
        {
            Stack<int> stack = new Stack<int>();
            for (int i = 0; i < 10; i++)
            {
                stack.Push(i);
            }

            foreach (var item in stack.TakeWhile((i, index) => index < 3))
            {
                Console.WriteLine(item);
            }
        }
    }
}
