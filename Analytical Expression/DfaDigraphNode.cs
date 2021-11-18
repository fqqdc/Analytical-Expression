using System.Collections.Generic;
using System.Text;
using System.Linq;


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
            builder.AppendLine(JoinEdgeValue(Edges));

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
        private string JoinEdgeValue(Dictionary<int, DfaDigraphNode> edge)
        {
            StringBuilder builder = new();

            foreach (var g in edge.GroupBy(e => e.Value))
            {
                StringBuilder vbuilder = new();
                StringBuilder cbuilder = new();
                foreach (var v in g)
                {
                    if (vbuilder.Length > 0)
                    {
                        vbuilder.Append(",");
                        cbuilder.Append(",");
                    }
                    vbuilder.Append(v.Key);
                    char c = (char)v.Key;
                    cbuilder.Append((char.IsControl(c) ? "??" : c));
                }

                builder.AppendLine($"--({{{vbuilder.ToString()}}}[{cbuilder.ToString()}])-->{g.Key}");
            }

            return builder.ToString();
        }
    }


}
