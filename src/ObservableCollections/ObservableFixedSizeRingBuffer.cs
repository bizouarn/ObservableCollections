﻿using ObservableCollections.Internal;
using System;
using System.Collections.Generic;

namespace ObservableCollections;

public sealed class ObservableFixedSizeRingBuffer<T> : SynchronizedList<RingBuffer<T>, T>, IObservableCollection<T>
{
    private readonly int capacity;

    public ObservableFixedSizeRingBuffer(int capacity)
    {
        this.capacity = capacity;
        Source = new RingBuffer<T>(capacity);
    }

    public ObservableFixedSizeRingBuffer(int capacity, IEnumerable<T> collection)
    {
        this.capacity = capacity;
        Source = new RingBuffer<T>(capacity);
        foreach (var item in collection)
        {
            if (capacity == Source.Count) Source.RemoveFirst();
            Source.AddLast(item);
        }
    }

    public int Capacity => capacity;

    public void AddFirst(T item)
    {
        lock (SyncRoot)
        {
            if (capacity == Source.Count)
            {
                var remItem = Source.RemoveLast();
                InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Remove(remItem, capacity - 1));
            }

            Source.AddFirst(item);
            InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(item, 0));
        }
    }

    public void AddLast(T item)
    {
        lock (SyncRoot)
        {
            if (capacity == Source.Count)
            {
                var remItem = Source.RemoveFirst();
                InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Remove(remItem, 0));
            }

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
            using (var xs = new CloneCollection<T>(items))
            {
                if (capacity <= Source.Count + xs.Span.Length)
                {
                    // calc remove count
                    var remCount = Math.Min(Source.Count, Source.Count + xs.Span.Length - capacity);
                    using (var ys = new ResizableArray<T>(remCount))
                    {
                        for (var i = 0; i < remCount; i++) ys.Add(Source.RemoveFirst());

                        InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Remove(ys.Span, 0));
                    }
                }

                var index = Source.Count;
                var span = xs.Span;
                if (span.Length > capacity) span = span.Slice(span.Length - capacity);

                foreach (var item in span) Source.AddLast(item);
                InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(span, index));
            }
        }
    }

    public void AddLastRange(T[] items)
    {
        lock (SyncRoot)
        {
            if (capacity <= Source.Count + items.Length)
            {
                // calc remove count
                var remCount = Math.Min(Source.Count, Source.Count + items.Length - capacity);
                using (var ys = new ResizableArray<T>(remCount))
                {
                    for (var i = 0; i < remCount; i++) ys.Add(Source.RemoveFirst());

                    InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Remove(ys.Span, 0));
                }
            }

            var index = Source.Count;
            var span = items.AsSpan();
            if (span.Length > capacity) span = span.Slice(span.Length - capacity);

            foreach (var item in span) Source.AddLast(item);
            InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(span, index));
        }
    }

    public void AddLastRange(ReadOnlySpan<T> items)
    {
        lock (SyncRoot)
        {
            if (capacity <= Source.Count + items.Length)
            {
                // calc remove count
                var remCount = Math.Min(Source.Count, Source.Count + items.Length - capacity);
                using (var ys = new ResizableArray<T>(remCount))
                {
                    for (var i = 0; i < remCount; i++) ys.Add(Source.RemoveFirst());

                    InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Remove(ys.Span, 0));
                }
            }

            var index = Source.Count;
            var span = items;
            if (span.Length > capacity) span = span.Slice(span.Length - capacity);

            foreach (var item in span) Source.AddLast(item);
            InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(span, index));
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

    public ISynchronizedView<T, TView> CreateView<TView>(Func<T, TView> transform)
    {
        return new ObservableRingBuffer<T>.View<TView>(this, transform);
    }
}