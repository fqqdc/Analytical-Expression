using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LexicalAnalyzer;
using SyntaxAnalyzer;

namespace SyntaxAnalyzerTest
{
    public class RegularLRSyntaxAnalyzer : LRSyntaxAnalyzer
    {
        public RegularLRSyntaxAnalyzer(
            Dictionary<(int state, Terminal t), List<ActionItem>> actionTable,
            Dictionary<(int state, NonTerminal t), int> gotoTable,
            IEnumerator<(Terminal sym, string symToken)> symEnumerator
            ) : base(actionTable, gotoTable, symEnumerator) { }

        Stack<Object> nfaStack = new();

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
                nfaStack.Push(terminalToken[1..2]);
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
                case "CharGroup":
                    Action_CharGroup(production);
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
            var nfa2 = (NFA)nfaStack.Pop();
            switch (opt)
            {
                case "?":
                    nfaStack.Push(NFA.CreateEpsilon().Or(nfa2));
                    break;
                case "*":
                    nfaStack.Push(nfa2.Closure());
                    break;
                case "+":
                    nfaStack.Push(nfa2.Join(nfa2.Closure()));
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void Action_CharGroup(Production production)
        {
            if (production.Right.First().Name == "char")
            {
                var str1 = (string)nfaStack.Pop();
                if (production.Right.Count() > 1)
                {
                    nfaStack.Pop();
                    var str2 = (string)nfaStack.Pop();
                    if (str1[0] > str2[0])
                        nfaStack.Push(NFA.CreateRange(str2[0], str1[0]));
                    else throw new NotSupportedException();
                }
                else
                {
                    nfaStack.Push(NFA.CreateFrom(str1[0]));
                }


            }
            else if (production.Right.First().Name == "CharGroup")
            {
                var str1 = (string)nfaStack.Pop();
                if (production.Right.Count() == 2)
                {
                    var nfa1 = (NFA)nfaStack.Pop();
                    nfaStack.Push(nfa1.Join(NFA.CreateFrom(str1[0])));
                }
                else
                {
                    nfaStack.Pop();
                    var str2 = (string)nfaStack.Pop();
                    var nfa1 = (NFA)nfaStack.Pop();
                    if (str1[0] > str2[0])
                        nfaStack.Push(nfa1.Or(NFA.CreateRange(str2[0], str1[0])));
                    else throw new NotSupportedException();
                }
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
    }
}
