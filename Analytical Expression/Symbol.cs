using System.Text;

namespace Analytical_Expression
{
    public abstract record Symbol(string Name)
    {
    }
    public record Terminal(string Name) : Symbol(Name)
    {
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"{Name}");
            return stringBuilder.ToString();
        }

        public static implicit operator Terminal(string Name)
        {
            return new(Name);
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