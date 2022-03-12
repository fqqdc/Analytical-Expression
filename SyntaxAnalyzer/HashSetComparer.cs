using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxAnalyzer
{
    public class HashSetComparer<T> : IEqualityComparer<HashSet<T>>
    {
        static HashSetComparer() => Default = new();
        public static HashSetComparer<T> Default { get; private set; }
        bool IEqualityComparer<HashSet<T>>.Equals(HashSet<T>? x, HashSet<T>? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            return x != null
                && y != null
                && x.SetEquals(y);
        }
        int IEqualityComparer<HashSet<T>>.GetHashCode(HashSet<T> obj)
        {
            var code = 0;
            foreach (var item in obj)
            {
                if (item == null)
                    continue;
                code = code ^ item.GetHashCode();
            }
            return code;
        }
    }

    public class HashSetComparer<T, X> : IEqualityComparer<(HashSet<T>, X)>
    {
        static HashSetComparer() => Default = new();
        public static HashSetComparer<T, X> Default { get; private set; }

        bool IEqualityComparer<(HashSet<T>, X)>.Equals((HashSet<T>, X) x, (HashSet<T>, X) y)
        {

            return x.Item2 != null
                && x.Item2.Equals(y.Item2)
                && x.Item1.SetEquals(y.Item1);
        }

        int IEqualityComparer<(HashSet<T>, X)>.GetHashCode((HashSet<T>, X) obj)
        {
            var code = 0;
            foreach (var item in obj.Item1)
            {
                if (item == null)
                    continue;
                code = code ^ item.GetHashCode();
            }
            if (obj.Item2 != null)
                code = code ^ obj.Item2.GetHashCode();
            return code;
        }
    }
}
