using ObservableCollections.Internal;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;

namespace ObservableCollections;

public sealed partial class ObservableQueue<T> : SynchronizedCollection<T, Queue<T>>, IObservableCollection<T>
{
    public ObservableQueue()
    {
        Source = new Queue<T>();
    }

    public ObservableQueue(int capacity)
    {
        Source = new Queue<T>(capacity);
    }

    public ObservableQueue(IEnumerable<T> collection)
    {
        Source = new Queue<T>(collection);
    }

    public event NotifyCollectionChangedEventHandler<T>? CollectionChanged;

    public void Enqueue(T item)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            Source.Enqueue(item);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(item, index));
        }
    }

    public void EnqueueRange(IEnumerable<T> items)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            using (var xs = new CloneCollection<T>(items))
            {
                foreach (var item in xs.Span) Source.Enqueue(item);
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(xs.Span, index));
            }
        }
    }

    public void EnqueueRange(T[] items)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            foreach (var item in items) Source.Enqueue(item);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(items, index));
        }
    }

    public void EnqueueRange(ReadOnlySpan<T> items)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            foreach (var item in items) Source.Enqueue(item);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(items, index));
        }
    }

    public T Dequeue()
    {
        lock (SyncRoot)
        {
            var v = Source.Dequeue();
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(v, 0));
            return v;
        }
    }

    public bool TryDequeue([MaybeNullWhen(false)] out T result)
    {
        lock (SyncRoot)
        {
            if (Source.Count != 0)
            {
                result = Source.Dequeue();
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(result, 0));
                return true;
            }

            result = default;
            return false;
        }
    }

    public void DequeueRange(int count)
    {
        lock (SyncRoot)
        {
            var dest = ArrayPool<T>.Shared.Rent(count);
            try
            {
                for (var i = 0; i < count; i++) dest[i] = Source.Dequeue();

                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(dest.AsSpan(0, count), 0));
            }
            finally
            {
                ArrayPool<T>.Shared.Return(dest, RuntimeHelpersEx.IsReferenceOrContainsReferences<T>());
            }
        }
    }

    public void DequeueRange(Span<T> dest)
    {
        lock (SyncRoot)
        {
            for (var i = 0; i < dest.Length; i++) dest[i] = Source.Dequeue();

            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(dest, 0));
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

    public T Peek()
    {
        lock (SyncRoot)
        {
            return Source.Peek();
        }
    }

    public bool TryPeek([MaybeNullWhen(false)] T result)
    {
        lock (SyncRoot)
        {
            if (Source.Count != 0)
            {
                result = Source.Peek();
                return true;
            }

            result = default;
            return false;
        }
    }

    public T[] ToArray()
    {
        lock (SyncRoot)
        {
            return Source.ToArray();
        }
    }

    public void TrimExcess()
    {
        lock (SyncRoot)
        {
            Source.TrimExcess();
        }
    }
}