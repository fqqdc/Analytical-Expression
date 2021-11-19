using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Analytical_Expression
{
    public class Program
    {
        static void Main(string[] args)
        {
            //n = [0-9]
            //nn *| nn *.|.nn *| nn *.nn *
            var exp_n = NfaDigraphCreater.CreateCharacterRange('0', '9');
            var exp_dot = NfaDigraphCreater.CreateSingleCharacter('.');
            var exp_nns = exp_n.Join(exp_n.Closure());
            var nfa = exp_nns.Join(exp_dot).Join(exp_nns);
            nfa = nfa.Union(exp_nns);
            nfa = nfa.Union(exp_nns.Join(exp_dot));
            nfa = nfa.Union(exp_dot.Join(nfa));
            DfaDigraphNode dfa = DfaDigraphCreater.CreateFrom(nfa);
            var dfaNumber = dfa.Minimize();
            DfaDigraphCreater.PrintDigraph(dfaNumber, "dfa", false);
            StateMachine smNumber = new(dfaNumber) { Name = "Number" };

            //n = [0-9]
            //c = [a-z][A-Z]
            //c(c|n)*
            exp_n = NfaDigraphCreater.CreateCharacterRange('0', '9');
            var exp_c = NfaDigraphCreater.CreateCharacterRange('a', 'z');
            exp_c = exp_c.Union(NfaDigraphCreater.CreateCharacterRange('A', 'Z'));
            nfa = exp_c.Union(exp_n).Closure();
            nfa = exp_c.Join(nfa);
            dfa = DfaDigraphCreater.CreateFrom(nfa);
            var dfaId = dfa.Minimize();
            DfaDigraphCreater.PrintDigraph(dfaId, "dfa", false);
            StateMachine smId = new(dfaId) { Name = "ID" };


            // +|-|*|/|<|<=|==|>=|>
            nfa = NfaDigraphCreater.CreateSingleCharacter('+'); // +
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('-')); // -
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('*')); // *
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('/')); // /
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('<')); // <
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('>')); // >
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('>').Join(NfaDigraphCreater.CreateSingleCharacter('='))); // >=
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('<').Join(NfaDigraphCreater.CreateSingleCharacter('='))); // <=
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('=').Join(NfaDigraphCreater.CreateSingleCharacter('='))); // ==
            nfa = nfa.Union(NfaDigraphCreater.CreateSingleCharacter('=')); // =
            dfa = DfaDigraphCreater.CreateFrom(nfa);
            var dfaSymbol = dfa.Minimize();
            DfaDigraphCreater.PrintDigraph(dfaSymbol, "dfa", false);
            StateMachine smSymbol = new(dfaSymbol) { Name = "Symbol" };

            // (
            nfa = NfaDigraphCreater.CreateSingleCharacter('('); // (
            dfa = DfaDigraphCreater.CreateFrom(nfa);
            var dfaLeft = dfa.Minimize();
            StateMachine smLeft = new(dfaLeft) { Name = "L" };

            // )
            nfa = NfaDigraphCreater.CreateSingleCharacter(')'); // )
            dfa = DfaDigraphCreater.CreateFrom(nfa);
            var dfaRight = dfa.Minimize();
            StateMachine smRight = new(dfaRight) { Name = "R" };

            List<StateMachine> listSM = new() { smNumber, smId, smSymbol, smLeft, smRight };
            HashSet<StateMachine> workSM = new();

            string txt = " 2 *(  3+(4-5) ) / 666>= ccc233  ";
            Console.WriteLine(txt);

            int basePos = 0;
            int curPos = 0;

            while (curPos < txt.Length)
            {
                char c = txt[curPos];

                if (workSM.Count == 0)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        basePos++;
                        curPos++;
                        continue;
                    }
                    else
                    {
                        listSM.ForEach(m => m.Reset());
                        workSM = new(listSM);
                    }
                }

                foreach (var sm in workSM)
                {
                    sm.Jump(c);
                    if (!sm.IsWork)
                        workSM.Remove(sm);
                }
                curPos += 1;


                if (workSM.Count > 0) continue;


                var machine = listSM.Max(new MachineComparer());
                if (machine != null && machine.Count != 0)
                {
                    Console.WriteLine($"{machine.Name,-10}:{txt.Substring(basePos, machine.Count)}");
                    basePos += machine.Count;
                    curPos = basePos;
                }
                else
                {
                    throw new Exception("非法字符");
                }
            }

        }

        class MachineComparer : IComparer<StateMachine>
        {
            int IComparer<StateMachine>.Compare(StateMachine? x, StateMachine? y)
            {
                return x.Count - y.Count;
            }
        }


        static void Main2(string[] args)
        {
            // a(b|c) *
            //var nfa = NfaDigraphCreater.CreateSingleCharacter('b') // b
            //    .Union(NfaDigraphCreater.CreateSingleCharacter('c')) // b|c
            //    .Closure(); // (b|c)*
            //nfa = NfaDigraphCreater.CreateSingleCharacter('a').Join(nfa); // a(b|c) *

            // fee | fie
            var nfa = NfaDigraphCreater.CreateSingleCharacter('f').Join(NfaDigraphCreater.CreateSingleCharacter('e')).Join(NfaDigraphCreater.CreateSingleCharacter('e'))
                .Union(NfaDigraphCreater.CreateSingleCharacter('f').Join(NfaDigraphCreater.CreateSingleCharacter('i')).Join(NfaDigraphCreater.CreateSingleCharacter('e'))
                );

            // [a-z]([a-z])*
            //var nfa = NfaDigraphCreater.CreateCharacterRange('a', 'z').Join(NfaDigraphCreater.CreateCharacterRange('a', 'z').Closure());

            // a|aa|aaa
            //var exp_a = NfaDigraphCreater.CreateSingleCharacter('a');
            //var exp_aa = exp_a.Join(exp_a);
            //var exp_aaa = exp_a.Join(exp_a).Join(exp_a);
            //var nfa = exp_a.Union(exp_aa).Union(exp_aaa);

            //n = [0-9]
            //nn*|nn*.|.nn*|nn*.nn*
            //var exp_n = nfadigraphcreater.createcharacterrange('0', '9');
            //var exp_dot = nfadigraphcreater.createsinglecharacter('.');
            //var exp_nns = exp_n.join(exp_n.closure());
            //var nfa = exp_nns.join(exp_dot).join(exp_nns);
            //nfa = nfa.union(exp_nns);
            //nfa = nfa.union(exp_nns.join(exp_dot));
            //nfa = nfa.union(exp_dot.join(nfa));

            NfaDigraphCreater.PrintDigraph(nfa);

            //Console.WriteLine("=============");
            //DfaDigraphNode dfa = DfaDigraphCreater.CreateFrom(nfa);
            //DfaDigraphCreater.PrintDigraph(dfa, "dfa", false);

            //Console.WriteLine("=============");
            //var dmin = dfa.Minimize();
            //DfaDigraphCreater.PrintDigraph(dmin, "dmin", false);
        }




    }

}
