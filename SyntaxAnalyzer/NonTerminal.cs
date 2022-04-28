using LexicalAnalyzer;
using System.Text;

namespace SyntaxAnalyzer
{
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