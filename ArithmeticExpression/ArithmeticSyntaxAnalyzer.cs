using LexicalAnalyzer;
using SyntaxAnalyzer;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ArithmeticExpression
{
    public class ArithmeticSyntaxAnalyzer : LRSyntaxAnalyzer
    {
        public ArithmeticSyntaxAnalyzer(Dictionary<(int state, Terminal t), List<ActionItem>> actionTable, Dictionary<(int state, NonTerminal t), int> gotoTable)
            : base(actionTable, gotoTable) { }

        private Stack<Expression> expStack = new();

        protected override void OnShiftItem(Terminal terminal, string terminalToken)
        {
            base.OnShiftItem(terminal, terminalToken);

            switch (terminal.Name)
            {
                case "integer":
                    {
                        expStack.Push(Expression.Constant(int.Parse(terminalToken), typeof(int)));
                    }
                    break;
                case "decimal":
                    {
                        expStack.Push(Expression.Constant(double.Parse(terminalToken), typeof(double)));
                    }
                    break;
                case "id":
                default:
                    {
                        expStack.Push(Expression.Constant(terminalToken, typeof(string)));
                    }
                    break;
            }
        }

        protected override void OnReduceItem(Production production)
        {
            base.OnReduceItem(production);

            var leftName = production.Left.Name;

            switch (leftName)
            {
                case "ExpAtom":
                    Action_ExpAtom(production);
                    break;
                case "ExpValue":
                    Action_ExpValue(production);
                    break;

                default:
                    break;
            }
        }

        private void Action_ExpValue(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 3:
                    {
                        var fstRight = production.Right.First().Name;
                        switch (fstRight)
                        {
                            case "id":
                                {
                                    var exp2 = (ConstantExpression)expStack.Pop();
                                    expStack.Pop();
                                    var exp1 = (ConstantExpression)expStack.Pop();

                                    if(exp1.Value == null || exp2.Value == null)
                                        throw new NotImplementedException();

                                    var exp = Expression.Parameter(typeof(ExpandoObject), (string)exp1.Value);
                                    var methodName = (string)exp2.Value;
                                    var getMethod = typeof(ExpandoObject).GetMethod("TryGetMember");
                                    throw new NotImplementedException();
                                }
                                break;
                            case "ExpValue":
                            default: throw new NotSupportedException();
                        }


                        throw new NotImplementedException();
                    }
                    break;
                case 4:
                    {
                        throw new NotImplementedException();
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }

        private void Action_ExpAtom(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: break;
                case 3:
                    {
                        expStack.Pop();
                        var exp = expStack.Pop();
                        expStack.Pop();
                        expStack.Push(exp);
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }

        protected override void OnAcceptItem()
        {
            base.OnAcceptItem();
        }
    }
}
