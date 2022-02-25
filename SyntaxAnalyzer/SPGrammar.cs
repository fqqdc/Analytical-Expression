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
            ) : base(allProduction, startNonTerminal) { }


        public static bool TryCreateSPGrammar(Grammar grammar, out SPGrammar? sPGrammar, out string errorMsg)
        {
            var (equalMatrix, lessMatrix, greaterMatrix, _) = SPGrammar.GetMatrices(grammar);

            StringBuilder sbErrorMsg = new();
            var intersectMatrix1 = SPGrammar.IntersectMatrix(equalMatrix, lessMatrix);
            if (intersectMatrix1.Cast<bool>().Any(b => b == true))
            {
                sbErrorMsg.AppendLine("=集与<集有交集。");
                sPGrammar = null;
                errorMsg = sbErrorMsg.ToString();
                return false;
            }

            var intersectMatrix2 = SPGrammar.IntersectMatrix(equalMatrix, greaterMatrix);
            if (intersectMatrix2.Cast<bool>().Any(b => b == true))
            {
                sbErrorMsg.AppendLine("=集与>集有交集。");
                sPGrammar = null;
                errorMsg = sbErrorMsg.ToString();
                return false;
            }

            var intersectMatrix3 = SPGrammar.IntersectMatrix(lessMatrix, greaterMatrix);
            if (intersectMatrix3.Cast<bool>().Any(b => b == true))
            {
                sbErrorMsg.AppendLine("<集与>集有交集。");
                sPGrammar = null;
                errorMsg = sbErrorMsg.ToString();
                return false;
            }

            sPGrammar = new(grammar.P, grammar.S);
            errorMsg = sbErrorMsg.ToString();
            return true;
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

        public static (bool[,] equalMatrix, bool[,] lessMatrix, bool[,] greaterMatrix, Dictionary<Symbol, int> symbolIndex) GetMatrices(Grammar grammar)
        {
            var arr = SPGrammar.GetSymbolOrderArray(grammar);
            var dict = arr.ToDictionary(i => i, i => Array.IndexOf(arr, i));

            var equalMatrix = SPGrammar.GetEqualMatrix(grammar, arr);
            var fisrtMatrix = SPGrammar.GetFirstMatrix(grammar, arr);
            var fisrtPlusMatrix = SPGrammar.ClosureMatrix(fisrtMatrix);
            var lessMatrix = SPGrammar.ProductMatrix(equalMatrix, fisrtPlusMatrix);
            var lastMatrix = SPGrammar.GetLastMatrix(grammar, arr);
            var lastPlusMatrix = SPGrammar.ClosureMatrix(lastMatrix);
            var trpLastPlusMatrix = SPGrammar.TransposeMatrix(lastPlusMatrix);
            var fisrtStarMatrix = SPGrammar.UnionMatrix(fisrtPlusMatrix, SPGrammar.IdentityMatrix(fisrtPlusMatrix.GetLength(0)));
            var greaterMatrix = SPGrammar.ProductMatrix(trpLastPlusMatrix, equalMatrix);
            greaterMatrix = SPGrammar.ProductMatrix(greaterMatrix, fisrtStarMatrix);
            return (equalMatrix, lessMatrix, greaterMatrix, dict);
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
        /// Last矩阵
        /// </summary>
        public static bool[,] GetLastMatrix(Grammar grammar, Symbol[] symbols)
        {
            bool[,] matrix = new bool[symbols.Length, symbols.Length];
            var symbolId = symbols.ToDictionary(s => s, s => Array.IndexOf(symbols, s));

            foreach (var p in grammar.P)
            {
                var revRight = p.Right.Reverse().ToArray();
                if (revRight.Length == 0 || revRight[0] == Terminal.Epsilon)
                    throw new Exception($"无效的产生式:{p}");
                var fstRight = revRight[0];
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
        public static bool[,] ProductMatrix(bool[,] left, bool[,] right)
        {
            if (left.GetLength(0) != left.GetLength(1))
                throw new Exception("left不是方阵。");
            if (right.GetLength(0) != right.GetLength(1))
                throw new Exception("right不是方阵。");
            if (left.GetLength(0) != right.GetLength(0))
                throw new Exception("left矩阵与right矩阵阶数不相等。");

            var n = left.GetLength(0);
            bool[,] matrix = new bool[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    bool v_ij = false;
                    for (int k = 0; k < n; k++)
                    {
                        v_ij = left[i, k] && right[k, j];
                        if (v_ij) break;
                    }
                }
            }

            return matrix;
        }

        /// <summary>
        /// 交集
        /// </summary>
        public static bool[,] IntersectMatrix(bool[,] left, bool[,] right)
        {
            if (left.GetLength(0) != left.GetLength(1))
                throw new Exception("left不是方阵。");
            if (right.GetLength(0) != right.GetLength(1))
                throw new Exception("right不是方阵。");
            if (left.GetLength(0) != right.GetLength(0))
                throw new Exception("left矩阵与right矩阵阶数不相等。");

            var n = left.GetLength(0);
            bool[,] matrix = new bool[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matrix[i, j] = left[i, j] && right[i, j];
                }
            }

            return matrix;
        }

        /// <summary>
        /// 并集
        /// </summary>
        public static bool[,] UnionMatrix(bool[,] left, bool[,] right)
        {
            if (left.GetLength(0) != left.GetLength(1))
                throw new Exception("left不是方阵。");
            if (right.GetLength(0) != right.GetLength(1))
                throw new Exception("right不是方阵。");
            if (left.GetLength(0) != right.GetLength(0))
                throw new Exception("left矩阵与right矩阵阶数不相等。");

            var n = left.GetLength(0);
            bool[,] matrix = new bool[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matrix[i, j] = left[i, j] || right[i, j];
                }
            }

            return matrix;
        }

        /// <summary>
        /// 单位矩阵
        /// </summary>
        public static bool[,] IdentityMatrix(int length)
        {
            bool[,] matrix = new bool[length, length];

            for (int i = 0; i < length; i++)
            {
                matrix[i, i] = true;
            }

            return matrix;
        }

        /// <summary>
        /// 转置矩阵
        /// </summary>
        public static bool[,] TransposeMatrix(bool[,] oriMatrix)
        {
            bool[,] transposeMatrix = new bool[oriMatrix.GetLength(1), oriMatrix.GetLength(0)];

            for (int i = 0; i < oriMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < oriMatrix.GetLength(1); j++)
                {
                    transposeMatrix[j, i] = oriMatrix[i, j];
                }
            }

            return transposeMatrix;
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
