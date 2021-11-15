using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Analytical_Expression
{
    public class DfaDigraphNode
    {
        static int number;

        public int ID { get; private set; }
        public DfaDigraphNode()
        {
            ID = Interlocked.Increment(ref number);
        }

        public HashSet<NfaDigraphNode> NfaElement { get; init; } = new();

        public HashSet<(HashSet<int> Value, DfaDigraphNode Node)> Edges { get; init; } = new();

        public override string ToString()
        {
            StringBuilder builder = new();
            builder.AppendLine($"\"dfa{ID} [{JoinNfaElement(NfaElement)}]\"");
            foreach (var e in Edges)
            {
                builder.AppendLine($"  --({JoinEdgeValue(e)})-->\"dfa{e.Node.ID}\"");
            }

            return builder.ToString();
        }

        private string JoinNfaElement(HashSet<NfaDigraphNode> elem)
        {
            StringBuilder builder = new();
            foreach (var n
                in elem)
            {
                if (builder.Length > 0)
                    builder.Append(",");
                builder.Append($"nfa{n.ID}");
            }

            return builder.ToString();
        }
        private string JoinEdgeValue((HashSet<int> Value, DfaDigraphNode Node) edge)
        {
            StringBuilder builder = new();
            foreach (var v in edge.Value)
            {
                if (builder.Length > 0)
                    builder.Append(",");
                builder.Append($"{v}[{ (v >= 0 && v <= 127 ? (char)v : "??") }]");
            }

            return builder.ToString();
        }
    }


}
