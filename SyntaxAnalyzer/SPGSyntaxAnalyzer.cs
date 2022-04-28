using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LexicalAnalyzer;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// 简单文本优先分析器
    /// </summary>
    public class SPGSyntaxAnalyzer : SyntaxAnalyzer
    {
        private SPGrammar grammar;
        private AdvanceProcedure advanceProcedure;
        private Terminal sym = Terminal.Epsilon;
        private char[,] predictiveMatrix = new char[0, 0];
        private Dictionary<Symbol, int> symbolIndex = new();
        private Stack<Symbol> stackAnalysis = new();
        public SPGSyntaxAnalyzer(SPGrammar grammar, AdvanceProcedure advanceProcedure)
        {
            this.grammar = grammar;
            this.advanceProcedure = advanceProcedure;

            InitSimplePredictiveMatrix();
        }

        /// <summary>
        /// 初始化优先矩阵
        /// </summary>
        private void InitSimplePredictiveMatrix()
        {
            var (equalMatrix, lessMatrix, greaterMatrix, symbolIndex) = SPGrammar.GetMatrices(this.grammar);
            int n = equalMatrix.GetLength(0);
            char[,] pMatrix = new char[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    int value = (equalMatrix[i, j] ? 1 : 0) + (lessMatrix[i, j] ? 2 : 0) + (greaterMatrix[i, j] ? 3 : 0);
                    switch (value)
                    {
                        case 1:
                            pMatrix[i, j] = '='; break;
                        case 2:
                            pMatrix[i, j] = '<'; break;
                        case 3:
                            pMatrix[i, j] = '>'; break;
                        default:
                            pMatrix[i, j] = '\0';
                            break;
                    }
                }
            }
            this.predictiveMatrix = pMatrix;
            this.symbolIndex = symbolIndex;

            // ===== Print

            StringBuilder sbMatrix = new StringBuilder();
            var symbols = symbolIndex.Keys.OrderBy(key => symbolIndex[key]).ToArray();
            for (int i = -1; i < pMatrix.GetLength(0); i++)
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


                for (int j = 0; j < pMatrix.GetLength(1); j++)
                {

                    if (i == -1)
                    {
                        // 行首
                        sbMatrix.Append($"{symbols[j]}\t");
                    }
                    else
                    {
                        sbMatrix.Append($"{pMatrix[i, j]}\t");
                    }
                }
                sbMatrix.AppendLine();
            }
            Console.WriteLine(sbMatrix.ToString());
        }

        private char GetPredictiveChar(Symbol left, Symbol right)
        {
            if (left == Terminal.EndTerminal)
                return '<';
            if (right == Terminal.EndTerminal)
                return '>';

            int indexLeft = symbolIndex[left];
            int indexRight = symbolIndex[right];
            return predictiveMatrix[indexLeft, indexRight];
        }


        private void Advance()
        {
            advanceProcedure(out this.sym);
            Console.WriteLine($"input:{this.sym}");
        }
        private void Error() { throw new Exception("语法分析错误"); }

        private void ProcedureInit()
        {
            stackAnalysis.Clear();
            stackAnalysis.Push(Terminal.EndTerminal);
        }

        private void Procedure()
        {
            do
            {
                while (GetPredictiveChar(stackAnalysis.Peek(), sym) != '>')
                {
                    stackAnalysis.Push(sym);
                    Advance();
                }

                int j = 0;
                while (GetPredictiveChar(stackAnalysis.ElementAt(j + 1), stackAnalysis.ElementAt(j)) != '<')
                {
                    j += 1;
                }

                bool hasFound = false;
                var n = j + 1;
                var stackPart = stackAnalysis.Take(n).Reverse().ToArray();

                foreach (var p in grammar.P)
                {
                    var pRight = p.Right.ToArray();                    
                    if (pRight.Length == n
                        && p.Right.SequenceEqual(stackPart))
                    {
                        for (int i = 0; i < n; i++)
                        {
                            stackAnalysis.Pop();
                        }
                        stackAnalysis.Push(p.Left);
                        Console.WriteLine(p);
                        hasFound = true;
                        break;
                    }
                }
                if (!hasFound)
                    Error();

                if (stackAnalysis.First() == grammar.S)
                {
                    if (sym == Terminal.EndTerminal)
                        break;
                    Error();
                }
                    
            }
            while (true);

        }

        public void Analyzer()
        {
            ProcedureInit();
            Advance();
            Procedure();
        }
    }
}
