using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexicalAnalyzer
{
    public sealed record State(int Id)
    {
        public Guid Guid { get; init; } = Guid.NewGuid();

        public State(State state, int Id) : this(Id)
        {
            this.Guid = state.Guid;
        }

        public override string ToString()
        {
            return Id.ToString(); 
        }

        public string ToFullString()
        {
            return $"{ToString()}[{Guid.ToString()}]";
        }
    }
}
