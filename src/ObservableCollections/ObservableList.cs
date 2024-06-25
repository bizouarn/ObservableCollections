using ObservableCollections.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ObservableCollections;

public sealed partial class ObservableList<T> : SynchronizedCollection<T, List<T>>, IList<T>, IReadOnlyList<T>,
    IObservableCollection<T>
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

    public bool IsReadOnly => false;

    public event NotifyCollectionChangedEventHandler<T>? CollectionChanged;

    public void Add(T item)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            Source.Add(item);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(item, index));
        }
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
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(xs.Span, index));
            }
        }
    }

    public void AddRange(T[] items)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            Source.AddRange(items);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(items, index));
        }
    }

    public void AddRange(ReadOnlySpan<T> items)
    {
        lock (SyncRoot)
        {
            var index = Source.Count;
            foreach (var item in items) Source.Add(item);

            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(items, index));
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

    public void ForEach(Action<T> action)
    {
        lock (SyncRoot)
        {
            foreach (var item in Source) action(item);
        }
    }

    public int IndexOf(T item)
    {
        lock (SyncRoot)
        {
            return Source.IndexOf(item);
        }
    }

    public void Insert(int index, T item)
    {
        lock (SyncRoot)
        {
            Source.Insert(index, item);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(item, index));
        }
    }

    public void InsertRange(int index, T[] items)
    {
        lock (SyncRoot)
        {
            Source.InsertRange(index, items);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(items, index));
        }
    }

    public void InsertRange(int index, IEnumerable<T> items)
    {
        lock (SyncRoot)
        {
            using (var xs = new CloneCollection<T>(items))
            {
                Source.InsertRange(index, xs.AsEnumerable());
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(xs.Span, index));
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
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Add(xs.Span, index));
            }
        }
    }

    public bool Remove(T item)
    {
        lock (SyncRoot)
        {
            var index = Source.IndexOf(item);

            if (index >= 0)
            {
                Source.RemoveAt(index);
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(item, index));
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public void RemoveAt(int index)
    {
        lock (SyncRoot)
        {
            var item = Source[index];
            Source.RemoveAt(index);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(item, index));
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
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(xs.Span, index));
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
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<T>.Move(removedItem, newIndex, oldIndex));
        }
    }
}