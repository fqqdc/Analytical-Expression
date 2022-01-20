using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LexicalAnalyzer
{
    public sealed record State(int Id)
    {
        private static int interId = 0;
        private static int NewInterId()
        {
            return Interlocked.Increment(ref interId);
        }
        public int InterId { get; init; } = State.NewInterId();

        public State(State state, int Id) : this(Id)
        {
            this.InterId = state.InterId;
        }

        public override string ToString()
        {
            return $"{Id}"; 
        }
    }
}
