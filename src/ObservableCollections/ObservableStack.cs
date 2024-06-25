﻿using ObservableCollections.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ObservableCollections;

public sealed partial class ObservableStack<T> : SynchronizedCollection<Stack<T>, T>, IObservableCollection<T>
{
    public ObservableStack()
    {
        Source = new Stack<T>();
    }

    public ObservableStack(int capacity)
    {
        Source = new Stack<T>(capacity);
    }

    public ObservableStack(IEnumerable<T> collection)
    {
        Source = new Stack<T>(collection);
    }

    public event NotifyCollectionChangedEventHandler<T>? CollectionChanged;

    public void Push(T item)
    {
        lock (SyncRoot)
        {
            Source.Push(item);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(item, 0));
        }
    }

    public void PushRange(IEnumerable<T> items)
    {
        lock (SyncRoot)
        {
            using (var xs = new CloneCollection<T>(items))
            {
                foreach (var item in xs.Span) Source.Push(item);
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(xs.Span, 0));
            }
        }
    }

    public void PushRange(T[] items)
    {
        lock (SyncRoot)
        {
            foreach (var item in items) Source.Push(item);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(items, 0));
        }
    }

    public void PushRange(ReadOnlySpan<T> items)
    {
        lock (SyncRoot)
        {
            foreach (var item in items) Source.Push(item);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(items, 0));
        }
    }

    public T Pop()
    {
        lock (SyncRoot)
        {
            var v = Source.Pop();
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(v, 0));
            return v;
        }
    }

    public bool TryPop([MaybeNullWhen(false)] out T result)
    {
        lock (SyncRoot)
        {
            if (Source.Count != 0)
            {
                result = Source.Pop();
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(result, 0));
                return true;
            }

            result = default;
            return false;
        }
    }

    public void PopRange(int count)
    {
        lock (SyncRoot)
        {
            var dest = ArrayPool<T>.Shared.Rent(count);
            try
            {
                for (var i = 0; i < count; i++) dest[i] = Source.Pop();

                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(dest.AsSpan(0, count), 0));
            }
            finally
            {
                ArrayPool<T>.Shared.Return(dest, RuntimeHelpersEx.IsReferenceOrContainsReferences<T>());
            }
        }
    }

    public void PopRange(Span<T> dest)
    {
        lock (SyncRoot)
        {
            for (var i = 0; i < dest.Length; i++) dest[i] = Source.Pop();

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