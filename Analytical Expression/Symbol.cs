#define DEBUG_PRINT


namespace Analytical_Expression
{
    public abstract record Symbol(string Name);
    public record Terminal(string Name) : Symbol(Name);
    public record NonTerminal(string Name) : Symbol(Name);
}