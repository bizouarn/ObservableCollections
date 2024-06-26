using ObservableCollections.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ObservableCollections;

public sealed partial class ObservableList<T> : SynchronizedList<List<T>, T>
{
    public ObservableList()
    {
        Source = new List<T>();
    }

    public ObservableList(int capacity)
    {
        Source = new List<T>(capacity);
    }

    public ObservableList(IEnumerable<T> collection)
    {
        Source = collection.ToList();
    }

    public void AddRange(IEnumerable<T> items)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            using (var xs = new CloneCollection<T>(items))
            {
                // to avoid iterate twice, require copy before insert.
                Source.AddRange(xs.AsEnumerable());
                InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(xs.Span, index));
            }
        }
    }

    public void AddRange(T[] items)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            Source.AddRange(items);
            InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(items, index));
        }
    }

    public void AddRange(ReadOnlySpan<T> items)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            foreach (var item in items) Source.Add(item);

            InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(items, index));
        }
    }

    public void ForEach(Action<T> action)
    {
        lock (SyncRoot)
        {
            foreach (var item in Source) action(item);
        }
    }

    public void InsertRange(int index, T[] items)
    {
        lock (SyncRoot)
        {
            Source.InsertRange(index, items);
            InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(items, index));
        }
    }

    public void InsertRange(int index, IEnumerable<T> items)
    {
        lock (SyncRoot)
        {
            using (var xs = new CloneCollection<T>(items))
            {
                Source.InsertRange(index, xs.AsEnumerable());
                InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(xs.Span, index));
            }
        }
    }

    public void InsertRange(int index, ReadOnlySpan<T> items)
    {
        lock (SyncRoot)
        {
            using (var xs = new CloneCollection<T>(items))
            {
                Source.InsertRange(index, xs.AsEnumerable());
                InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Add(xs.Span, index));
            }
        }
    }

    public void RemoveRange(int index, int count)
    {
        lock (SyncRoot)
        {
#if NET5_0_OR_GREATER
            var range = CollectionsMarshal.AsSpan(Source).Slice(index, count);
#else
            var range = Source.GetRange(index, count);
#endif

            // require copy before remove
            using (var xs = new CloneCollection<T>(range))
            {
                Source.RemoveRange(index, count);
                InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Remove(xs.Span, index));
            }
        }
    }

    public void Move(int oldIndex, int newIndex)
    {
        lock (SyncRoot)
        {
            var removedItem = Source[oldIndex];
            Source.RemoveAt(oldIndex);
            Source.Insert(newIndex, removedItem);
            InvokeCollectionChanged(NotifyCollectionChangedEventArgs<T>.Move(removedItem, newIndex, oldIndex));
        }
    }
}