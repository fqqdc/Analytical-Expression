using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using LexicalAnalyzer;

namespace SyntaxAnalyzer
{
    public record ProductionItem_1
    {
        public ProductionItem_1(Production Production, int Position, Terminal Follow)
        {
            if (Production == null) throw new ArgumentNullException("Production");
            if (Position < 0 || Position > Production.Right.Count()) throw new NotSupportedException("Position");

            this.Production = Production;
            this.Position = Position;
            this.Follow = Follow;
        }

        public Production Production { get; init; }

        public int Position { get; init; }

        public Terminal Follow { get; init; }

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

            if(Follow == Terminal.EndTerminal)
                builder.Append($", #");
            else builder.Append($", {this.Follow}");

            return true;
        }

        public virtual bool Equals(ProductionItem_1? other)
        {
            return other != null
                && Position == other.Position
                && Production.Equals(other.Production)
                && Follow.Equals(other.Follow);
        }

        public override int GetHashCode()
        {
            int value = Production.GetHashCode();
            value = value ^ Position;
            value = value ^ Follow.GetHashCode();
            return value;
        }
    }
}