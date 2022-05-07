using LexicalAnalyzer;
using SyntaxAnalyzer;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ArithmeticExpression
{
    public class ArithmeticSyntaxAnalyzer : LRSyntaxAnalyzer
    {
        private static MethodInfo? GetMemberMethod = typeof(ArithmeticHelper).GetMethod("GetMember", new[] { typeof(object), typeof(string) });
        private static MethodInfo? ParseToNumberMethod = typeof(ArithmeticHelper).GetMethod("ParseToNumber", new[] { typeof(object) });

        public ArithmeticSyntaxAnalyzer(Dictionary<(int state, Terminal t), List<ActionItem>> actionTable, Dictionary<(int state, NonTerminal t), int> gotoTable)
            : base(actionTable, gotoTable) { }

        private Stack<Expression> expStack = new();

        public ParameterExpression? Parameter { get; private set; }
        public Expression? Target { get; private set; }

        protected override void OnProcedureInit()
        {
            base.OnProcedureInit();

            this.Parameter = Expression.Parameter(typeof(IDictionary<string, object?>), "Parameter");
            this.Target = null;
        }

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
                        expStack.Push(Expression.Constant(decimal.Parse(terminalToken), typeof(decimal)));
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
                case "ExpObject":
                    Action_ExpObject(production);
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
            return;

            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1:
                    {
                        var fstRight = production.Right.First().Name;
                        switch (fstRight)
                        {
                            default: throw new NotSupportedException();
                        }
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }



        private void Action_ExpObject(Production production)
        {
            var rightLength = production.Right.Count();
            var fstRight = production.Right.First().Name;
            switch (rightLength)
            {
                case 3:
                    {
                        switch (fstRight)
                        {
                            case "id": // id_1 . id_2
                                {
                                    if (GetMemberMethod == null || Parameter == null)
                                        throw new NullReferenceException();

                                    var expId_2 = (ConstantExpression)expStack.Pop();
                                    expStack.Pop();
                                    var expId_1 = (ConstantExpression)expStack.Pop();

                                    if (expId_1.Value == null || expId_2.Value == null)
                                        throw new NotImplementedException();

                                    var key1 = (string)expId_1.Value;
                                    var expValue1 = Expression.Call(GetMemberMethod, Parameter, Expression.Constant(key1));

                                    var key2 = (string)expId_2.Value;
                                    var expValue2 = Expression.Call(GetMemberMethod, expValue1, Expression.Constant(key2));


                                    expStack.Push(expValue2);
                                }
                                break;
                            case "ExpObject": // ExpObject . id
                                {
                                    if (GetMemberMethod == null || Parameter == null)
                                        throw new NullReferenceException();

                                    var expId = (ConstantExpression)expStack.Pop();
                                    expStack.Pop();
                                    var expExpObject = expStack.Pop();

                                    if (expId.Value == null)
                                        throw new NotImplementedException();

                                    var key2 = (string)expId.Value;
                                    var expValue2 = Expression.Call(GetMemberMethod, expExpObject, Expression.Constant(key2));

                                    expStack.Push(expValue2);
                                }
                                break;
                            default: throw new NotSupportedException();
                        }
                    }
                    break;
                case 4:
                    {
                        switch (fstRight)
                        {
                            case "id": // id [ Exp ]
                                { 
                                    expStack.Pop();
                                    var exp = expStack.Pop();
                                    expStack.Pop();
                                    var expId = expStack.Pop();
                                }
                                break;
                            default: throw new NotSupportedException();
                        }
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
                case 1:
                    {
                        var fstRight = production.Right.First().Name;
                        switch (fstRight)
                        {
                            case "id":
                                {
                                    if (ParseToNumberMethod == null || GetMemberMethod == null || Parameter == null)
                                        throw new NullReferenceException();

                                    var exp = (ConstantExpression)expStack.Pop();
                                    if (exp.Value == null) throw new NullReferenceException();

                                    var key = (string)exp.Value;
                                    var expValue = Expression.Call(GetMemberMethod, Parameter, Expression.Constant(key));
                                    var expNumber = Expression.Call(ParseToNumberMethod, expValue);

                                    expStack.Push(expNumber);
                                }
                                break;
                            default: break;
                        }
                    }
                    break;
                case 3: // ( Exp )
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

            Target = expStack.Pop();
        }
    }
}
