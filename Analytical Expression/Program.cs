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

        static void Main(string[] args)
        {
            //// a(b|c) *
            //var nfa = NfaDigraphCreater.CreateSingleCharacter('b') // b
            //    .Union(NfaDigraphCreater.CreateSingleCharacter('c')) // b|c
            //    .Closure(); // (b|c)*
            //nfa = NfaDigraphCreater.CreateSingleCharacter('a').Join(nfa); // a(b|c) *

            //var nfa = NfaDigraphCreater.CreateSingleCharacter('a')
            //    .Join(NfaDigraphCreater.CreateSingleCharacter('b'))
            //    .Join(NfaDigraphCreater.CreateSingleCharacter('c'));

            //// fee|fie
            //var nfa = NfaDigraphCreater.CreateSingleCharacter('f').Join(NfaDigraphCreater.CreateSingleCharacter('e')).Join(NfaDigraphCreater.CreateSingleCharacter('e'))
            //    .Union(NfaDigraphCreater.CreateSingleCharacter('f').Join(NfaDigraphCreater.CreateSingleCharacter('i')).Join(NfaDigraphCreater.CreateSingleCharacter('e'))
            //    );

            ////ace | adf | bdf
            //var nfa = NfaDigraphCreater.CreateSingleCharacter('a').Join(NfaDigraphCreater.CreateSingleCharacter('c')).Join(NfaDigraphCreater.CreateSingleCharacter('e'))
            //    .Union(NfaDigraphCreater.CreateSingleCharacter('a').Join(NfaDigraphCreater.CreateSingleCharacter('d')).Join(NfaDigraphCreater.CreateSingleCharacter('f')))
            //    .Union(NfaDigraphCreater.CreateSingleCharacter('b').Join(NfaDigraphCreater.CreateSingleCharacter('d')).Join(NfaDigraphCreater.CreateSingleCharacter('f')));

            // [a-z]([a-z])*
            var nfa = NfaDigraphCreater.CreateCharacterRange('a','z').Join(NfaDigraphCreater.CreateCharacterRange('a', 'z').Closure());


            NfaDigraphCreater.PrintDigraph(nfa);

            Console.WriteLine("=============");

            DfaDigraphNode dfa = DfaDigraphCreater.CreateFrom(nfa);
            DfaDigraphCreater.PrintDigraph(dfa);

            Console.WriteLine("=============");

            var sets = SplitByTail(dfa, nfa.Tail);

            sets = Split(sets);
            foreach (var item in sets)
            {
                Console.WriteLine($"subSet : {ToString(item)}");
            }
        }

        static string ToString(HashSet<DfaDigraphNode> set)
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

        /// <summary>
        /// Hopcroft算法
        /// </summary>
        static HashSet<HashSet<DfaDigraphNode>> Split(HashSet<HashSet<DfaDigraphNode>> set)
        {
            bool haveSplited = false;
            set = new(set);
            do
            {
                haveSplited = false;
                foreach (var subSet in set.ToArray())
                {
                    set.Remove(subSet);
                    var newSet = SingleSplit(subSet);
                    if (newSet.Count > 1)
                        haveSplited = true;
                    set.UnionWith(newSet);
                }
            } while (haveSplited);

            return set;
        }
        static HashSet<HashSet<DfaDigraphNode>> SingleSplit(HashSet<DfaDigraphNode> set)
        {
            HashSet<HashSet<DfaDigraphNode>> sets = new();
            var chars = set.SelectMany(n => n.Edges).Select(e => e.Value).Distinct().ToArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (set.Count <= 1)
                    break;

                char c = (char)chars[i];
                HashSet<DfaDigraphNode> newSet = new(set.Where(n
                    => n.Edges.Any(e
                         =>
                     {
                         return e.Value == c
                         && !set.Contains(e.Node);
                     })));

                if (newSet.Count > 0)
                {
                    set = new(set.Except(newSet));
                    sets.Add(newSet);
                }
            }
            if (set.Count > 0)
                sets.Add(new(set));

            return sets;
        }

        static HashSet<HashSet<DfaDigraphNode>> SplitByTail(DfaDigraphNode digraph, NfaDigraphNode endNode)
        {
            HashSet<DfaDigraphNode> N = new(), A = new(), visited = new();
            Queue<DfaDigraphNode> queue = new();
            var node = digraph;
            queue.Enqueue(node);
            while (queue.Count > 0)
            {
                node = queue.Dequeue();

                visited.Add(node);
                if (node.NfaElement.Contains(endNode)) A.Add(node);
                else N.Add(node);

                foreach (var edge in node.Edges)
                {
                    if (!visited.Contains(edge.Node))
                        queue.Enqueue(edge.Node);
                }
            }

            return new() { N, A };
        }
    }



    class HashSetEqualityComparer<T> : IEqualityComparer<HashSet<T>>
    {
        bool IEqualityComparer<HashSet<T>>.Equals(HashSet<T>? x, HashSet<T>? y)
        {
            if (x == null || y == null) return false;
            return x.SetEquals(y);
        }

        int IEqualityComparer<HashSet<T>>.GetHashCode(HashSet<T> obj)
        {
            StringBuilder builder = new();
            foreach (var item in obj)
            {
                if (item == null) continue;

                builder.Append(item.GetHashCode());
            }
            return builder.ToString().GetHashCode();
        }
    }

}
