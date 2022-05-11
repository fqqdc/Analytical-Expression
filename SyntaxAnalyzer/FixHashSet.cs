using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SyntaxAnalyzer
{
    public class FixHashSet<T> : IReadOnlySet<T>
    {
        private HashSet<T> _set;
        private int _hashcode;

        public FixHashSet(IEnumerable<T> seq)
        {
            _set = seq.ToHashSet();
            _hashcode = CalcHashCode();
        }

        private int CalcHashCode()
        {
            var code = 0;
            foreach (var item in _set)
            {
                if (item == null)
                    continue;
                code = code ^ item.GetHashCode();
            }
            return code;
        }

        public override int GetHashCode()
        {
            return _hashcode;
        }

        #region IReadOnlySet<T>
        public int Count => _set.Count;

        public bool Contains(T item)
        {
            return _set.Contains(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _set.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _set.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _set.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _set.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return _set.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return _set.SetEquals(other);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _set.GetEnumerator();
        }
        #endregion
    }
}
