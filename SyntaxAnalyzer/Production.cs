using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace SyntaxAnalyzer
{
    public record Production
    {
        private Symbol[] right;

        public Production(NonTerminal left, IEnumerable<Symbol> right)
        {
            if (left == null) throw new ArgumentNullException("left");
            if (right == null) throw new ArgumentNullException("right");

            Left = left;
            this.right = right.ToArray();
        }

        public Production(NonTerminal left, Symbol singleSymbol) : this(left, new Symbol[] { singleSymbol }) { }

        public NonTerminal Left { get; init; }

        public IEnumerable<Symbol> Right
        {
            get { return right; }
        }

        public static Symbol[] Epsilon { get; private set; } = new Symbol[] { Terminal.Epsilon };

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
            foreach (var rChild in Right)
            {
                builder.Append($" {rChild}");
            }

            return true;
        }

        public virtual bool Equals(Production? other)
        {
            if (ReferenceEquals(this, other)) 
                return true;

            return other != null
                && Left.Equals(other.Left)
                && Right.SequenceEqual(other.Right);
        }

        public override int GetHashCode()
        {
            int value = Left.GetHashCode();
            foreach (var item in Right)
                value ^= item.GetHashCode();
            return value;
        }

        public static Production CreateSingle(string left, string right)
        {
            var sLeft = new NonTerminal(left);
            string[] itemsRight = right.Split(' ', StringSplitOptions.TrimEntries);

            if (itemsRight.Length == 1 && string.IsNullOrWhiteSpace(itemsRight[0]))
                return new Production(sLeft, Epsilon);

            var sRight = itemsRight.Select(item => char.IsUpper(item[0]) ? (Symbol)new NonTerminal(item) : (Symbol)new Terminal(item)).ToArray();
            return new Production(sLeft, sRight);
        }

        public static IEnumerable<Production> Create(string left, string right)
        {
            var itemsRight = right.Split('|', StringSplitOptions.TrimEntries);
            return itemsRight.Select(i => CreateSingle(left, i)).ToArray();
        }
    }
}