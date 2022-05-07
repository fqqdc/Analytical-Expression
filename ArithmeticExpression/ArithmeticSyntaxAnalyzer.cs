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
        private static MethodInfo? GetIndexMethod = typeof(ArithmeticHelper).GetMethod("GetIndex", new[] { typeof(object), typeof(object) });
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
                case "number":
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
                case "ExpObject":
                    Action_ExpObject(production);
                    break;
                case "ExpValue":
                    Action_ExpValue(production);
                    break;
                case "ExpSquare":
                    Action_ExpSquare(production);
                    break;
                case "ExpSign":
                    Action_ExpSign(production);
                    break;
                case "ExpMulti":
                    Action_ExpMulti(production);
                    break;
                case "ExpAdd":
                    Action_ExpAdd(production);
                    break;
                case "ExpCompare":
                    Action_ExpCompare(production);
                    break;
                case "ExpLogicEqual":
                    Action_ExpLogicEqual(production);
                    break;
                case "ExpLogicOr":
                    Action_ExpLogicOr(production);
                    break;
                case "ExpLogicAnd":
                    Action_ExpLogicAnd(production);
                    break;
                default:
                    break;
            }
        }

        private void Action_ExpLogicAnd(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: // ExpLogicOr
                    break;
                case 3: // ExpLogicAnd && ExpLogicOr
                    {
                        var ExpLogicOr = expStack.Pop();
                        var expOp = (ConstantExpression)expStack.Pop();
                        var ExpLogicAnd = expStack.Pop();

                        if (expOp.Value == null)
                            throw new NullReferenceException();

                        Expression castExpLogicAndToDouble;
                        if (ExpLogicAnd.Type != typeof(double))
                            castExpLogicAndToDouble = Expression.Convert(ExpLogicAnd, typeof(double));
                        else castExpLogicAndToDouble = ExpLogicAnd;

                        Expression castExpLogicOrToDouble;
                        if (ExpLogicOr.Type != typeof(double))
                            castExpLogicOrToDouble = Expression.Convert(ExpLogicOr, typeof(double));
                        else castExpLogicOrToDouble = ExpLogicOr;

                        var compare = Expression.And(
                            Expression.GreaterThan(castExpLogicAndToDouble, Expression.Constant(0d)),
                           Expression.GreaterThan(castExpLogicOrToDouble, Expression.Constant(0d)));

                        var exp = Expression.Condition(compare, Expression.Constant(1d), Expression.Constant(0d));
                        expStack.Push(exp);
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }

        private void Action_ExpLogicOr(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: // ExpLogicEqual
                    break;
                case 3: // ExpLogicOr or ExpLogicEqual
                    {
                        var ExpLogicEqual = expStack.Pop();
                        var expOp = (ConstantExpression)expStack.Pop();
                        var ExpLogicOr = expStack.Pop();

                        if (expOp.Value == null)
                            throw new NullReferenceException();

                        Expression castExpLogicOrToDouble;
                        if (ExpLogicOr.Type != typeof(double))
                            castExpLogicOrToDouble = Expression.Convert(ExpLogicOr, typeof(double));
                        else castExpLogicOrToDouble = ExpLogicOr;

                        Expression castExpLogicEqualToDouble;
                        if (ExpLogicEqual.Type != typeof(double))
                            castExpLogicEqualToDouble = Expression.Convert(ExpLogicEqual, typeof(double));
                        else castExpLogicEqualToDouble = ExpLogicEqual;

                        var compare = Expression.Or(
                            Expression.GreaterThan(castExpLogicOrToDouble, Expression.Constant(0d)),
                           Expression.GreaterThan(castExpLogicEqualToDouble, Expression.Constant(0d)));

                        var exp = Expression.Condition(compare, Expression.Constant(1d), Expression.Constant(0d));
                        expStack.Push(exp);
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }

        private void Action_ExpLogicEqual(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: // ExpCompare
                    break;
                case 3: // ExpCompare == ExpCompare|ExpCompare != ExpCompare
                    {
                        var ExpCompare_2 = expStack.Pop();
                        var expOp = (ConstantExpression)expStack.Pop();
                        var ExpCompare_1 = expStack.Pop();

                        if (expOp.Value == null)
                            throw new NullReferenceException();

                        Expression castExpCompare2ToDouble;
                        if (ExpCompare_2.Type != typeof(double))
                            castExpCompare2ToDouble = Expression.Convert(ExpCompare_2, typeof(double));
                        else castExpCompare2ToDouble = ExpCompare_2;

                        Expression castExpCompare1ToDouble;
                        if (ExpCompare_1.Type != typeof(double))
                            castExpCompare1ToDouble = Expression.Convert(ExpCompare_1, typeof(double));
                        else castExpCompare1ToDouble = ExpCompare_1;


                        Expression compare;
                        switch (expOp.Value)
                        {
                            case "==":
                                {
                                    compare = Expression.Equal(castExpCompare1ToDouble, castExpCompare2ToDouble);
                                }
                                break;
                            case "!=":
                                {
                                    compare = Expression.NotEqual(castExpCompare1ToDouble, castExpCompare2ToDouble);
                                }
                                break;
                            default: throw new NotSupportedException();
                        }

                        var exp = Expression.Condition(compare, Expression.Constant(1d), Expression.Constant(0d));
                        expStack.Push(exp);
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }

        private void Action_ExpCompare(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: // ExpAdd
                    break;
                case 3: // ExpAdd > ExpAdd|ExpAdd >= ExpAdd|ExpAdd < ExpAdd|ExpAdd <= ExpAdd
                    {
                        var ExpAdd_2 = expStack.Pop();
                        var expOp = (ConstantExpression)expStack.Pop();
                        var ExpAdd_1 = expStack.Pop();

                        if (expOp.Value == null)
                            throw new NullReferenceException();

                        Expression castExpAdd2ToDouble;
                        if (ExpAdd_2.Type != typeof(double))
                            castExpAdd2ToDouble = Expression.Convert(ExpAdd_2, typeof(double));
                        else castExpAdd2ToDouble = ExpAdd_2;

                        Expression castExpAdd1ToDouble;
                        if (ExpAdd_1.Type != typeof(double))
                            castExpAdd1ToDouble = Expression.Convert(ExpAdd_1, typeof(double));
                        else castExpAdd1ToDouble = ExpAdd_1;


                        Expression compare;
                        switch (expOp.Value)
                        {
                            case ">":
                                {
                                    compare = Expression.GreaterThan(castExpAdd1ToDouble, castExpAdd2ToDouble);
                                }
                                break;
                            case ">=":
                                {
                                    compare = Expression.GreaterThanOrEqual(castExpAdd1ToDouble, castExpAdd2ToDouble);
                                }
                                break;
                            case "<":
                                {
                                    compare = Expression.LessThan(castExpAdd1ToDouble, castExpAdd2ToDouble);
                                }
                                break;
                            case "<=":
                                {
                                    compare = Expression.LessThanOrEqual(castExpAdd1ToDouble, castExpAdd2ToDouble);
                                }
                                break;
                            default: throw new NotSupportedException();
                        }

                        var exp = Expression.Condition(compare, Expression.Constant(1d), Expression.Constant(0d));
                        expStack.Push(exp);
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }

        private void Action_ExpAdd(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: // ExpMulti
                    break;
                case 3: // ExpAdd + ExpMulti|ExpAdd - ExpMulti"
                    {
                        var ExpMulti = expStack.Pop();
                        var expOp = (ConstantExpression)expStack.Pop();
                        var ExpAdd = expStack.Pop();

                        if (expOp.Value == null)
                            throw new NullReferenceException();

                        Expression castExpSignToDouble;
                        if (ExpMulti.Type != typeof(double))
                            castExpSignToDouble = Expression.Convert(ExpMulti, typeof(double));
                        else castExpSignToDouble = ExpMulti;

                        Expression castExpMultiToDouble;
                        if (ExpAdd.Type != typeof(double))
                            castExpMultiToDouble = Expression.Convert(ExpAdd, typeof(double));
                        else castExpMultiToDouble = ExpAdd;


                        Expression exp;
                        switch (expOp.Value)
                        {
                            case "+":
                                {
                                    exp = Expression.Add(castExpMultiToDouble, castExpSignToDouble);
                                }
                                break;
                            case "-":
                                {
                                    exp = Expression.Subtract(castExpMultiToDouble, castExpSignToDouble);
                                }
                                break;
                            default: throw new NotSupportedException();
                        }
                        expStack.Push(exp);
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }

        private void Action_ExpMulti(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: // ExpSquare
                    break;
                case 3: // ExpMulti * ExpSign|ExpMulti / ExpSign|ExpMulti % ExpSign
                    {
                        var expSign = expStack.Pop();
                        var expOp = (ConstantExpression)expStack.Pop();
                        var expMulti = expStack.Pop();

                        if (expOp.Value == null)
                            throw new NullReferenceException();

                        Expression castExpSignToDouble;
                        if (expSign.Type != typeof(double))
                            castExpSignToDouble = Expression.Convert(expSign, typeof(double));
                        else castExpSignToDouble = expSign;

                        Expression castExpMultiToDouble;
                        if (expMulti.Type != typeof(double))
                            castExpMultiToDouble = Expression.Convert(expMulti, typeof(double));
                        else castExpMultiToDouble = expMulti;


                        Expression exp;
                        switch (expOp.Value)
                        {
                            case "*":
                                {
                                    exp = Expression.Multiply(castExpMultiToDouble, castExpSignToDouble);
                                }
                                break;
                            case "/":
                                {
                                    exp = Expression.Divide(castExpMultiToDouble, castExpSignToDouble);
                                }
                                break;
                            case "%":
                                {
                                    exp = Expression.Modulo(castExpMultiToDouble, castExpSignToDouble);
                                }
                                break;
                            default: throw new NotSupportedException();
                        }
                        expStack.Push(exp);
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }

        private void Action_ExpSign(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: // ExpSquare
                    break;
                case 2: // - ExpSign
                    {
                        var expSign = expStack.Pop();
                        expStack.Pop();

                        Expression exp;
                        if (expSign.Type == typeof(object))
                            exp = Expression.Negate(Expression.Convert(expSign, typeof(double)));
                        else exp = Expression.Negate(expSign);

                        expStack.Push(exp);
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }

        private void Action_ExpSquare(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: // ExpValue
                    break;
                case 3: // ExpValue ^ ExpSquare
                    {
                        var expSquare = expStack.Pop();
                        expStack.Pop();
                        var expValue = expStack.Pop();

                        Expression castVar1ToDouble;
                        if (expValue.Type != typeof(double))
                            castVar1ToDouble = Expression.Convert(expValue, typeof(double));
                        else castVar1ToDouble = expValue;

                        Expression castVar2ToDouble;
                        if (expSquare.Type != typeof(double))
                            castVar2ToDouble = Expression.Convert(expSquare, typeof(double));
                        else castVar2ToDouble = expSquare;

                        var exp = Expression.Power(castVar1ToDouble, castVar2ToDouble);
                        expStack.Push(exp);
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }

        private void Action_ExpValue(Production production)
        {
            var rightLength = production.Right.Count();
            var fstRight = production.Right.First().Name;
            switch (rightLength)
            {
                case 1:
                    {
                        switch (fstRight)
                        {
                            case "ExpAtom":
                            case "ExpObject":
                                {
                                    if (ParseToNumberMethod == null) throw new NullReferenceException();

                                    var expObject = expStack.Pop();
                                    Expression exp;
                                    if (expObject.Type == typeof(double))
                                        exp = expObject;
                                    else exp = Expression.Call(ParseToNumberMethod, expObject);

                                    expStack.Push(exp);
                                }
                                break;
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
                            case "id": // id [ ExpValue ]
                                {
                                    if (GetIndexMethod == null || Parameter == null)
                                        throw new NullReferenceException();

                                    expStack.Pop();
                                    var expValue = expStack.Pop();
                                    expStack.Pop();
                                    var exp_Id = (ConstantExpression)expStack.Pop();

                                    var idValue = Expression.Call(GetIndexMethod, Parameter, Expression.Constant(exp_Id.Value));
                                    var exp = Expression.Call(GetIndexMethod, idValue, expValue);

                                    expStack.Push(exp);
                                }
                                break;
                            case "ExpObject": // ExpObject [ ExpValue ]
                                {
                                    if (GetIndexMethod == null || Parameter == null)
                                        throw new NullReferenceException();

                                    expStack.Pop();
                                    var expValue = expStack.Pop();
                                    expStack.Pop();
                                    var expObject = expStack.Pop();

                                    var exp = Expression.Call(GetIndexMethod, expObject, Expression.Convert(expValue, typeof(object)));

                                    expStack.Push(exp);
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
            var fstRight = production.Right.First().Name;
            switch (rightLength)
            {
                case 1:
                    {
                        switch (fstRight)
                        {
                            case "number": break;
                            case "id": break;
                            default: throw new NotSupportedException();
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

            Target = Expression.Convert(expStack.Pop(), typeof(object));
        }
    }
}
