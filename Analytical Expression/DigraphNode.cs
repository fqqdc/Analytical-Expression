using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public record DigraphNode<TContent, TEdgeValue>
    {
        public TContent Content { get; set; }
        public HashSet<(TEdgeValue Value, DigraphNode<TContent, TEdgeValue> Node)> Edges { get; init; } = new();

        protected virtual bool PrintMembers(StringBuilder builder)
        {
            builder.Append($"{Content} =>");
            for (int i = 0; i < Edges.Count; i++)
            {
                var e = Edges.ElementAt(i);
                builder.Append($"{e.Value}--->{e.Node}");
            }

            return true;
        }
    }
}
