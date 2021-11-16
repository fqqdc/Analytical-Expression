using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class NfaDigraph
    {
        internal NfaDigraph() { }

        public NfaDigraphNode Head { get; init; }
        public NfaDigraphNode Tail { get; init; }
    }
}
