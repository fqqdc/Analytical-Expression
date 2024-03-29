﻿using System.Text;

namespace LexicalAnalyzer
{
    public abstract record Symbol(string Name)
    {
        public override string ToString()
        {
            StringBuilder builder = new();
            foreach (var c in Name)
            {
                builder.Append(c.Escape());
            }
            return builder.ToString();
        }
    }
    public record Terminal(string Name) : Symbol(Name)
    {
        public static Terminal Epsilon { get; private set; } = new("\"eps\"");
        public static Terminal EndTerminal { get; private set; } = new("\"end\"");

        public override string ToString()
        {
            return base.ToString();
        }
    }
}