using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxAnalyzer
{
    public class EnumerableComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        static EnumerableComparer() => Default = new();
        public static EnumerableComparer<T> Default { get; private set; }

        public int GetHashCode([DisallowNull] IEnumerable<T> obj)
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

        bool IEqualityComparer<IEnumerable<T>>.Equals(IEnumerable<T>? x, IEnumerable<T>? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            return x != null
                && y != null
                && x.SequenceEqual(y);
        }
    }
}
