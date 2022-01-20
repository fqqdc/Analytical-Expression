using System.Text;

namespace LexicalAnalyzer
{
    public abstract record Symbol(string Name)
    {
        public static Symbol Epsilon { get; private set; }
        static Symbol() => Epsilon = new Terminal("eps");

        public static implicit operator Symbol(char c)
        {
            if (char.IsUpper(c))
                return new NonTerminal(c.ToString());
            else
                return new Terminal(c.ToString());
        }
    }
    public record Terminal(string Name) : Symbol(Name)
    {
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"{Name}");
            return stringBuilder.ToString();
        }
    }
    public record NonTerminal(string Name) : Symbol(Name)
    {
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"{Name}");
            return stringBuilder.ToString();
        }
    }
}