using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ObservableCollections.Internal;

namespace ObservableCollections;

// can not implements ISet<T> because set operation can not get added/removed values.
public sealed partial class ObservableHashSet<T> : SynchronizedCollection<HashSet<T>, T>, IReadOnlySet<T>
    where T : notnull
{
    public ObservableHashSet()
    {
        Source = new HashSet<T>();
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    public ObservableHashSet(int capacity)
    {
        Source = new HashSet<T>(capacity);
    }

#endif

    public ObservableHashSet(IEnumerable<T> collection)
    {
        Source = new HashSet<T>(collection);
    }

    public bool IsReadOnly => false;

    public event NotifyCollectionChangedEventHandler<T>? CollectionChanged;

    public bool Contains(T item)
    {
        lock (SyncRoot)
        {
            return Source.Contains(item);
        }
    }

    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        lock (SyncRoot)
        {
            return Source.IsProperSubsetOf(other);
        }
    }

    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        lock (SyncRoot)
        {
            return Source.IsProperSupersetOf(other);
        }
    }

    public bool IsSubsetOf(IEnumerable<T> other)
    {
        lock (SyncRoot)
        {
            return Source.IsSubsetOf(other);
        }
    }

    public bool IsSupersetOf(IEnumerable<T> other)
    {
        lock (SyncRoot)
        {
            return Source.IsSupersetOf(other);
        }
    }

    public bool Overlaps(IEnumerable<T> other)
    {
        lock (SyncRoot)
        {
            return Source.Overlaps(other);
        }
    }

    public bool SetEquals(IEnumerable<T> other)
    {
        lock (SyncRoot)
        {
            return Source.SetEquals(other);
        }
    }

    public bool Add(T item)
    {
        lock (SyncRoot)
        {
            if (Source.Add(item))
            {
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(item, -1));
                return true;
            }

            return false;
        }
    }

    public void AddRange(IEnumerable<T> items)
    {
        lock (SyncRoot)
        {
            if (!items.TryGetNonEnumeratedCount(out var capacity)) capacity = 4;

            using (var list = new ResizableArray<T>(capacity))
            {
                foreach (var item in items)
                    if (Source.Add(item))
                        list.Add(item);

                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(list.Span, -1));
            }
        }
    }

    public void AddRange(T[] items)
    {
        AddRange(items.AsSpan());
    }

    public void AddRange(ReadOnlySpan<T> items)
    {
        lock (SyncRoot)
        {
            using (var list = new ResizableArray<T>(items.Length))
            {
                foreach (var item in items)
                    if (Source.Add(item))
                        list.Add(item);

                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(list.Span, -1));
            }
        }
    }

    public bool Remove(T item)
    {
        lock (SyncRoot)
        {
            if (Source.Remove(item))
            {
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(item, -1));
                return true;
            }

            return false;
        }
    }

    public void RemoveRange(IEnumerable<T> items)
    {
        lock (SyncRoot)
        {
            if (!items.TryGetNonEnumeratedCount(out var capacity)) capacity = 4;

            using (var list = new ResizableArray<T>(capacity))
            {
                foreach (var item in items)
                    if (Source.Remove(item))
                        list.Add(item);

                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(list.Span, -1));
            }
        }
    }

    public void RemoveRange(T[] items)
    {
        RemoveRange(items.AsSpan());
    }

    public void RemoveRange(ReadOnlySpan<T> items)
    {
        lock (SyncRoot)
        {
            using (var list = new ResizableArray<T>(items.Length))
            {
                foreach (var item in items)
                    if (Source.Remove(item))
                        list.Add(item);

                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(list.Span, -1));
            }
        }
    }

    public void Clear()
    {
        lock (SyncRoot)
        {
            Source.Clear();
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Reset());
        }
    }

#if !NETSTANDARD2_0 && !NET_STANDARD_2_0 && !NET_4_6
    public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue)
    {
        lock (SyncRoot)
        {
            return Source.TryGetValue(equalValue, out actualValue);
        }
    }

#endif
}