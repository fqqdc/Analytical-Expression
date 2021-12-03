using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analytical_Expression
{
    public class HashSetComparer<T> : IEqualityComparer<HashSet<T>>
    {
        static HashSetComparer() => Default = new();
        public static HashSetComparer<T> Default { get; private set; }
        bool IEqualityComparer<HashSet<T>>.Equals(HashSet<T>? x, HashSet<T>? y)
        {
            if (x == null || y == null)
                return false;
            return x.SetEquals(y);
        }
        int IEqualityComparer<HashSet<T>>.GetHashCode(HashSet<T> obj) => 0;
    }

    public class EnumerableComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        static EnumerableComparer() => Default = new();
        public static HashSetComparer<T> Default { get; private set; }
        bool IEqualityComparer<IEnumerable<T>>.Equals(IEnumerable<T>? x, IEnumerable<T>? y)
        {
            return x.Except(y).Count() == 0 && y.Except(x).Count() == 0;
        }
        int IEqualityComparer<IEnumerable<T>>.GetHashCode(IEnumerable<T> obj) => 0;
    }
}
