using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexicalAnalyzer
{
    public class DFA : FA
    {
        public DFA(IEnumerable<State> S, IEnumerable<Symbol> Sigma, IEnumerable<(State s1, Symbol symbol, State s2)> MappingTable, State S_0, IEnumerable<State> Z)
            : base(S, Sigma, MappingTable, Z) { this.S_0 = S_0; }
        public State S_0 { get; private set; }

        protected override void S0ToString(StringBuilder builder)
        {
            builder.Append(PRE).Append($"S_0 : {S_0}").AppendLine();
        }

        private static HashSet<State> EpsilonClosure(IEnumerable<(State s1, Symbol symbol, State s2)> nfaMappingTable, State s)
        {
            HashSet<State> visited = new();
            HashSet<State> closure = new();
            Queue<State> queue = new();

            queue.Enqueue(s);
            while (queue.Count > 0)
            {
                var s1 = queue.Dequeue();
                closure.Add(s1);
                visited.Add(s1);

                foreach (var s2 in nfaMappingTable.Where(i => i.s1 == s1 && i.symbol == Symbol.Epsilon)
                    .Select(i => i.s2))
                {
                    if (!visited.Contains(s2))
                    {
                        queue.Enqueue(s2);
                    }
                }
            }
            return closure;
        }
        private static HashSet<State> EpsilonClosure(IEnumerable<(State s1, Symbol t, State s2)> nfaMappingTable, HashSet<State> S)
        {
            HashSet<State> newSet = new();

            foreach (var s in S)
            {
                HashSet<State> closureSet = EpsilonClosure(nfaMappingTable, s);
                newSet.UnionWith(closureSet);
            }

            return newSet;
        }
        public static DFA CreateFrom(NFA nfa)
        {
            var nfaMappingTable = nfa.MappingTable;
            if (nfaMappingTable.Any(i => i.symbol is NonTerminal))
                throw new NotImplementedException("未实现包含非终结符的NFA转换");

            //Mapping
            var MappingTable = new HashSet<(State s1, Symbol t, State s2)>();
            //Z
            var Z = new HashSet<int>();

            throw new Exception();
        }
    }

    public static class DFAHelper
    {
        
    }
}
