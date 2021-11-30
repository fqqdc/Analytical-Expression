using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class HashSetEqualityComparer<T> : IEqualityComparer<HashSet<T>>
    {
        static HashSetEqualityComparer() => Default = new HashSetEqualityComparer<T>();
        public static HashSetEqualityComparer<T> Default { get; private set; }

        bool IEqualityComparer<HashSet<T>>.Equals(HashSet<T>? x, HashSet<T>? y)
        {
            if(x == null || y == null) 
                return false;
            return x.SetEquals(y);
        }

        int IEqualityComparer<HashSet<T>>.GetHashCode(HashSet<T> obj)
        {
            return 0;
        }
    }
}
