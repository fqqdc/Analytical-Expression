#define DEBUG_PRINT
using System.Text;


namespace Analytical_Expression
{
    public record Production(NonTerminal Left, Symbol[] Right, int Position = -1)
    {
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{ ");
            if (PrintMembers(builder))
            {
                builder.Append(" ");
            }
            builder.Append("}");
            return builder.ToString();
        }
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            builder.Append($"{Left} =>");
            for (int i = 0; i < Right.Length; i++)
            {
                var rChild = Right[i];
                builder.Append(" ");
                if (Position == i)
                    builder.Append($"[{rChild}]");
                else builder.Append($"{rChild}");
            }

            if (Position == Right.Length)
            {
                builder.Append($" []");
            }

            return true;
        }
    }
}