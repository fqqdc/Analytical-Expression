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
        public static bool[,] GetFirstMatrix(Grammar grammar, Symbol[] symbols)
        {
            bool[,] matrix = new bool[symbols.Length, symbols.Length];
            var symbolId = symbols.ToDictionary(s => s, s => Array.IndexOf(symbols, s));


            foreach (var p in grammar.P)
            {
                if (!p.Right.Any() || p.Right.First() == Terminal.Epsilon)
                    throw new Exception($"无效的产生式:{p}");
                var fstRight = p.Right.First();
                matrix[symbolId[p.Left], symbolId[fstRight]] = true;
            }

            return matrix;
        }


        /// <summary>
        /// =矩阵
        /// </summary>
        public static bool[,] GetEqualMatrix(Grammar grammar, Symbol[] symbols)
        {
            bool[,] matrix = new bool[symbols.Length, symbols.Length];
            var symbolId = symbols.ToDictionary(s => s, s => Array.IndexOf(symbols, s));


            foreach (var p in grammar.P)
            {
                var arrRight = p.Right.ToArray();
                if (arrRight.Length < 2)
                    continue;

                for (int i = 0; i < arrRight.Length - 1; i++)
                {
                    var s1 = arrRight[i];
                    var s2 = arrRight[i + 1];
                    matrix[symbolId[s1], symbolId[s2]] = true;
                }
            }

            return matrix;
        }

        /// <summary>
        /// 布尔矩阵乘积
        /// </summary>
        public static bool[,] BooleanProduct(bool[,] left, bool[,] right)
        {
            if (left.GetLength(0) != left.GetLength(1))
                throw new Exception("left不是方阵。");
            if (right.GetLength(0) != right.GetLength(1))
                throw new Exception("right不是方阵。");
            if (left.GetLength(0) != right.GetLength(0))
                throw new Exception("left矩阵与right矩阵阶数不相等。");

            var n = left.GetLength(0);
            bool[] leftRow = new bool[n];
            bool[] rightCol = new bool[n];
            bool[,] matrix = new bool[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (left[i, j])
                        leftRow[i] = true;
                    if (right[i, j])
                        rightCol[j] = true;
                }
            }

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matrix[i, j] = leftRow[i] && rightCol[j];
                }
            }

            return matrix;
        }


        /// <summary>
        /// 矩阵+
        /// </summary>
        public static bool[,] ClosureMatrix(bool[,] oriMatrix)
        {
            int n = oriMatrix.GetLength(0);
            if (n != oriMatrix.GetLength(1))
            {
                throw new Exception("不支持非方阵矩阵");
            }

            var matrix = (bool[,])oriMatrix.Clone();
            //Warshall算法
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (matrix[j, i])
                    {
                        for (int k = 0; k < n; k++)
                        {
                            matrix[j, k] = matrix[j, k] || matrix[i, k];
                        }
                    }
                }
            }
            return matrix;
        }

        /// <summary>
        /// 矩阵*
        /// </summary>
        public static bool[,] SelfClosureMatrix(bool[,] oriMatrix)
        {
            var matrix = ClosureMatrix(oriMatrix);
            var n = matrix.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                matrix[i, i] = true;
            }

            return matrix;
        }

        public static string FormatMatrix(bool[,] matrix, Symbol[] symbols)
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
                        sbMatrix.Append($"{(matrix[i, j] ? 1 : 0)}\t");
                    }
                }
                sbMatrix.AppendLine();
            }
            return sbMatrix.ToString();
        }

        #endregion
    }
}
