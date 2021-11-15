using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class Program
    {
        static void Main(string[] args)
        {
            // a(b|c)*
            //var nfa = NfaDigraphCreater.CreateSingleCharacter('b') // b
            //    .Union(NfaDigraphCreater.CreateSingleCharacter('c')) // b|c
            //    .Closure(); // (b|c)*
            //nfa = NfaDigraphCreater.CreateSingleCharacter('a').Join(nfa); // a(b|c)*

            var nfa = NfaDigraphCreater.CreateSingleCharacter('a').Join(NfaDigraphCreater.CreateSingleCharacter('b'));

            NfaDigraphCreater.PrintDigraph(nfa);

            Console.WriteLine("=============");

            DfaDigraphNode dfa = DfaDigraphCreater.CreateFrom(nfa);
            DfaDigraphCreater.PrintDigraph(dfa);
        }



        





    }


}
