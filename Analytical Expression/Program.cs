using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

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
            var dmin = dfa.Minimize();

            StateMachine sm = new(dmin);
            Console.WriteLine($"State:{sm.State} A:{sm.Acceptable}");
            foreach (var c in "12.")
            {                
                bool isError = sm.Jump(c);
                Console.WriteLine($"State:{sm.State} Act:{sm.Acceptable} JUMP:{isError}");
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

            Console.WriteLine("=============");
            DfaDigraphNode dfa = DfaDigraphCreater.CreateFrom(nfa);
            DfaDigraphCreater.PrintDigraph(dfa, "dfa", false);

            Console.WriteLine("=============");
            var dmin = dfa.Minimize();
            DfaDigraphCreater.PrintDigraph(dmin, "dmin", false);
        }




    }

}
