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
        static void LexicalAnalyzer_Analyze()
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
            DfaDigraphCreater.PrintDigraph(dfaNumber, false);
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
            DfaDigraphCreater.PrintDigraph(dfaId, false);
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
            DfaDigraphCreater.PrintDigraph(dfaSymbol, false);
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

            LexicalAnalyzer analyzer = new(listSM);
            string txt = " 2 *(  3+(4-5) ) / 666>= ccc233  ";
            analyzer.Analyze(txt); ;




        }

        static void NFa_Dfa()
        {
            // a(b|c) *
            var nfa = NfaDigraphCreater.CreateSingleCharacter('b') // b
                .Union(NfaDigraphCreater.CreateSingleCharacter('c')) // b|c
                .Closure(); // (b|c)*
            nfa = NfaDigraphCreater.CreateSingleCharacter('a').Join(nfa); // a(b|c) *

            // fee | fie
            //var nfa = NfaDigraphCreater.CreateSingleCharacter('f').Join(NfaDigraphCreater.CreateSingleCharacter('e')).Join(NfaDigraphCreater.CreateSingleCharacter('e'))
            //    .Union(NfaDigraphCreater.CreateSingleCharacter('f').Join(NfaDigraphCreater.CreateSingleCharacter('i')).Join(NfaDigraphCreater.CreateSingleCharacter('e'))
            //    );

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
            DfaDigraphCreater.PrintDigraph(dfa, false);

            Console.WriteLine("=============");
            var dmin = dfa.Minimize();
            DfaDigraphCreater.PrintDigraph(dmin, false);
        }


        static List<Production> Productions = new();
        static void Main(string[] args)
        {
            Productions.Add(new("S", new string[] { "N", "V", "N" }));
            Productions.Add(new("N", new string[] { "s" }));
            Productions.Add(new("N", new string[] { "t" }));
            Productions.Add(new("N", new string[] { "g" }));
            Productions.Add(new("N", new string[] { "w" }));
            Productions.Add(new("V", new string[] { "e" }));
            Productions.Add(new("V", new string[] { "d" }));

            string[] tokens = { "w", "d", "w" };
            int indexTokens = 0;
            if (TryMatch(tokens, ref indexTokens, "S"))
                Console.WriteLine("ok");
            else Console.WriteLine("error");
        }

        static bool TryMatch(string[] tokens, ref int indexTokens, string token)
        {
            if (!char.IsUpper(token[0]))
            {
                if (token == tokens[indexTokens])
                {
                    indexTokens += 1;
                    return true;
                }
                else return false;
            }
            else
            {
                var productions = Productions.Where(p => p.Left == token).ToList();
                var r = false;
                foreach (var production in productions)
                {
                    foreach (var _token in production.Right)
                    {
                        int oldIndexTokens = indexTokens;
                        r = TryMatch(tokens, ref indexTokens, _token);
                        if (!r)
                        {
                            indexTokens = oldIndexTokens;
                            break;
                        }
                    }
                    if (r) return true;
                }
            }
            return false;
        }
    }

    record Production(string Left, string[] Right)
    {
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            builder.Append($"{Left} =>");

            for (int i = 0; i < Right.Length; i++)
            {
                var rChild = Right[i];
                builder.Append($" {rChild}");
            }
            return true;
        }
    }
}