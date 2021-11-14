using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class NfaDigraphNode
    {
        static int number;

        public int ID { get; private set; }
        public NfaDigraphNode()
        {
            ID = Interlocked.Increment(ref number);
        }

        public HashSet<(int Value, NfaDigraphNode Node)> Edges { get; init; } = new();

        public override string ToString()
        {
            StringBuilder builder = new();
            builder.AppendLine($"\"nfa{ID}\"");
            foreach (var e in Edges)
            {
                builder.AppendLine($"  --({e.Value}[{ (e.Value >= 0 && e.Value <= 127 ? (char)e.Value : "??") }])-->\"nfa{e.Node.ID}\"");
            }

            return builder.ToString();
        }
    }
}
