﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace SyntaxAnalyzer
{
    public class FixHashSetComparer<T> : IEqualityComparer<FixHashSet<T>>
    {
        static FixHashSetComparer() => Default = new();
        public static FixHashSetComparer<T> Default { get; private set; }
        bool IEqualityComparer<FixHashSet<T>>.Equals(FixHashSet<T>? x, FixHashSet<T>? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            return x != null
                && y != null
                && x.SetEquals(y);
        }
        int IEqualityComparer<FixHashSet<T>>.GetHashCode(FixHashSet<T> obj)
        {
            return obj.GetHashCode();
        }
    }

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

    public class HashSetComparer<SET, OBJ> : IEqualityComparer<(HashSet<SET>, OBJ)>
    {
        static HashSetComparer() => Default = new();
        public static HashSetComparer<SET, OBJ> Default { get; private set; }

        bool IEqualityComparer<(HashSet<SET>, OBJ)>.Equals((HashSet<SET>, OBJ) x, (HashSet<SET>, OBJ) y)
        {
            return x.Item2 != null
                && x.Item2.Equals(y.Item2)
                && x.Item1.SetEquals(y.Item1);
        }

        int IEqualityComparer<(HashSet<SET>, OBJ)>.GetHashCode((HashSet<SET>, OBJ) obj)
        {
            var code = 0;
            if (obj.Item2 != null)
                code = code ^ obj.Item2.GetHashCode();
            foreach (var item in obj.Item1)
            {
                if (item == null)
                    continue;
                code = code ^ item.GetHashCode();
            }
            return code;
        }
    }

    public class FixHashSetComparer<TItem, TObj> : IEqualityComparer<(FixHashSet<TItem>, TObj)>
    {
        static FixHashSetComparer() => Default = new();
        public static FixHashSetComparer<TItem, TObj> Default { get; private set; }

        bool IEqualityComparer<(FixHashSet<TItem>, TObj)>.Equals((FixHashSet<TItem>, TObj) x, (FixHashSet<TItem>, TObj) y)
        {
            return x.Item2 != null
                && x.Item2.Equals(y.Item2)
                && x.Item1.SetEquals(y.Item1);
        }

        int IEqualityComparer<(FixHashSet<TItem>, TObj)>.GetHashCode((FixHashSet<TItem>, TObj) obj)
        {
            var code = 0;
            if (obj.Item2 != null)
            {
                code = code ^ obj.Item1.GetHashCode();
                code = code ^ obj.Item2.GetHashCode();
            }
            return code;
        }
    }
}
