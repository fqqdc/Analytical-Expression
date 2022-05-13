using LexicalAnalyzer;
using SyntaxAnalyzer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static MethodInfo? GetMemberMethod = typeof(ArithmeticHelper).GetMethod(nameof(ArithmeticHelper.GetMember), new[] { typeof(object), typeof(string) });
        private static MethodInfo? GetIndexMethod = typeof(ArithmeticHelper).GetMethod(nameof(ArithmeticHelper.GetIndex), new[] { typeof(object), typeof(double) });
        private static MethodInfo? ParseToNumberMethod = typeof(ArithmeticHelper).GetMethod(nameof(ArithmeticHelper.ParseToNumber), new[] { typeof(object) });
        private static MethodInfo? ObjectEqualsMethod = typeof(object).GetMethod(nameof(object.Equals), new[] { typeof(object), typeof(object) });

        private Dictionary<Production, Func<object[], object>> _semanticSubroutine = new(); // 语义子程序字典
        private Stack<object> _pushdownStack = new(); // 下推栈
        private Func<object[], object> _defaultFunction = arr => arr[0];

        public ArithmeticSyntaxAnalyzer(Dictionary<(int state, Terminal t), List<ActionItem>> actionTable, Dictionary<(int state, NonTerminal t), int> gotoTable)
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
                var expObject1 = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                return Expression.Power(expObject1, (Expression)arr[2]);
            }); // 开方-数值与对象
            RegisterFunction("ExpSquare", "ExpNumber ^ ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject2 = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.Power((Expression)arr[0], expObject2);
            }); // 开方-数值与对象
            RegisterFunction("ExpSquare", "ExpObject ^ ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject1 = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                var expObject2 = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.Power(expObject1, expObject2);
            }); // 开方-对象
            // 取反
            RegisterFunction("ExpSign", "ExpSquare", _defaultFunction);
            RegisterFunction("ExpSign", "- ExpSign", arr =>
            {
                var expSign = (Expression)arr[1];
                return Expression.Negate(expSign);
            });
            RegisterFunction("ExpSign", "- ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[1]);
                return Expression.Negate(expObject);
            }); // 取反-对象
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
            RegisterFunction("ExpMulti", "ExpObject % ExpSign", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                return Expression.Modulo(expObject, (Expression)arr[2]);
            }); // 乘法-对象op数值
            RegisterFunction("ExpMulti", "ExpObject / ExpSign", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                return Expression.Divide(expObject, (Expression)arr[2]);
            }); // 乘法-对象op数值
            RegisterFunction("ExpMulti", "ExpObject * ExpSign", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                return Expression.Multiply(expObject, (Expression)arr[2]);
            }); // 乘法-对象op数值
            RegisterFunction("ExpMulti", "ExpMulti % ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.Modulo((Expression)arr[0], expObject);
            }); // 乘法-数值op对象
            RegisterFunction("ExpMulti", "ExpMulti / ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.Divide((Expression)arr[0], expObject);
            }); // 乘法-数值op对象
            RegisterFunction("ExpMulti", "ExpMulti * ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.Multiply((Expression)arr[0], expObject);
            }); // 乘法-数值op对象
            RegisterFunction("ExpMulti", "ExpObject % ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject1 = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                var expObject2 = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.Modulo(expObject1, expObject2);
            }); // 乘法-对象  
            RegisterFunction("ExpMulti", "ExpObject / ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject1 = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                var expObject2 = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.Divide(expObject1, expObject2);
            }); // 乘法-对象   
            RegisterFunction("ExpMulti", "ExpObject * ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject1 = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                var expObject2 = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.Multiply(expObject1, expObject2);
            }); // 乘法-对象
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
            RegisterFunction("ExpAdd", "ExpObject - ExpMulti", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                return Expression.Subtract(expObject, (Expression)arr[2]);
            }); // 加法-对象op数值
            RegisterFunction("ExpAdd", "ExpObject + ExpMulti", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                return Expression.Add(expObject, (Expression)arr[2]);
            }); // 加法-对象op数值
            RegisterFunction("ExpAdd", "ExpAdd - ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.Subtract((Expression)arr[0], expObject);
            }); // 加法-数值op对象
            RegisterFunction("ExpAdd", "ExpAdd + ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.Add((Expression)arr[0], expObject);
            }); // 加法-数值op对象
            RegisterFunction("ExpAdd", "ExpObject - ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject1 = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                var expObject2 = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.Subtract(expObject1, expObject2);
            }); // 加法-对象
            RegisterFunction("ExpAdd", "ExpObject + ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject1 = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                var expObject2 = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.Add(expObject1, expObject2);
            }); // 加法-对象
            // 算术表达式
            RegisterFunction("ExpArith", "ExpAdd", _defaultFunction);
            // 布尔表达式
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
                return Expression.Not(Expression.Call(ObjectEqualsMethod, (Expression)arr[0], (Expression)arr[1]));
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
            RegisterFunction("ExpBool", "ExpObject != ExpArith", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                return Expression.NotEqual(expObject, (Expression)arr[2]);
            }); // 比较-数值与对象
            RegisterFunction("ExpBool", "ExpObject == ExpArith", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                return Expression.Equal(expObject, (Expression)arr[2]);
            }); // 比较-数值与对象
            RegisterFunction("ExpBool", "ExpObject <= ExpArith", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                return Expression.LessThanOrEqual(expObject, (Expression)arr[2]);
            }); // 比较-数值与对象
            RegisterFunction("ExpBool", "ExpObject < ExpArith", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                return Expression.LessThan(expObject, (Expression)arr[2]);
            }); // 比较-数值与对象
            RegisterFunction("ExpBool", "ExpObject >= ExpArith", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                return Expression.GreaterThanOrEqual(expObject, (Expression)arr[2]);
            }); // 比较-数值与对象
            RegisterFunction("ExpBool", "ExpObject > ExpArith", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                return Expression.GreaterThan(expObject, (Expression)arr[2]);
            }); // 比较-数值与对象
            RegisterFunction("ExpBool", "ExpArith != ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.NotEqual((Expression)arr[0], expObject);
            }); // 比较-数值与对象            
            RegisterFunction("ExpBool", "ExpArith == ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.Equal((Expression)arr[0], expObject);
            }); // 比较-数值与对象            
            RegisterFunction("ExpBool", "ExpArith <= ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.LessThanOrEqual((Expression)arr[0], expObject);
            }); // 比较-数值与对象
            RegisterFunction("ExpBool", "ExpArith < ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.LessThan((Expression)arr[0], expObject);
            }); // 比较-数值与对象
            RegisterFunction("ExpBool", "ExpArith >= ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.GreaterThanOrEqual((Expression)arr[0], expObject);
            }); // 比较-数值与对象
            RegisterFunction("ExpBool", "ExpArith > ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.GreaterThan((Expression)arr[0], expObject);
            }); // 比较-数值与对象
            RegisterFunction("ExpBool", "ExpObject <= ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject1 = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                var expObject2 = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.LessThanOrEqual(expObject1, expObject2);
            }); // 比较-对象  
            RegisterFunction("ExpBool", "ExpObject < ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject1 = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                var expObject2 = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.LessThan(expObject1, expObject2);
            }); // 比较-对象
            RegisterFunction("ExpBool", "ExpObject >= ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject1 = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                var expObject2 = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.GreaterThanOrEqual(expObject1, expObject2);
            }); // 比较-对象
            RegisterFunction("ExpBool", "ExpObject > ExpObject", arr =>
            {
                if (ParseToNumberMethod == null) throw new NullReferenceException();
                var expObject1 = Expression.Call(ParseToNumberMethod, (Expression)arr[0]);
                var expObject2 = Expression.Call(ParseToNumberMethod, (Expression)arr[2]);
                return Expression.GreaterThan(expObject1, expObject2);
            }); // 比较-对象
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
            _semanticSubroutine[Production.CreateSingle(left, right)] = function;
        }

        [Conditional("_UnusedTemplate_")]
        private void RegisterExpObject()
        {
            void Register(string right, Func<object[], object> function) { RegisterFunction("ExpObject", right, function); }

            Register("id", arr =>
            {
                if (GetMemberMethod == null || Parameter == null)
                    throw new NullReferenceException();
                var exp_id = (ConstantExpression)arr[0];
                return Expression.Call(GetMemberMethod, Parameter, exp_id);
            });
            Register("null", _defaultFunction);
            Register("ExpObject . id", arr =>
            {
                if (GetMemberMethod == null)
                    throw new NullReferenceException();
                var exp_id = (ConstantExpression)arr[2];
                var expObject = (Expression)arr[0];
                return Expression.Call(GetMemberMethod, expObject, exp_id);
            }); // 读取对象、字典(<string,object>)
            Register("ExpObject [ ExpArith ]", arr =>
            {
                if (GetIndexMethod == null) throw new NullReferenceException();
                var expArith = (Expression)arr[2];
                var expObject = (Expression)arr[0];
                return Expression.Call(GetIndexMethod, expObject, expArith);
            }); // 读取数组
            Register("ExpObject [ ExpObject ]", arr =>
            {
                if (ParseToNumberMethod == null || GetIndexMethod == null)
                    throw new NullReferenceException();
                var expObject2 = (Expression)arr[2];
                var expObject1 = (Expression)arr[0];
                var expObject2ToNumber = Expression.Call(ParseToNumberMethod, expObject2);
                return Expression.Call(GetIndexMethod, expObject1, expObject2ToNumber);
            }); // 读取数组-对象索引
        }

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
                        _pushdownStack.Push(Expression.Constant(double.Parse(terminalToken), typeof(double)));
                    }
                    break;
                case "bool":
                    {
                        _pushdownStack.Push(Expression.Constant(bool.Parse(terminalToken), typeof(bool)));
                    }
                    break;
                case "null":
                    {
                        _pushdownStack.Push(Expression.Constant(null, typeof(object)));
                    }
                    break;
                case "id":
                default:
                    {
                        _pushdownStack.Push(Expression.Constant(terminalToken, typeof(string)));
                    }
                    break;
            }
        }

        protected override void OnReduceItem(Production production)
        {
            base.OnReduceItem(production);

            if (!_semanticSubroutine.TryGetValue(production, out var function))
                throw new NotSupportedException($"找不到产生式 {production} 的语义子程序。");

            // 使用语义子程序替换下推栈对象
            var rightLength = production.Right.Count();
            object[] rightObjects = new object[rightLength];
            for (int i = rightLength - 1; i >= 0; i--)
                rightObjects[i] = _pushdownStack.Pop();
            _pushdownStack.Push(function(rightObjects));
        }

        protected override void OnAcceptItem()
        {
            base.OnAcceptItem();

            Target = Expression.Convert((Expression)_pushdownStack.Pop(), typeof(object));
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
