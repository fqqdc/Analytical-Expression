using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class LL1 : PDA
    {
        private LL1(IEnumerable<int> Q, IEnumerable<Terminal> Sigma, IEnumerable<Mapping> Delta, int q_0, Symbol z_0, IEnumerable<int> F)
            : base(Q, Sigma, Delta, q_0, z_0, F)
        {
        }

        public static LL1 CreateFrom(Grammar grammar)
        {
            int q_0 = 0;
            Symbol z_0 = grammar.S;
            HashSet<int> Q = new();
            Q.Add(q_0);
            HashSet<int> F = new();
            F.Add(q_0);

            var Sigam = grammar.P.SelectMany(p => p.Right).Where(s => s is Terminal terminal).Cast<Terminal>()
                .SkipWhile(s => s == Grammar.Epsilon).ToHashSet();
            HashSet<Mapping> Delta = new();

            foreach (var p in grammar.P)
            {
                var firstSet = grammar.GetFirstSet(p.Right);
                foreach (var s in firstSet)
                {
                    if (s == Grammar.Epsilon) continue;
                    var z = p.Left;
                    var a = s;
                    if (Delta.Any(i => i.z == z && i.a == a))
                        throw new Exception("Not an LL(1) grammar");
                    Delta.Add(new(q_0, z, a, q_0, p.Right.ToArray()));

                }
                if (firstSet.Contains(Grammar.Epsilon))
                {
                    var followSet = grammar.GetFollowSet(p.Left);
                    foreach (var s in followSet)
                    {
                        if (s == Grammar.Epsilon) continue;
                        var z = p.Left;
                        var a = s;
                        if (Delta.Any(i => i.z == z && i.a == a))
                            throw new Exception("Not an LL(1) grammar");
                        Delta.Add(new(q_0, z, a, q_0, p.Right.ToArray()));
                    }
                }
            }

            return new(Q, Sigam, Delta, q_0, z_0, F);
        }
    }

}
