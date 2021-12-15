using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public static class LL1StateTable
    {
        public record MappingKey(Symbol z, Terminal a);

        public static Dictionary<MappingKey, IEnumerable<Symbol>> CreateFrom(Grammar grammar)
        {
            Dictionary<MappingKey, IEnumerable<Symbol>> dict = new();

            foreach (var p in grammar.P)
            {
                var firstSet = grammar.GetFirstSet(p.Right);
                foreach (var s in firstSet)
                {
                    if (s == Grammar.Epsilon) continue;
                    MappingKey key = new(p.Left, s);
                    if(dict.ContainsKey(key))
                        throw new Exception("Not an LL(1) grammar");
                    dict[key] = p.Right;
                        
                }
                if (firstSet.Contains(Grammar.Epsilon))
                {
                    var followSet = grammar.GetFollowSet(p.Left);
                    foreach (var s in followSet)
                    {
                        if (s == Grammar.Epsilon) continue;
                        MappingKey key = new(p.Left, s);
                        if (dict.ContainsKey(key))
                            throw new Exception("Not an LL(1) grammar");
                        dict[key] = p.Right;
                    }
                }
            }

            return dict;
        }
    }
}
