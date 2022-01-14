using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexicalAnalyzer
{
    public abstract record Edge
    {
        public abstract string Name { get; }
    }

    public sealed record EdgeNoright : Edge
    {
        public static Edge Instance { get; private set; }
        static EdgeNoright() => Instance = new EdgeNoright();

        public override string Name => "eps";

        public override string ToString()
        {
            return base.ToString();
        }

        private EdgeNoright() { }
    }

    public sealed record EdgeRight(char Value) : Edge
    {
        public override string Name => Value.ToString();

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
