using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// 简单优先文法
    /// </summary>
    public class SPGrammar : Grammar
    {
        private SPGrammar(IEnumerable<Production> allProduction, NonTerminal startNonTerminal
            , Dictionary<NonTerminal, HashSet<Terminal>> mapFirst
            , Dictionary<NonTerminal, HashSet<Terminal>> mapFollow
            ) : base(allProduction, startNonTerminal)
        {
            throw new NotImplementedException();
        }


        public static bool TryCreateSPGrammar(Grammar grammar, out SPGrammar sPGrammar, out string errorMsg)
        {
            throw new NotImplementedException();
        }


        #region 公共方法

        public static Symbol[] GetSymbolOrderArray(Grammar grammar)
        {
            return grammar.Vn.Cast<Symbol>().Union(grammar.Vt.Cast<Symbol>()).Distinct()
                .OrderBy(s => s != grammar.S)
                .ThenBy(s => s is not NonTerminal)
                .ThenBy(s => s.Name)                
                .ToArray();
        }

        /// <summary>
        /// First矩阵
        /// </summary>
        public static int[,] GetFirstMatrix(Grammar grammar, Symbol[] symbols)
        {
            int[,] matrixFirst = new int[symbols.Length, symbols.Length];
            var symbolId = symbols.ToDictionary(s => s, s => Array.IndexOf(symbols, s));


            foreach (var p in grammar.P)
            {
                if (!p.Right.Any() || p.Right.First() == Terminal.Epsilon)
                    throw new Exception($"无效的产生式:{p}");
                var fstRight = p.Right.First();
                matrixFirst[symbolId[p.Left], symbolId[fstRight]] = 1;
            }

            return matrixFirst;
        }

        /// <summary>
        /// First+矩阵
        /// </summary>
        public static int[,] GetClosureMatrix(int[,] oriMatrix)
        {
            int n = oriMatrix.GetLength(0);
            if (n != oriMatrix.GetLength(1))
            {
                throw new Exception("不支持非方阵矩阵");
            }

            var matrix = (int[,])oriMatrix.Clone();
            //Warshall算法
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if(matrix[j,i] == 1)
                    {
                        for (int k = 0; k < n; k++)
                        {
                            if (matrix[j, k] == 1 || matrix[i, k] == 1)
                                matrix[j, k] = 1;
                        }
                    }
                }
            }
            return matrix;
        }

        /// <summary>
        /// First*矩阵
        /// </summary>
        public static int[,] GetSelfClosureMatrix(int[,] oriMatrix)
        {
            var matrix = GetClosureMatrix(oriMatrix);
            var n = matrix.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                matrix[i, i] = 1;
            }

            return matrix;
        }

        public static string FormatMatrix(int[,] matrix, Symbol[] symbols)
        {
            StringBuilder sbMatrix = new StringBuilder();
            for (int i = -1; i < matrix.GetLength(0); i++)
            {

                if (i == -1)
                {
                    // 行首
                    sbMatrix.Append("\t");
                }
                else
                {
                    sbMatrix.Append($"{symbols[i]}\t");
                }


                for (int j = 0; j < matrix.GetLength(1); j++)
                {

                    if (i == -1)
                    {
                        // 行首
                        sbMatrix.Append($"{symbols[j]}\t");
                    }
                    else
                    {
                        sbMatrix.Append($"{matrix[i, j]}\t");
                    }
                }
                sbMatrix.AppendLine();
            }
            return sbMatrix.ToString();
        }

        #endregion
    }
}
