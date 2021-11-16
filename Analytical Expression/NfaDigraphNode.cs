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
        public int ID { get; set; }

        public HashSet<(int Value, NfaDigraphNode Node)> Edges { get; init; }

        public string PrintString(bool showCode)
        {
            StringBuilder builder = new();
            builder.AppendLine($"\"nfa{ID}\" {(showCode ? "{ " + GetHashCode() + " }" : "")}");
            foreach (var e in Edges)
            {
                builder.AppendLine($"  --({e.Value}[{ (e.Value >= 0 && e.Value <= 127 ? (char)e.Value : "??") }])-->\"nfa{e.Node.ID}\"");
            }

            return builder.ToString();
        }

        public NfaDigraphNode()
        {
            Edges = new();
        }
    }
}
