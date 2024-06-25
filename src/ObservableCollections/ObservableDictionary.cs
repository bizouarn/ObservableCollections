using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ObservableCollections;

public sealed partial class ObservableDictionary<TKey, TValue> : SynchronizedCollection<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>,
    IDictionary<TKey, TValue>,
    IReadOnlyObservableDictionary<TKey, TValue>
    where TKey : notnull
{
    public ObservableDictionary()
    {
        Source = new Dictionary<TKey, TValue>();
    }

    public ObservableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
    {
#if NET6_0_OR_GREATER
        Source = new Dictionary<TKey, TValue>(collection);
#else
        Source = new Dictionary<TKey, TValue>();
        foreach (var item in collection) Source.Add(item.Key, item.Value);
#endif
    }

    public event NotifyCollectionChangedEventHandler<KeyValuePair<TKey, TValue>>? CollectionChanged;

    public TValue this[TKey key]
    {
        get
        {
            lock (SyncRoot)
            {
                return Source[key];
            }
        }
        set
        {
            lock (SyncRoot)
            {
                if (Source.TryGetValue(key, out var oldValue))
                {
                    Source[key] = value;
                    CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Replace(
                        new KeyValuePair<TKey, TValue>(key, value),
                        new KeyValuePair<TKey, TValue>(key, oldValue!),
                        -1, -1));
                }
                else
                {
                    Add(key, value);
                }
            }
        }
    }

    // for lock synchronization, hide keys and values.
    ICollection<TKey> IDictionary<TKey, TValue>.Keys
    {
        get
        {
            lock (SyncRoot)
            {
                return Source.Keys;
            }
        }
    }

    ICollection<TValue> IDictionary<TKey, TValue>.Values
    {
        get
        {
            lock (SyncRoot)
            {
                return Source.Values;
            }
        }
    }

    public bool IsReadOnly => false;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
    {
        get
        {
            lock (SyncRoot)
            {
                return Source.Keys;
            }
        }
    }

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
    {
        get
        {
            lock (SyncRoot)
            {
                return Source.Values;
            }
        }
    }

    public void Add(TKey key, TValue value)
    {
        lock (SyncRoot)
        {
            Source.Add(key, value);
            CollectionChanged?.Invoke(
                NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Add(
                    new KeyValuePair<TKey, TValue>(key, value), -1));
        }
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    public bool TryAdd(TKey key, TValue value)
    {
        lock (SyncRoot)
        {
            if (Source.TryAdd(key, value))
            {
                CollectionChanged?.Invoke(
                    NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Add(
                        new KeyValuePair<TKey, TValue>(key, value), -1));
                return true;
            }

            return false;
        }
    }
#endif

    public void Clear()
    {
        lock (SyncRoot)
        {
            Source.Clear();
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Reset());
        }
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        lock (SyncRoot)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>) Source).Contains(item);
        }
    }

    public bool ContainsKey(TKey key)
    {
        lock (SyncRoot)
        {
            return ((IDictionary<TKey, TValue>) Source).ContainsKey(key);
        }
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        lock (SyncRoot)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>) Source).CopyTo(array, arrayIndex);
        }
    }

    public bool Remove(TKey key)
    {
        lock (SyncRoot)
        {
            if (Source.Remove(key, out var value))
            {
                CollectionChanged?.Invoke(
                    NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Remove(
                        new KeyValuePair<TKey, TValue>(key, value), -1));
                return true;
            }

            return false;
        }
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        lock (SyncRoot)
        {
            if (Source.TryGetValue(item.Key, out var value)
                && EqualityComparer<TValue>.Default.Equals(value, item.Value)
                && Source.Remove(item.Key, out var value2))
            {
                CollectionChanged?.Invoke(
                    NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Remove(
                        new KeyValuePair<TKey, TValue>(item.Key, value2), -1));
                return true;
            }

            return false;
        }
    }

#pragma warning disable CS8767
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767
    {
        lock (SyncRoot)
        {
            return Source.TryGetValue(key, out value);
        }
    }
}