using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace SyntaxAnalyzer
{
    public record ProductionItem
    {
        public ProductionItem(Production Production, int Position)
        {
            if (Production == null) throw new ArgumentNullException("Production");
            if (Position < 0 || Position > Production.Right.Count()) throw new NotSupportedException("Position");

            this.Production = Production;
            this.Position = Position;
        }

        public Production Production { get; init; }

        public int Position { get; init; }

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
            builder.Append($"{Production.Left} =>");
            int i = 0;
            bool posAtEnd = true;
            foreach (var symbol in Production.Right)
            {
                builder.Append(" ");
                if (Position == i)
                {
                    builder.Append($"[{symbol}]");
                    posAtEnd = false;
                }
                else builder.Append($"{symbol}");
                i++;
            }

            if (posAtEnd)
            {
                builder.Append($" []");
            }

            return true;
        }

        public virtual bool Equals(ProductionItem? other)
        {
            return other != null
                && Position == other.Position
                && Production.Equals(other.Production);
        }

        public override int GetHashCode()
        {
            int value = Production.GetHashCode();
            return value ^ Position;
        }

        public static IEnumerable<ProductionItem> CreateSet(Production p)
        {
            var length = p.Right.Count();
            List<ProductionItem> list = new();
            for (int i = 0; i <= length; i++)
            {
                list.Add(new ProductionItem(p, i));
            }
            return list;
        }
    }
}