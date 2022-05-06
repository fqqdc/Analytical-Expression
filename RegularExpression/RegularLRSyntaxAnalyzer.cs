using LexicalAnalyzer;
using SyntaxAnalyzer;

namespace RegularExpression
{
    public class RegularLRSyntaxAnalyzer : LRSyntaxAnalyzer
    {
        public RegularLRSyntaxAnalyzer(
            Dictionary<(int state, Terminal t), List<ActionItem>> actionTable,
            Dictionary<(int state, NonTerminal t), int> gotoTable
            ) : base(actionTable, gotoTable) { }

        public LexicalAnalyzer.LexicalAnalyzer? LexicalAnalyzer { get; set; }
        public static RegularLRSyntaxAnalyzer LoadFromFile(string? fileName = null)
        {
            if (fileName == null)
                fileName = RegularLRSyntaxBuilder.DefaultFileName;

            FileInfo syntaxFile = new($"{fileName}.syntax");
            FileInfo lexicalFile = new($"{fileName}.lexical");

            if (!syntaxFile.Exists || !lexicalFile.Exists)
            {
                if (fileName == RegularLRSyntaxBuilder.DefaultFileName)
                {
                    RegularLRSyntaxBuilder.CreateRegularFiles();
                    syntaxFile.Refresh();
                    if (!syntaxFile.Exists)
                        throw new FileNotFoundException("文件未找到", syntaxFile.FullName);
                    lexicalFile.Refresh();
                    if (!lexicalFile.Exists)
                        throw new FileNotFoundException("文件未找到", lexicalFile.FullName);
                }
            }


            var symEnumerator = Enumerable.Empty<(Terminal sym, string symToken)>().GetEnumerator();

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
                    //Console.WriteLine(actionTable.ToFullString());
                    gotoTable = LRSyntaxAnalyzerHelper.LoadGotoTable(br);
                    //Console.WriteLine(gotoTable.ToFullString());
                }
            }
            else
            {
                throw new FileNotFoundException("找不到语法数据", syntaxFile.FullName);
            }

            return new(actionTable, gotoTable) { LexicalAnalyzer = lexical };
        }

        Stack<object> nfaStack = new();

        public NFA? RegularNFA { get; set; }

        protected override void OnShiftItem(Terminal terminal, string terminalToken)
        {
            base.OnShiftItem(terminal, terminalToken);

            switch (terminal.Name)
            {
                case "char":
                    Action_char(terminalToken);
                    break;
                case "charGroup":
                    Action_charGroup(terminalToken);
                    break;
                default:
                    nfaStack.Push(terminalToken);
                    break;
            }
        }

        private void Action_char(string terminalToken)
        {
            if (terminalToken.Length == 1)
                nfaStack.Push(terminalToken);
            else if (terminalToken.Length == 2 && terminalToken[0] == '\\')
            {
                nfaStack.Push(terminalToken.Substring(1, 1));
            }
            else throw new NotSupportedException();
        }

        private NFA nfaDigit = NFA.CreateRange('0', '9');
        private NFA nfaLetter = NFA.CreateRange('a', 'z').Or(NFA.CreateRange('A', 'Z'));
        private void Action_charGroup(string terminalToken)
        {
            switch (terminalToken)
            {
                case ".":
                    nfaStack.Push(nfaDigit.Or(nfaLetter));
                    break;
                case "\\w":
                    nfaStack.Push(nfaLetter);
                    break;
                case "\\s":
                    nfaStack.Push(NFA.CreateFrom(' ', '\t'));
                    break;
                case "\\d":
                    nfaStack.Push(nfaDigit);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected override void OnReduceItem(Production production)
        {
            base.OnReduceItem(production);

            switch (production.Left.Name)
            {
                case "Group":
                    Action_Group(production);
                    break;
                case "Optional":
                    Action_Optional(production);
                    break;
                case "Array":
                    Action_Array(production);
                    break;
                case "JoinExp":
                    Action_JoinExp(production);
                    break;
                case "OrExp":
                    Action_OrExp(production);
                    break;
                case "Exp":
                    Action_Exp(production);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void Action_Exp(Production production)
        {
        }

        private void Action_OrExp(Production production)
        {
            if (production.Right.First().Name == "OrExp")
            {
                var nfa1 = (NFA)nfaStack.Pop();
                nfaStack.Pop();
                var nfa3 = (NFA)nfaStack.Pop();
                nfaStack.Push(nfa3.Or(nfa1));
            }
        }

        private void Action_JoinExp(Production production)
        {
            if (production.Right.First().Name == "JoinExp")
            {
                var nfa1 = (NFA)nfaStack.Pop();
                var nfa2 = (NFA)nfaStack.Pop();
                nfaStack.Push(nfa2.Join(nfa1));
            }
        }

        private void Action_Array(Production production)
        {
            var opt = production.Right.ElementAt(1).Name;
            nfaStack.Pop();
            var nfa = (NFA)nfaStack.Pop();
            switch (opt)
            {
                case "?":
                    nfaStack.Push(NFA.CreateEpsilon().Or(nfa));
                    break;
                case "*":
                    nfaStack.Push(nfa.Closure());
                    break;
                case "+":
                    nfaStack.Push(nfa.Join(nfa.Closure()));
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void Action_Optional(Production production)
        {
            var rightLength = production.Right.Count();
            switch (rightLength)
            {
                case 1: // char
                    {
                        var str1 = (string)nfaStack.Pop();
                        nfaStack.Push(NFA.CreateFrom(str1[0]));
                    }
                    break;
                case 2: // Optional char
                    {
                        var str1 = (string)nfaStack.Pop();
                        var nfa1 = (NFA)nfaStack.Pop();
                        nfaStack.Push(nfa1.Join(NFA.CreateFrom(str1[0])));
                    }
                    break;
                case 3: // char - char
                    {
                        var str1 = (string)nfaStack.Pop();
                        nfaStack.Pop();
                        var str2 = (string)nfaStack.Pop();
                        if (str1[0] > str2[0])
                            nfaStack.Push(NFA.CreateRange(str2[0], str1[0]));
                        else throw new NotSupportedException();
                    }
                    break;
                case 4: // Optional char - char
                    {
                        var str1 = (string)nfaStack.Pop();
                        nfaStack.Pop();
                        var str2 = (string)nfaStack.Pop();
                        var nfa1 = (NFA)nfaStack.Pop();
                        if (str1[0] > str2[0])
                            nfaStack.Push(nfa1.Or(NFA.CreateRange(str2[0], str1[0])));
                        else throw new NotSupportedException();
                    }
                    break;
                default: throw new NotSupportedException();
            }
        }

        private void Action_Group(Production production)
        {
            var fstName = production.Right.First().Name;
            if (fstName == "(" || fstName == "[")
            {
                nfaStack.Pop();
                var nfa = (NFA)nfaStack.Pop();
                nfaStack.Pop();
                nfaStack.Push(nfa);
            }
            else if (fstName == "char")
            {
                var str = (string)nfaStack.Pop();
                nfaStack.Push(NFA.CreateFrom(str[0]));
            }
        }

        protected override void OnAcceptItem()
        {
            base.OnAcceptItem();

            RegularNFA = (NFA)nfaStack.Pop();
        }

        public void Analyzer(string text)
        {
            using var reader = new StringReader(text);
            if (LexicalAnalyzer == null)
                throw new ArgumentNullException("LexicalAnalyzer", "词法分析器不能为空");
            Analyzer(LexicalAnalyzer.GetEnumerator(reader));
        }
    }
}
