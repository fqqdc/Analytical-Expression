using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace SyntaxAnalyzer
{
    public record Production(NonTerminal Left, IEnumerable<Symbol>Right, int Position = -1)
    {
        public static Symbol[] Epsilon { get; private set; }  = new Symbol[] { Terminal.Epsilon };

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

        public static implicit operator Production((string left, string right) r)
        {
            return Production.Create(r.left, r.right);
        }

        public static Production Create(string left, string right)
        {
            var sLeft = new NonTerminal(left);
            var strRight = right.Split(' ', StringSplitOptions.TrimEntries);
            var sRight = strRight.Select(str => str.Length > 0 && char.IsUpper(str[0]) ? (Symbol)new NonTerminal(str) : (Symbol)new Terminal(str)).ToArray();
            if (sRight.Length == 1 && sRight[0].Name == String.Empty)
                sRight = Epsilon;
            return new Production(sLeft, sRight);
        }
    }
}