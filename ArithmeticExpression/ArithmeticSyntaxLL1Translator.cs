using LexicalAnalyzer;
using SyntaxAnalyzer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ArithmeticExpression
{
    public class ArithmeticSyntaxLL1Translator : LL1SyntaxAnalyzer
    {
        private Dictionary<Production, Func<object[], object>> _semanticSubroutine = new(); // 语义子程序字典
        private Stack<object> _pushdownStack = new(); // 下推栈
        private Func<object[], object> _defaultFunction = arr => arr[0];

        private ParameterExpression _varExp = Expression.Variable(typeof(double));
        private VariableModifier _modifier;

        public ArithmeticSyntaxLL1Translator(LL1Grammar grammar, AdvanceProcedure advanceProcedure) : base(grammar, advanceProcedure)
        {
            _modifier = new VariableModifier(_varExp);

            RegisterFunction("ExpNumber", "number", _defaultFunction);
            RegisterFunction("ExpNumber", "( ExpArith )", arr =>
            {
                return arr[1];
            });
            RegisterFunction("ExpMultiA", "", arr =>
            {
                return _varExp;
            });
            RegisterFunction("ExpMultiA", "* ExpNumber ExpMultiA", arr =>
            {
                var expNumber = (Expression)arr[1];
                var expMultiA = (Expression)arr[2];
                var exp = Expression.Multiply(_varExp, expNumber);
                return _modifier.Modify(expMultiA, exp);
            });
            RegisterFunction("ExpMulti", "ExpNumber ExpMultiA", arr =>
            {
                var expNumber = (Expression)arr[0];
                var expMultiA = (Expression)arr[1];
                return _modifier.Modify(expMultiA, expNumber);
            });
            RegisterFunction("ExpAddA", "", arr =>
            {
                return _varExp;
            });
            RegisterFunction("ExpAddA", "+ ExpMulti ExpAddA", arr =>
            {
                var expMulti = (Expression)arr[1];
                var expAddA = (Expression)arr[2];
                var exp = Expression.Add(_varExp, expMulti);
                return _modifier.Modify(expAddA, exp);
            });
            RegisterFunction("ExpAdd", "ExpMulti ExpAddA", arr =>
            {
                var expMulti = (Expression)arr[0];
                var expAddA = (Expression)arr[1];
                return _modifier.Modify(expAddA, expMulti);
            });
            RegisterFunction("ExpArith", "ExpAdd", _defaultFunction);
        }

        private void RegisterFunction(string left, string right, Func<object[], object> function)
        {
            _semanticSubroutine[Production.CreateSingle(left, right)] = function;
        }

        protected override void OnTerminalFinish(Terminal terminal, string terminalToken)
        {
            base.OnTerminalFinish(terminal, terminalToken);

            switch (terminal.Name)
            {
                case "number":
                    {
                        _pushdownStack.Push(Expression.Constant(double.Parse(terminalToken), typeof(double)));
                    }
                    break;
                default:
                    {
                        _pushdownStack.Push(terminalToken);
                    }
                    break;
            }
        }

        protected override void OnProcedureFinish(Production production)
        {
            base.OnProcedureFinish(production);

            if (!_semanticSubroutine.TryGetValue(production, out var function))
                throw new NotSupportedException($"找不到产生式 {production} 的语义子程序。");

            // 使用语义子程序替换下推栈对象
            var rightLength = production.Right.Count();
            if (production.Right.SequenceEqual(Production.Epsilon))
                rightLength = 0;
            object[] rightObjects = new object[rightLength];
            for (int i = rightLength - 1; i >= 0; i--)
                rightObjects[i] = _pushdownStack.Pop();
            _pushdownStack.Push(function(rightObjects));
        }

        protected override void OnAnalyzerFinish()
        {
            base.OnAnalyzerFinish();

            Target = (Expression)_pushdownStack.Pop();
        }

        public LexicalAnalyzer.LexicalAnalyzer? LexicalAnalyzer { get; set; }

        public static ArithmeticSyntaxLL1Translator LoadFromFile(string? fileName = null)
        {
            if (fileName == null)
                fileName = ArithmeticSyntaxBuilder.DefaultFileName;

            FileInfo lexicalFile = new($"{fileName}.lexical");

            if (!lexicalFile.Exists)
                throw new FileNotFoundException("文件未找到", lexicalFile.FullName);

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

            var listProduction = new List<Production>();
            listProduction.AddRange(Production.Create("ExpNumber", "number|( ExpArith )")); // 乘法
            listProduction.AddRange(Production.Create("ExpMulti", "ExpNumber ExpMultiA")); // 乘法
            listProduction.AddRange(Production.Create("ExpMultiA", "* ExpNumber ExpMultiA|")); // 乘法
            listProduction.AddRange(Production.Create("ExpAdd", "ExpMulti ExpAddA")); // 加法
            listProduction.AddRange(Production.Create("ExpAddA", "+ ExpMulti ExpAddA|")); // 加法
            listProduction.AddRange(Production.Create("ExpArith", "ExpAdd")); // 算术表达式
            Grammar grammar = new Grammar(listProduction, new("ExpArith"));

            if (!LL1Grammar.TryCreate(grammar, out var lL1Grammar, out var slrMsg))
            {
                Console.WriteLine();
                Console.WriteLine($"Error:\n{slrMsg}");

                Console.WriteLine(grammar);
                throw new NotSupportedException("LL1文法创建失败");
            }

            return new(lL1Grammar, (out Terminal _, out string _) => throw new NotSupportedException()) { LexicalAnalyzer = lexical };
        }

        public Expression? Target { get; private set; }
        public Func<double> Translate(string text)
        {
            using var reader = new StringReader(text);
            if (LexicalAnalyzer == null)
                throw new ArgumentNullException("LexicalAnalyzer", "词法分析器不能为空");
            var obj = LexicalAnalyzer.GetEnumerator(reader);
            _advanceProcedure = (out Terminal t, out string token) =>
            {
                if (obj.MoveNext())
                    (t, token) = obj.Current;
                else
                    (t, token) = (Terminal.EndTerminal, string.Empty);
            };

            Analyzer();

            if (Target == null)
                throw new NullReferenceException();

            return Expression.Lambda<Func<double>>(Target).Compile();
        }
    }

    internal class VariableModifier : ExpressionVisitor
    {
        public ParameterExpression _replaced;
        public Expression? _replace;

        public VariableModifier(ParameterExpression replaced)
        {
            _replaced = replaced;
        }

        public Expression Modify(Expression expression, Expression replace)
        {
            try
            {
                _replace = replace;
                return Visit(expression);
            }
            finally
            {
                _replace = null;
            }

        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (_replace != null && node == _replaced)
                return _replace;

            return base.VisitParameter(node);
        }
    }
}
