using LexicalAnalyzer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxAnalyzer
{
    public record DoubleTerminal(Terminal First, Terminal Second) : IEnumerable<Terminal>
    {
        public static DoubleTerminal Epsilon { get; private set; } = new(Terminal.Epsilon, Terminal.Epsilon);
        public static DoubleTerminal EndTerminal { get; private set; } = new(Terminal.EndTerminal, Terminal.Epsilon);

        public IEnumerator<Terminal> GetEnumerator()
        {
            yield return First;
            yield return Second;
        }

        public override string ToString()
        {
            return $"({First}, {Second})";
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static class DoubleTerminalExtensions
    {
        public static HashSet<DoubleTerminal> Product(this HashSet<DoubleTerminal> fst, HashSet<DoubleTerminal> snd)
        {
            HashSet<DoubleTerminal> product = new();

            foreach (var fstDt in fst)
            {
                foreach (var sndDt in snd)
                {
                    var fstT = fstDt.TakeWhile(t => t != Terminal.Epsilon);
                    var sndT = sndDt.TakeWhile(t => t != Terminal.Epsilon);

                    var seqT = fstT.Union(sndT);

                    DoubleTerminal dt = new(seqT.ElementAtOrDefault(0) ?? Terminal.Epsilon, seqT.ElementAtOrDefault(1) ?? Terminal.Epsilon);

                    if (dt.First == Terminal.EndTerminal)
                        dt = dt with { Second = Terminal.Epsilon };

                    product.Add(dt);
                }
            }

            return product;
        }

        public static void ProductWith(this HashSet<DoubleTerminal> fst, HashSet<DoubleTerminal> snd)
        {
            HashSet<DoubleTerminal> product = new();

            foreach (var fstDt in fst)
            {
                foreach (var sndDt in snd)
                {
                    var fstT = fstDt.TakeWhile(t => t != Terminal.Epsilon);
                    var sndT = sndDt.TakeWhile(t => t != Terminal.Epsilon);

                    var seqT = fstT.Union(sndT);

                    DoubleTerminal dt = new(seqT.ElementAtOrDefault(0) ?? Terminal.Epsilon, seqT.ElementAtOrDefault(1) ?? Terminal.Epsilon);

                    if (dt.First == Terminal.EndTerminal)
                        dt = dt with { Second = Terminal.Epsilon };

                    product.Add(dt);
                }
            }

            fst.Clear();
            fst.UnionWith(product);
        }
    }
}
