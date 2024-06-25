#nullable disable

namespace System.Collections.Generic;

internal static class CollectionExtensions
{
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
    {
        key = kvp.Key;
        value = kvp.Value;
    }

    public static bool Remove<TKey, TValue>(this SortedDictionary<TKey, TValue> dict, TKey key, out TValue value)
    {
        return dict.TryGetValue(key, out value) && dict.Remove(key);
    }

    public static bool Remove<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, out TValue value)
    {
        return dict.TryGetValue(key, out value) && dict.Remove(key);
    }

#if !NET6_0_OR_GREATER

    public static bool TryGetNonEnumeratedCount<T>(this IEnumerable<T> source, out int count)
    {
        if (source is ICollection<T> collection)
        {
            count = collection.Count;
            return true;
        }

        if (source is IReadOnlyCollection<T> rCollection)
        {
            count = rCollection.Count;
            return true;
        }

        count = 0;
        return false;
    }

#endif
}

#if !NET5_0_OR_GREATER

internal interface IReadOnlySet<out T> : IReadOnlyCollection<T>
{
}

#endif