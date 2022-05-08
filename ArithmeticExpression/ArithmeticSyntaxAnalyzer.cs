using LexicalAnalyzer;
using SyntaxAnalyzer;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
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
        private static MethodInfo? GetIndexMethod = typeof(ArithmeticHelper).GetMethod("GetIndex", new[] { typeof(object), typeof(double) });
        private static MethodInfo? ParseToNumberMethod = typeof(ArithmeticHelper).GetMethod("ParseToNumber", new[] { typeof(object) });

        public ArithmeticSyntaxAnalyzer(Dictionary<(int state, Terminal t), List<ActionItem>> actionTable, Dictionary<(int state, NonTerminal t), int> gotoTable)
            : base(actionTable, gotoTable) { }

        private Stack<Expression> expStack = new();

        public ParameterExpression? Parameter { get; private set; }
        public Expression? Target { get; private set; }

        protected override void OnProcedureInit()
        {
            base.OnProcedureInit();

            this.Parameter = Expression.Parameter(typeof(object), "Parameter");
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
                case "bool":
                    {
                        expStack.Push(Expression.Constant(bool.Parse(terminalToken), typeof(bool)));
                    }
                    break;
                case "null":
                    {
                        expStack.Push(Expression.Constant(null, typeof(object)));
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
                case "ExpObject":
                    Action_ExpObject(production);
                    break;
                case "ExpNumber":
                    Action_ExpNumber(production);
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
                case "ExpBool":
                    Action_ExpBool(production);
                    break;
                case "ExpEqual":
                    Action_ExpEqual(production);
                    break;
                case "ExpOr":
                    Action_ExpOr(production);
                    break;
                case "ExpAnd":
                    Action_ExpAnd(production);
                    break;
                default:
                    break;
            }
        }

        private void Action_ExpAnd(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: // ExpOr
                    break;
                case 3: // ExpAnd && ExpOr
                    {
                        var expOr = expStack.Pop();
                        expStack.Pop();
                        var expAnd = expStack.Pop();

                        var exp = Expression.And(expAnd, expOr);
                        expStack.Push(exp);
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }

        private void Action_ExpOr(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: // ExpEqual
                    break;
                case 3: // ExpOr or ExpEqual
                    {
                        var expEqual = expStack.Pop();
                        expStack.Pop();
                        var ExpOr = expStack.Pop();

                        var exp = Expression.Or(ExpOr, expEqual);
                        expStack.Push(exp);
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }

        private void Action_ExpEqual(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: // ExpBool
                    break;
                case 3: // ExpEqual == ExpBool|ExpEqual != ExpBool
                    {
                        var t1 = production.Right.ElementAt(1).Name;

                        var expBool = expStack.Pop();
                        expStack.Pop();
                        var expEqual = expStack.Pop();
                        switch (t1)
                        {
                            case "==":
                                {
                                    var exp = Expression.Equal(expEqual, expBool);
                                    expStack.Push(exp);
                                }
                                break;
                            case "!=":
                                {
                                    var exp = Expression.NotEqual(expEqual, expBool);
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

        private void Action_ExpBool(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: // bool
                    break;
                case 3: // ExpAdd > ExpAdd|ExpAdd >= ExpAdd|ExpAdd < ExpAdd|ExpAdd <= ExpAdd
                    {
                        var t0 = production.Right.ElementAt(0).Name;
                        switch (t0)
                        {
                            case "(": // ( ExpLogic )
                                {
                                    expStack.Pop();
                                    var expLogic = expStack.Pop();
                                    expStack.Pop();

                                    expStack.Push(expLogic);
                                }
                                break;
                            case "ExpObject": // ExpObject == ExpObject
                                {
                                    var expObject2 = expStack.Pop();
                                    expStack.Pop();
                                    var expObject1 = expStack.Pop();

                                    var exp = Expression.Equal(expObject1, expObject2);
                                    expStack.Push(exp);
                                }
                                break;
                            case "ExpArith":
                                {
                                    var t1 = production.Right.ElementAt(1).Name;

                                    // ExpArith > ExpArith|ExpArith >= ExpArith|ExpArith < ExpArith|ExpArith <= ExpArith"
                                    // ExpArith == ExpArith|ExpArith != ExpArith
                                    var expArith2 = expStack.Pop();
                                    expStack.Pop();
                                    var expArith1 = expStack.Pop();
                                    switch (t1)
                                    {

                                        case ">":
                                            {
                                                var exp = Expression.GreaterThan(expArith1, expArith2);
                                                expStack.Push(exp);
                                            }
                                            break;
                                        case ">=":
                                            {
                                                var exp = Expression.GreaterThanOrEqual(expArith1, expArith2);
                                                expStack.Push(exp);
                                            }
                                            break;
                                        case "<":
                                            {
                                                var exp = Expression.LessThan(expArith1, expArith2);
                                                expStack.Push(exp);
                                            }
                                            break;
                                        case "<=":
                                            {
                                                var exp = Expression.LessThanOrEqual(expArith1, expArith2);
                                                expStack.Push(exp);
                                            }
                                            break;

                                        case "==":
                                            {
                                                var exp = Expression.Equal(expArith1, expArith2);
                                                expStack.Push(exp);
                                            }
                                            break;
                                        case "!=":
                                            {
                                                var exp = Expression.NotEqual(expArith1, expArith2);
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
                case 3: // ExpAdd + ExpMulti|ExpAdd - ExpMulti
                    {
                        var ExpMulti = expStack.Pop();
                        var expOp = (ConstantExpression)expStack.Pop();
                        var ExpAdd = expStack.Pop();

                        if (expOp.Value == null)
                            throw new NullReferenceException();

                        Expression exp;
                        switch (expOp.Value)
                        {
                            case "+":
                                {
                                    exp = Expression.Add(ExpAdd, ExpMulti);
                                }
                                break;
                            case "-":
                                {
                                    exp = Expression.Subtract(ExpAdd, ExpMulti);
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


                        Expression exp;
                        switch (expOp.Value)
                        {
                            case "*":
                                {
                                    exp = Expression.Multiply(expMulti, expSign);
                                }
                                break;
                            case "/":
                                {
                                    exp = Expression.Divide(expMulti, expSign);
                                }
                                break;
                            case "%":
                                {
                                    exp = Expression.Modulo(expMulti, expSign);
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

                        var exp = Expression.Negate(expSign);

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
                case 1: // ExpNumber
                    break;
                case 3: // ExpNumber ^ ExpSquare
                    {
                        var expSquare = expStack.Pop();
                        expStack.Pop();
                        var expNumber = expStack.Pop();

                        var exp = Expression.Power(expNumber, expSquare);
                        expStack.Push(exp);
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }

        private void Action_ExpNumber(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: // number
                    break;
                case 3: // ( ExpObject )|( ExpArith )
                    {
                        var t1 = production.Right.ElementAt(1).Name;
                        switch (t1)
                        {
                            case "ExpObject": // ( ExpObject )
                                {
                                    if (ParseToNumberMethod == null) throw new NullReferenceException();

                                    expStack.Pop();
                                    var expObject = expStack.Pop();
                                    expStack.Pop();

                                    var exp = Expression.Call(ParseToNumberMethod, expObject);
                                    expStack.Push(exp);
                                }
                                break;
                            case "ExpArith": // ( ExpArith )
                                {
                                    expStack.Pop();
                                    var expArith = expStack.Pop();
                                    expStack.Pop();

                                    expStack.Push(expArith);
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
            var t0 = production.Right.First().Name;
            switch (rightLength)
            {
                case 1: // id|null
                    {
                        switch (t0)
                        {
                            case "id":
                                {
                                    if (GetMemberMethod == null || Parameter == null)
                                        throw new NullReferenceException();

                                    var exp_id = (ConstantExpression)expStack.Pop();

                                    var exp = Expression.Call(GetMemberMethod, Parameter, exp_id);
                                    expStack.Push(exp);
                                }
                                break;
                            case "null": break;
                            default: throw new NotSupportedException();
                        }
                    }
                    break;
                case 3: // ExpObject . id"
                    {
                        if (GetMemberMethod == null || Parameter == null)
                            throw new NullReferenceException();

                        var exp_id = (ConstantExpression)expStack.Pop();
                        expStack.Pop();
                        var expObject = expStack.Pop();

                        var exp = Expression.Call(GetMemberMethod, expObject, exp_id);
                        expStack.Push(exp);
                    }
                    break;
                case 4: // ExpObject [ ExpArith ]
                    {
                        if (GetIndexMethod == null || Parameter == null)
                            throw new NullReferenceException();

                        expStack.Pop();
                        var expArith = expStack.Pop();
                        expStack.Pop();
                        var expObject = expStack.Pop();

                        var exp = Expression.Call(GetIndexMethod, expObject, expArith);
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

        public LexicalAnalyzer.LexicalAnalyzer? LexicalAnalyzer { get; set; }
        public Func<object, object?> Analyzer(string text)
        {
            using var reader = new StringReader(text);
            if (LexicalAnalyzer == null)
                throw new ArgumentNullException("LexicalAnalyzer", "词法分析器不能为空");
            Analyzer(LexicalAnalyzer.GetEnumerator(reader));

            if (Target == null || Parameter == null)
                throw new NullReferenceException();

            return Expression.Lambda<Func<object, object?>>(Target, Parameter).Compile();
        }

        public static ArithmeticSyntaxAnalyzer LoadFromFile(string? fileName = null)
        {
            if (fileName == null)
                fileName = ArithmeticSyntaxBuilder.DefaultFileName;

            FileInfo syntaxFile = new($"{fileName}.syntax");
            FileInfo lexicalFile = new($"{fileName}.lexical");

            if (!syntaxFile.Exists || !lexicalFile.Exists)
            {
                if (fileName == ArithmeticSyntaxBuilder.DefaultFileName)
                {
                    ArithmeticSyntaxBuilder.CreateRegularFiles();
                    syntaxFile.Refresh();
                    if (!syntaxFile.Exists)
                        throw new FileNotFoundException("文件未找到", syntaxFile.FullName);
                    lexicalFile.Refresh();
                    if (!lexicalFile.Exists)
                        throw new FileNotFoundException("文件未找到", lexicalFile.FullName);
                }
            }

            Dictionary<(int state, Terminal t), List<ActionItem>>? actionTable = null;
            Dictionary<(int state, NonTerminal t), int>? gotoTable = null;
            LexicalAnalyzer.LexicalAnalyzer? lexical = null;

            if (lexicalFile.Exists)
            {
                using (var fs = lexicalFile.Open(FileMode.Open))
                using (var br = new BinaryReader(fs))
                {
                    lexical = new(br);
                }
            }
            else
            {
                throw new FileNotFoundException("找不到词法数据", lexicalFile.FullName);
            }

            if (syntaxFile.Exists)
            {
                using (var fs = syntaxFile.Open(FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    actionTable = LRSyntaxAnalyzerHelper.LoadActionTable(br);
                    gotoTable = LRSyntaxAnalyzerHelper.LoadGotoTable(br);
                }
            }
            else
            {
                throw new FileNotFoundException("找不到语法数据", syntaxFile.FullName);
            }

            return new(actionTable, gotoTable) { LexicalAnalyzer = lexical };
        }
    }
}
