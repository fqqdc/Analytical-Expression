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


        static void Main(string[] args)
        {
            NonTerminal n_N = new("N");
            NonTerminal o = new(n_N);

            Console.WriteLine(Equals(o, n_N));
            Console.WriteLine(ReferenceEquals(o, n_N));

            Console.WriteLine(ReferenceEquals(o.Productions, n_N.Productions));

            return;


            Terminal t_s = new("s");
            Terminal t_t = new("t");
            Terminal t_g = new("g");
            Terminal t_w = new("w");
            n_N.Productions.Add(new() { t_s });
            n_N.Productions.Add(new() { t_t });
            n_N.Productions.Add(new() { t_g });
            n_N.Productions.Add(new() { t_w });

            NonTerminal n_V = new("V");
            Terminal t_e = new("e");
            Terminal t_d = new("d");
            n_V.Productions.Add(new() { t_e });
            n_V.Productions.Add(new() { t_d });

            NonTerminal n_S = new("S");
            n_S.Productions.Add(new() { n_N, n_V, n_N });

            Console.WriteLine(n_S);
            Console.WriteLine(n_N);
            Console.WriteLine(n_V);
            Console.WriteLine(t_d);

            Token[] tokens = { t_t, t_d, t_w };
            int i = 0;
            int right_i = 0;
            Stack<Token> stack = new(new Token[] { n_S });
            while (stack.Count > 0)
            {
                if (stack.Peek() is Terminal t)
                {
                    if (t == tokens[i++])
                        stack.Pop();
                    else
                    {
                        BackTrack();
                    }
                }
                else if (stack.Peek() is NonTerminal T)
                {
                    stack.Pop();
                    var right = T.Productions[right_i++].AsEnumerable();
                    foreach (var item in right.Reverse())
                    {
                        stack.Push(item);
                    }
                }
            }
        }

        private static void BackTrack()
        {
            throw new NotImplementedException();
        }
    }

    abstract class Token
    {
        public string Name { get; init; }
        protected Token(string Name) => this.Name = Name;
        protected Token(Token original) : this(original.Name) { }
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(GetType().Name[0]);
            stringBuilder.Append(" { ");
            if (PrintMembers(stringBuilder))
            {
                stringBuilder.Append(" ");
            }
            stringBuilder.Append("}");
            return stringBuilder.ToString();
        }

        protected virtual bool PrintMembers(StringBuilder builder)
        {
            return false;
        }
    }

    class Terminal : Token
    {
        public Terminal(string Name) : base(Name) { }

        public override string ToString()
        {
            return base.ToString();
        }

        protected override bool PrintMembers(StringBuilder builder)
        {
            builder.Append($"{Name}");
            return true;
        }
    }
    class NonTerminal : Token
    {
        public NonTerminal(string Name) : base(Name) { }

        public NonTerminal(NonTerminal other) : base(other) { this.Productions = other.Productions.Select(p => p.ToList()).ToList(); }

        public List<List<Token>> Productions { get; init; } = new();

        public override string ToString()
        {
            return base.ToString();
        }

        protected override bool PrintMembers(StringBuilder builder)
        {
            builder.Append($"{Name}");

            for (int i = 0; i < Productions.Count; i++)
            {
                var pChildren = Productions[i];
                if (i == 0) builder.Append($", P = {{ ");

                if (pChildren == null) continue;
                for (int j = 0; j < pChildren.Count; j++)
                {
                    var child = pChildren[j];
                    if (j == 0) builder.Append($" {Name} => ");
                    builder.Append(child.Name);
                    if (j < pChildren.Count - 1) builder.Append($" ");
                }

                if (i < Productions.Count - 1) builder.Append($",");
                else builder.Append($" }}");
            }
            return true;
        }

    }
}