using System.Collections.Generic;
using System.Text;

namespace Analytical_Expression
{
    public class HashSetEqualityComparer<T> : IEqualityComparer<HashSet<T>>
    {
        bool IEqualityComparer<HashSet<T>>.Equals(HashSet<T>? x, HashSet<T>? y)
        {
            if (x == null || y == null) return false;
            return x.SetEquals(y);
        }

        int IEqualityComparer<HashSet<T>>.GetHashCode(HashSet<T> obj)
        {
            StringBuilder builder = new();
            foreach (var item in obj)
            {
                if (item == null) continue;

                builder.Append(item.GetHashCode());
            }
            return builder.ToString().GetHashCode();
        }
    }

}
