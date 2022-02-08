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
        public static Terminal Epsilon { get; private set; } = new("\"eps\"");
        public static Terminal EndTerminal { get; private set; } = new("\"end\"");

        public override string ToString()
        {
            return base.ToString();
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