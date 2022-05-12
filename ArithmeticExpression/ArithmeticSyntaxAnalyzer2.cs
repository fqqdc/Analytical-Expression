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
    public class ArithmeticSyntaxAnalyzer2 : LRSyntaxAnalyzer
    {
        private static MethodInfo? GetMemberMethod = typeof(ArithmeticHelper).GetMethod("GetMember", new[] { typeof(object), typeof(string) });
        private static MethodInfo? GetIndexMethod = typeof(ArithmeticHelper).GetMethod("GetIndex", new[] { typeof(object), typeof(double) });
        private static MethodInfo? ParseToNumberMethod = typeof(ArithmeticHelper).GetMethod("ParseToNumber", new[] { typeof(object) });
        private static MethodInfo? ObjectEqualsMethod = typeof(object).GetMethod("Equals", new[] { typeof(object), typeof(object) });

        private Dictionary<Production, Func<object[], object>> _reduceFunction = new();
        private Stack<object> _pushdownStack = new();
        private Func<object[], object> _defaultFunction = arr => arr[0];

        public ArithmeticSyntaxAnalyzer2(Dictionary<(int state, Terminal t), List<ActionItem>> actionTable, Dictionary<(int state, NonTerminal t), int> gotoTable)
            : base(actionTable, gotoTable)
        {
            // 对象
            RegisterFunction("ExpObject", "id", arr =>
            {
                if (GetMemberMethod == null || Parameter == null)
                    throw new NullReferenceException();
                var exp_id = (ConstantExpression)arr[0];
                return Expression.Call(GetMemberMethod, Parameter, exp_id);
            });
            RegisterFunction("ExpObject", "null", _defaultFunction);
            RegisterFunction("ExpObject", "ExpObject . id", arr =>
            {
                if (GetMemberMethod == null)
                    throw new NullReferenceException();
                var exp_id = (ConstantExpression)arr[2];
                var expObject = (Expression)arr[0];
                return Expression.Call(GetMemberMethod, expObject, exp_id);
            }); // 读取对象、字典(<string,object>)
            RegisterFunction("ExpObject", "ExpObject [ ExpArith ]", arr =>
            {
                if (GetIndexMethod == null) throw new NullReferenceException();
                var expArith = (Expression)arr[2];
                var expObject = (Expression)arr[0];
                return Expression.Call(GetIndexMethod, expObject, expArith);
            }); // 读取数组
            RegisterFunction("ExpObject", "ExpObject [ ExpObject ]", arr =>
            {
                if (ParseToNumberMethod == null || GetIndexMethod == null) 
                    throw new NullReferenceException();
                var expObject2 = (Expression)arr[2];
                var expObject1 = (Expression)arr[0];
                var expObject2ToNumber = Expression.Call(ParseToNumberMethod, expObject2);
                return Expression.Call(GetIndexMethod, expObject1, expObject2ToNumber);
            }); // 读取数组-对象索引
            // 数值
            RegisterFunction("ExpNumber", "number", _defaultFunction);
            RegisterFunction("ExpNumber", "( ExpArith )", arr =>
            {
                return (Expression)arr[1];
            });
            RegisterFunction("ExpNumber", "( ExpObject )", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                return Expression.Call(ParseToNumberMethod, (Expression)arr[1]);

            }); // 将对象转换为数值，可抛出异常
            // 开方
            RegisterFunction("ExpSquare", "ExpNumber", _defaultFunction);
            RegisterFunction("ExpSquare", "ExpNumber ^ ExpSquare", arr =>
            {
                var expSquare = (Expression)arr[2];
                var expNumber = (Expression)arr[0];
                return Expression.Power(expNumber, expSquare);
            });
            RegisterFunction("ExpSquare", "ExpObject ^ ExpSquare", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject1 = (Expression)arr[0];
                if (expObject1.Type != typeof(double))
                    expObject1 = Expression.Call(ParseToNumberMethod, expObject1);
                var expSquare = (Expression)arr[2];
                return Expression.Power(expObject1, expSquare);
            }); // 开方-数值与对象
            RegisterFunction("ExpSquare", "ExpNumber ^ ExpObject", arr => {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expNumber = (Expression)arr[0];
                var expObject2 = (Expression)arr[2];
                if (expObject2.Type != typeof(double))
                    expObject2 = Expression.Call(ParseToNumberMethod, expObject2);
                return Expression.Power(expNumber, expObject2);
            }); // 开方-数值与对象
            RegisterFunction("ExpSquare", "ExpObject ^ ExpObject", arr => {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject1 = (Expression)arr[0];
                if (expObject1.Type != typeof(double))
                    expObject1 = Expression.Call(ParseToNumberMethod, expObject1);
                var expObject2 = (Expression)arr[2];
                if (expObject2.Type != typeof(double))
                    expObject2 = Expression.Call(ParseToNumberMethod, expObject2);
                return Expression.Power(expObject1, expObject2);
            }); // 开方-对象
            // 取反
            RegisterFunction("ExpSign", "ExpSquare", _defaultFunction);
            RegisterFunction("ExpSign", "- ExpSign", arr =>
            {
                var expSign = (Expression)arr[1];
                return Expression.Negate(expSign);
            });
            // 乘法
            RegisterFunction("ExpMulti", "ExpSign", _defaultFunction);
            RegisterFunction("ExpMulti", "ExpMulti * ExpSign", arr =>
            {
                return Expression.Multiply((Expression)arr[0], (Expression)arr[2]);
            });
            RegisterFunction("ExpMulti", "ExpMulti / ExpSign", arr =>
            {
                return Expression.Divide((Expression)arr[0], (Expression)arr[2]);
            });
            RegisterFunction("ExpMulti", "ExpMulti % ExpSign", arr =>
            {
                return Expression.Modulo((Expression)arr[0], (Expression)arr[2]);
            });
            // 加法
            RegisterFunction("ExpAdd", "ExpMulti", _defaultFunction);
            RegisterFunction("ExpAdd", "ExpAdd + ExpMulti", arr =>
            {
                return Expression.Add((Expression)arr[0], (Expression)arr[2]);
            });
            RegisterFunction("ExpAdd", "ExpAdd - ExpMulti", arr =>
            {
                return Expression.Subtract((Expression)arr[0], (Expression)arr[2]);
            });
            // 算术表达式
            RegisterFunction("ExpArith", "ExpAdd", _defaultFunction);
            // 布尔
            RegisterFunction("ExpBool", "bool", _defaultFunction);
            RegisterFunction("ExpBool", "( ExpLogic )", arr =>
            {
                return (Expression)arr[1];
            });
            RegisterFunction("ExpBool", "ExpObject == ExpObject", arr =>
            {
                if (ObjectEqualsMethod == null) throw new NullReferenceException();
                return Expression.Call(ObjectEqualsMethod, (Expression)arr[0], (Expression)arr[1]);
            }); // 比较对象
            RegisterFunction("ExpBool", "ExpObject != ExpObject", arr =>
            {
                if (ObjectEqualsMethod == null) throw new NullReferenceException();
                return Expression.Negate(Expression.Call(ObjectEqualsMethod, (Expression)arr[0], (Expression)arr[1]));
            }); // 比较对象
            RegisterFunction("ExpBool", "ExpArith > ExpArith", arr =>
            {
                return Expression.GreaterThan((Expression)arr[0], (Expression)arr[2]);
            }); // 比较数值
            RegisterFunction("ExpBool", "ExpArith >= ExpArith", arr =>
            {
                return Expression.GreaterThanOrEqual((Expression)arr[0], (Expression)arr[2]);
            }); // 比较数值
            RegisterFunction("ExpBool", "ExpArith < ExpArith", arr =>
            {
                return Expression.LessThan((Expression)arr[0], (Expression)arr[2]);
            }); // 比较数值
            RegisterFunction("ExpBool", "ExpArith <= ExpArith", arr =>
            {
                return Expression.LessThanOrEqual((Expression)arr[0], (Expression)arr[2]);
            }); // 比较数值
            RegisterFunction("ExpBool", "ExpArith == ExpArith", arr =>
            {
                return Expression.Equal((Expression)arr[0], (Expression)arr[2]);
            }); // 比较数值
            RegisterFunction("ExpBool", "ExpArith != ExpArith", arr =>
            {
                return Expression.NotEqual((Expression)arr[0], (Expression)arr[2]);
            }); // 比较数值
            // 等于
            RegisterFunction("ExpEqual", "ExpBool", _defaultFunction);
            RegisterFunction("ExpEqual", "ExpEqual == ExpBool", arr =>
            {
                return Expression.Equal((Expression)arr[0], (Expression)arr[2]);
            });
            RegisterFunction("ExpEqual", "ExpEqual != ExpBool", arr =>
            {
                return Expression.NotEqual((Expression)arr[0], (Expression)arr[2]);
            });
            // 或    
            RegisterFunction("ExpOr", "ExpEqual", _defaultFunction);
            RegisterFunction("ExpOr", "ExpOr or ExpEqual", arr =>
            {
                return Expression.Or((Expression)arr[0], (Expression)arr[2]);
            });
            // 与
            RegisterFunction("ExpAnd", "ExpOr", _defaultFunction);
            RegisterFunction("ExpAnd", "ExpAnd && ExpOr", arr =>
            {
                return Expression.And((Expression)arr[0], (Expression)arr[2]);
            });
            // 布尔表达式
            RegisterFunction("ExpLogic", "ExpAnd", _defaultFunction);
            // 顶层表达式
            RegisterFunction("Exp", "ExpArith", _defaultFunction);
            RegisterFunction("Exp", "ExpLogic", _defaultFunction);
            RegisterFunction("Exp", "ExpObject", _defaultFunction);
        }

        private void RegisterFunction(string left, string right, Func<object[], object> function)
        {
            _reduceFunction[Production.CreateSingle(left, right)] = function;
        }

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
                        var arrT = production.Right.Select(t => t.Name).ToArray();
                        switch (arrT[0])
                        {
                            case "(": // ( ExpLogic )
                                {
                                    expStack.Pop();
                                    var exp = expStack.Pop();
                                    expStack.Pop();

                                    expStack.Push(exp);
                                }
                                break;
                            default:
                                {
                                    // ExpObject == ExpObject|ExpObject != ExpObject
                                    if (arrT[0] == "ExpObject" && arrT[2] == "ExpObject"
                                            && (arrT[1] == "==") || (arrT[1] == "!="))
                                    {
                                        if (ObjectEqualsMethod == null) throw new NullReferenceException();

                                        var expObject2 = expStack.Pop();
                                        expStack.Pop();
                                        var expObject1 = expStack.Pop();

                                        switch (arrT[1])
                                        {
                                            case "==":
                                                {
                                                    var exp = Expression.Call(ObjectEqualsMethod, expObject1, expObject2);
                                                    expStack.Push(exp);
                                                }
                                                break;
                                            case "!=":
                                                {
                                                    var exp = Expression.Not(Expression.Call(ObjectEqualsMethod, expObject1, expObject2));
                                                    expStack.Push(exp);
                                                }
                                                break;
                                            default: throw new NotSupportedException();
                                        }
                                        break;
                                    }

                                    {
                                        if (ParseToNumberMethod == null) throw new NullReferenceException();

                                        // ExpArith > ExpArith|ExpArith >= ExpArith|ExpArith < ExpArith|ExpArith <= ExpArith"
                                        // ExpArith == ExpArith|ExpArith != ExpArith
                                        var expArith2 = expStack.Pop();
                                        if (expArith2.Type != typeof(double))
                                            expArith2 = Expression.Call(ParseToNumberMethod, expArith2);

                                        expStack.Pop();

                                        var expArith1 = expStack.Pop();
                                        if (expArith1.Type != typeof(double))
                                            expArith1 = Expression.Call(ParseToNumberMethod, expArith1);

                                        switch (arrT[1])
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
                                }
                                break;
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
                        if (ParseToNumberMethod == null) throw new NullReferenceException();

                        var expMulti = expStack.Pop();
                        if (expMulti.Type != typeof(double))
                            expMulti = Expression.Call(ParseToNumberMethod, expMulti);

                        var expOp = (ConstantExpression)expStack.Pop();

                        var expAdd = expStack.Pop();
                        if (expAdd.Type != typeof(double))
                            expAdd = Expression.Call(ParseToNumberMethod, expAdd);

                        if (expOp.Value == null)
                            throw new NullReferenceException();

                        Expression exp;
                        switch (expOp.Value)
                        {
                            case "+":
                                {
                                    exp = Expression.Add(expAdd, expMulti);
                                }
                                break;
                            case "-":
                                {
                                    exp = Expression.Subtract(expAdd, expMulti);
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
                        if (ParseToNumberMethod == null) throw new NullReferenceException();

                        var expSign = expStack.Pop();
                        if (expSign.Type != typeof(double))
                            expSign = Expression.Call(ParseToNumberMethod, expSign);

                        var expOp = (ConstantExpression)expStack.Pop();

                        var expMulti = expStack.Pop();
                        if (expMulti.Type != typeof(double))
                            expMulti = Expression.Call(ParseToNumberMethod, expMulti);

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
                        if (ParseToNumberMethod == null) throw new NullReferenceException();

                        var expSign = expStack.Pop();
                        if (expSign.Type != typeof(double))
                            expSign = Expression.Call(ParseToNumberMethod, expSign);

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
                        if (ParseToNumberMethod == null) throw new NullReferenceException();

                        var expSquare = expStack.Pop();
                        if (expSquare.Type != typeof(double))
                            expSquare = Expression.Call(ParseToNumberMethod, expSquare);

                        expStack.Pop();

                        var expNumber = expStack.Pop();
                        if (expNumber.Type != typeof(double))
                            expNumber = Expression.Call(ParseToNumberMethod, expNumber);

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
                case 4:
                    {
                        if (GetIndexMethod == null || Parameter == null)
                            throw new NullReferenceException();

                        var t2 = production.Right.ElementAt(2).Name;

                        switch (t2)
                        {
                            case "ExpArith": // ExpObject [ ExpArith ]
                                {
                                    expStack.Pop();
                                    var expArith = expStack.Pop();
                                    expStack.Pop();
                                    var expObject = expStack.Pop();

                                    var exp = Expression.Call(GetIndexMethod, expObject, expArith);
                                    expStack.Push(exp);
                                }
                                break;
                            case "ExpObject": // ExpObject [ ExpObject ]
                                {
                                    if (ParseToNumberMethod == null) throw new NullReferenceException();

                                    expStack.Pop();
                                    var expObject2 = expStack.Pop();
                                    expStack.Pop();
                                    var expObject1 = expStack.Pop();

                                    var expObject2ToNumber = Expression.Call(ParseToNumberMethod, expObject2);
                                    var exp = Expression.Call(GetIndexMethod, expObject1, expObject2ToNumber);

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
