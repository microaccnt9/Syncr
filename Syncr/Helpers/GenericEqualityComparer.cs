using System.Collections.Generic;
using System;

namespace Syncr.Helpers
{
    internal class GenericEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, object> selector;

        public GenericEqualityComparer(Func<T, object> selector)
        {
            this.selector = selector;
        }

        public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y) || x != null && y != null && selector(x).Equals(selector(y));
        }

        public int GetHashCode(T obj)
        {
            return selector(obj).GetHashCode();
        }
    }
}
