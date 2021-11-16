using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class Program
    {
        static void Main2(string[] args)
        {


        }

        static NfaDigraph Clone(NfaDigraph old, int index)
        {
            Dictionary<NfaDigraphNode, NfaDigraphNode> tableOld2New = new();

            Queue<NfaDigraphNode> queue = new();
            queue.Enqueue(old.Head);
            while (queue.Count > 0)
            {
                var oldNode = queue.Dequeue();
                if (!tableOld2New.TryGetValue(oldNode, out NfaDigraphNode newNode))
                {
                    newNode = new() { ID = index++ };
                    tableOld2New[oldNode] = newNode;

                    foreach (var n in oldNode.Edges.Select(e => e.Node).Distinct())
                    {
                        queue.Enqueue(n);
                    }
                }
            }

            queue = new();
            queue.Enqueue(old.Head);
            HashSet<NfaDigraphNode> visited = new();
            while (queue.Count > 0)
            {
                var oldNode = queue.Dequeue();
                visited.Add(oldNode);

                foreach (var (value, oldOpNode) in oldNode.Edges)
                {
                    var newNode = tableOld2New[oldNode];
                    newNode.Edges.Add((value, tableOld2New[oldOpNode]));

                    if (!visited.Contains(oldOpNode))
                    {
                        queue.Enqueue(oldOpNode);
                    }
                }
            }

            return new() { Head = tableOld2New[old.Head], Tail = tableOld2New[old.Tail] };
        }

        static void Main(string[] args)
        {
            // a(b|c) *
            //var nfa = NfaDigraphCreater.CreateSingleCharacter('b') // b
            //    .Union(NfaDigraphCreater.CreateSingleCharacter('c')) // b|c
            //    .Closure(); // (b|c)*
            //nfa = NfaDigraphCreater.CreateSingleCharacter('a').Join(nfa); // a(b|c) *

            // ab
            //var nfa = NfaDigraphCreater.CreateSingleCharacter('a')
            //   .Join(NfaDigraphCreater.CreateSingleCharacter('b'));

            //// fee | fie
            //var nfa = NfaDigraphCreater.CreateSingleCharacter('f').Join(NfaDigraphCreater.CreateSingleCharacter('e')).Join(NfaDigraphCreater.CreateSingleCharacter('e'))
            //    .Union(NfaDigraphCreater.CreateSingleCharacter('f').Join(NfaDigraphCreater.CreateSingleCharacter('i')).Join(NfaDigraphCreater.CreateSingleCharacter('e'))
            //    );

            ////ace | adf | bdf
            //var nfa = NfaDigraphCreater.CreateSingleCharacter('a').Join(NfaDigraphCreater.CreateSingleCharacter('c')).Join(NfaDigraphCreater.CreateSingleCharacter('e'))
            //    .Union(NfaDigraphCreater.CreateSingleCharacter('a').Join(NfaDigraphCreater.CreateSingleCharacter('d')).Join(NfaDigraphCreater.CreateSingleCharacter('f')))
            //    .Union(NfaDigraphCreater.CreateSingleCharacter('b').Join(NfaDigraphCreater.CreateSingleCharacter('d')).Join(NfaDigraphCreater.CreateSingleCharacter('f')));

            //// [a-z]([a-z])*
            //var nfa = NfaDigraphCreater.CreateCharacterRange('a', 'z').Join(NfaDigraphCreater.CreateCharacterRange('a', 'z').Closure());

            var a = NfaDigraphCreater.CreateSingleCharacter('a');
            var b = NfaDigraphCreater.CreateSingleCharacter('b');
            var nfa = a.Join(b);
            nfa = nfa.Join(nfa);

            NfaDigraphCreater.PrintDigraph(a);
            Console.WriteLine("=============");
            NfaDigraphCreater.PrintDigraph(b);
            Console.WriteLine("=============");
            NfaDigraphCreater.PrintDigraph(nfa);

            //Console.WriteLine("=============");

            //DfaDigraphNode dfa = DfaDigraphCreater.CreateFrom(nfa);
            //DfaDigraphCreater.PrintDigraph(dfa, "dfa", true);

            //Console.WriteLine("=============");

            //var dmin = dfa.Minimize(nfa.Head, nfa.Tail);
            //DfaDigraphCreater.PrintDigraph(dmin, "dmin", false);
        }





        static string NodeSetPrintString(HashSet<DfaDigraphNode> set)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{ ");
            foreach (DfaDigraphNode node in set)
            {
                builder.Append($"\"dfa{node.ID}\" ");
            }
            builder.Append("}");

            return builder.ToString();
        }
    }

}
