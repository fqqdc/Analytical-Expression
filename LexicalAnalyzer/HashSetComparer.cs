using System.Collections.Generic;

namespace LexicalAnalyzer
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
        int IEqualityComparer<HashSet<T>>.GetHashCode(HashSet<T> obj)
        {
            int code = 0;
            foreach (T item in obj)
            {
                if (item == null)
                    continue;
                code ^= item.GetHashCode();
            }
            return code;
        }
    }
}
