#nullable disable

using ObservableCollections.Internal;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ObservableCollections;

public sealed class FreezedDictionary<TKey, TValue> :
    FreezedCollection<IReadOnlyDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    public FreezedDictionary(IReadOnlyDictionary<TKey, TValue> dictionary) : base(dictionary)
    {
    }

    public TValue this[TKey key] => Collection[key];

    public IEnumerable<TKey> Keys => Collection.Keys;

    public IEnumerable<TValue> Values => Collection.Values;

    public bool ContainsKey(TKey key)
    {
        return Collection.ContainsKey(key);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return Collection.TryGetValue(key, out value);
    }
}