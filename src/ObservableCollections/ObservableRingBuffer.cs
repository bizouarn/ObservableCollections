using System;
using System.Collections.Generic;
using ObservableCollections.Internal;

namespace ObservableCollections;

public sealed partial class ObservableRingBuffer<T> : SynchronizedList<RingBuffer<T>, T>,
    IObservableCollection<T>
{
    public ObservableRingBuffer()
    {
        Source = new RingBuffer<T>();
    }

    public ObservableRingBuffer(IEnumerable<T> collection)
    {
        Source = new RingBuffer<T>(collection);
    }

    public void AddFirst(T item)
    {
        lock (SyncRoot)
        {
            Source.AddFirst(item);
            InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(item, 0));
        }
    }

    public void AddLast(T item)
    {
        lock (SyncRoot)
        {
            Source.AddLast(item);
            InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(item, Source.Count - 1));
        }
    }

    public T RemoveFirst()
    {
        lock (SyncRoot)
        {
            var item = Source.RemoveFirst();
            InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Remove(item, 0));
            return item;
        }
    }

    public T RemoveLast()
    {
        lock (SyncRoot)
        {
            var index = Source.Count - 1;
            var item = Source.RemoveLast();
            InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Remove(item, index));
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
                InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(xs.Span, index));
            }
        }
    }

    public void AddLastRange(T[] items)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            foreach (var item in items) Source.AddLast(item);
            InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(items, index));
        }
    }

    public void AddLastRange(ReadOnlySpan<T> items)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            foreach (var item in items) Source.AddLast(item);
            InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(items, index));
        }
    }

    public override void Insert(int index, T item)
    {
        throw new NotSupportedException();
    }

    public override bool Remove(T item)
    {
        throw new NotSupportedException();
    }

    public override void RemoveAt(int index)
    {
        throw new NotSupportedException();
    }

    public override void Add(T item)
    {
        AddLast(item);
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