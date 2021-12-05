using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class TreeFA : FA
    {
        public TreeFA()
            : base(new int[] { 0 }, new Terminal[0], new (int, Terminal, int)[0], 0, new int[0])
        { }

        public HashSet<int> Union(FA fa)
        {
            //S
            int base_id = _S.Count;
            _S.UnionWith(fa.S.Select(s => base_id + s));
            var tail = _S.Count;
            if (fa.Z.Count() > 1)
                _S.Add(tail);

            //Sigma
            _Sigma.UnionWith(fa.Sigma);

            //Mapping
            _MappingTable.UnionWith(fa.MappingTable.Select(i => (base_id + i.s1, i.t, base_id + i.s2)));

            //Z
            var oldZ = fa.Z.Select(i => (base_id + i));
            var newZ = new HashSet<int>();
            if (oldZ.Count() > 1)
            {
                newZ.Add(tail);
                _Z.Add(tail);
            }
            else
            {
                newZ.UnionWith(oldZ);
                _Z.UnionWith(newZ);
            }

            //Union
            _MappingTable.Add((S_0, FA.EPSILON, base_id + fa.S_0));
            if (oldZ.Count() > 1)
            {
                foreach (var s in oldZ)
                {
                    _MappingTable.Add((s, FA.EPSILON, tail));
                }
            }

            return newZ.ToHashSet();
        }

        public NFA ToNFA()
        {
            return new(S, Sigma, MappingTable, S_0, Z);
        }
    }
}
