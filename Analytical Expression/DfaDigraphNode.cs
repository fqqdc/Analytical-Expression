using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Analytical_Expression
{
    public class DfaDigraphNode
    {
        public int ID { get; private set; }
        public DfaDigraphNode(int id)
        {
            ID = id;
        }
        public DfaNodeType Type { get; init; }

        public HashSet<NfaDigraphNode> NfaElement { get; init; } = new();

        public Dictionary<int, DfaDigraphNode> Edges { get; init; } = new();

        public override string ToString()
        {
            return $"Dfa-{ID}";
        }

        public string PrintString(string pre, bool showNfa)
        {
            StringBuilder builder = new();
            string flagS = "", flagA = "";
            if ((Type & DfaNodeType.Start) == DfaNodeType.Start) flagS = "[S]";
            if ((Type & DfaNodeType.Acceptable) == DfaNodeType.Acceptable) flagA = "[A]";

            builder.AppendLine($"\"{pre}{ID}\"  { (showNfa ? "{ " + JoinNfaElement(NfaElement) + " }" : string.Empty)} {flagS} {flagA}");
            foreach (var (value, node) in Edges)
            {
                builder.AppendLine($"  --({value}[{ (value >= 0 && value <= 127 ? (char)value : "??") }])-->\"{pre}{node.ID}\"");
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
