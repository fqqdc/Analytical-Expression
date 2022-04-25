using System.Text;

namespace SyntaxAnalyzer
{
    public abstract record Symbol(string Name)
    {
        public override string ToString()
        {
            return $"{Name}";
        }
    }
    public record Terminal(string Name) : Symbol(Name)
    {
        public static Terminal Epsilon { get; private set; } = new("\"_NUL_\"");
        public static Terminal EndTerminal { get; private set; } = new("\"_EOT_\"");

        public override string ToString()
        {
            return base.ToString();
        }
    }

    public record CharTerminal(char CharValue) : Terminal("char")
    {
        public override string ToString()
        {
            return $"{base.ToString()}[{CharValue}]";
        }
    }


    public record NonTerminal(string Name) : Symbol(Name)
    {
        public override string ToString()
        {
            return base.ToString();
        }
    }
}