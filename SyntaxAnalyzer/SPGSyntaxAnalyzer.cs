using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxAnalyzer
{
    /// <summary>
    /// 简单文本优先分析器
    /// </summary>
    public class SPGSyntaxAnalyzer
    {
        public delegate void AdvanceProcedure(out Terminal Sym);

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
        }

        private char GetPredictiveChar(Symbol left, Symbol right)
        {
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
            while (GetPredictiveChar(stackAnalysis.Peek(), sym) != '>')
            {
                stackAnalysis.Push(sym);
                Advance();
            }
            int j = 0;
            while (GetPredictiveChar(stackAnalysis.ElementAt(j+1), stackAnalysis.ElementAt(j)) != '<')
            {
                j += 1;
            }
        }

        public void Analyzer()
        {
            ProcedureInit();
            Advance();
            Procedure();
        }
    }
}
