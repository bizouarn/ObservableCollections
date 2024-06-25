using ObservableCollections.Internal;
using System;
using System.Collections.Generic;

namespace ObservableCollections;

public sealed partial class ObservableRingBuffer<T> : SynchronizedCollection<T, RingBuffer<T>>, IList<T>, IReadOnlyList<T>,
    IObservableCollection<T>
{
    public event NotifyCollectionChangedEventHandler<T>? CollectionChanged;

    public ObservableRingBuffer()
    {
        Source = new RingBuffer<T>();
    }

    public ObservableRingBuffer(IEnumerable<T> collection)
    {
        Source = new RingBuffer<T>(collection);
    }

    public bool IsReadOnly => false;

    public T this[int index]
    {
        get
        {
            lock (SyncRoot)
            {
                return Source[index];
            }
        }
        set
        {
            lock (SyncRoot)
            {
                var oldValue = Source[index];
                Source[index] = value;
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Replace(value, oldValue, index, index));
            }
        }
    }

    public void AddFirst(T item)
    {
        lock (SyncRoot)
        {
            Source.AddFirst(item);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(item, 0));
        }
    }

    public void AddLast(T item)
    {
        lock (SyncRoot)
        {
            Source.AddLast(item);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(item, Source.Count - 1));
        }
    }

    public T RemoveFirst()
    {
        lock (SyncRoot)
        {
            var item = Source.RemoveFirst();
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(item, 0));
            return item;
        }
    }

    public T RemoveLast()
    {
        lock (SyncRoot)
        {
            var index = Source.Count - 1;
            var item = Source.RemoveLast();
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(item, index));
            return item;
        }
    }

    // AddFirstRange is not exists.

    public void AddLastRange(IEnumerable<T> items)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            using (var xs = new CloneCollection<T>(items))
            {
                foreach (var item in xs.Span) Source.AddLast(item);
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(xs.Span, index));
            }
        }
    }

    public void AddLastRange(T[] items)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            foreach (var item in items) Source.AddLast(item);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(items, index));
        }
    }

    public void AddLastRange(ReadOnlySpan<T> items)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            foreach (var item in items) Source.AddLast(item);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(items, index));
        }
    }

    public int IndexOf(T item)
    {
        lock (SyncRoot)
        {
            return Source.IndexOf(item);
        }
    }

    void IList<T>.Insert(int index, T item)
    {
        throw new NotSupportedException();
    }

    bool ICollection<T>.Remove(T item)
    {
        throw new NotSupportedException();
    }

    void IList<T>.RemoveAt(int index)
    {
        throw new NotSupportedException();
    }

    void ICollection<T>.Add(T item)
    {
        AddLast(item);
    }

    public void Clear()
    {
        lock (SyncRoot)
        {
            Source.Clear();
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Reset());
        }
    }

    public bool Contains(T item)
    {
        lock (SyncRoot)
        {
            return Source.Contains(item);
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (SyncRoot)
        {
            Source.CopyTo(array, arrayIndex);
        }
    }

    public T[] ToArray()
    {
        lock (SyncRoot)
        {
            return Source.ToArray();
        }
    }

    public int BinarySearch(T item)
    {
        lock (SyncRoot)
        {
            return Source.BinarySearch(item);
        }
    }

    public int BinarySearch(T item, IComparer<T> comparer)
    {
        lock (SyncRoot)
        {
            return Source.BinarySearch(item, comparer);
        }
    }
}