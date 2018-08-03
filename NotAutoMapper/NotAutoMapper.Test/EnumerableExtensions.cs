using System;
using System.Collections.Generic;

namespace NotAutoMapper.Test
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            var enumerator = collection.GetEnumerator();

            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;

                if (!predicate(enumerator.Current))
                    yield break;
            }
        }
    }
}
