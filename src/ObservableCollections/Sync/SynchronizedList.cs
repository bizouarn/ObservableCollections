using System.Collections.Generic;

namespace ObservableCollections;

public abstract class SynchronizedList<TCol, TSub> 
    : SynchronizedCollection<TCol, TSub>, IList<TSub>, IReadOnlyList<TSub>, ICollection<TSub>
    where TCol : IList<TSub>, IEnumerable<TSub>, IReadOnlyCollection<TSub>, ICollection<TSub>
{
    public TSub this[int index]
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
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TSub>.Replace(value, oldValue, index, index));
            }
        }
    }

    public bool IsReadOnly => false;

    public event NotifyCollectionChangedEventHandler<TSub>? CollectionChanged;

    public virtual void Add(TSub item)
    {
        lock (SyncRoot)
        {
            var index = ((IList<TSub>)Source).Count;
            Source.Add(item);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TSub>.Add(item, index));
        }
    }

    public virtual void Clear()
    {
        lock (SyncRoot)
        {
            Source.Clear();
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TSub>.Reset());
        }
    }

    public bool Contains(TSub item)
    {
        lock (SyncRoot)
        {
            return Source.Contains(item);
        }
    }

    public void CopyTo(TSub[] array, int arrayIndex)
    {
        lock (SyncRoot)
        {
            Source.CopyTo(array, arrayIndex);
        }
    }

    public virtual bool Remove(TSub item)
    {
        lock (SyncRoot)
        {
            var index = Source.IndexOf(item);

            if (index >= 0)
            {
                Source.RemoveAt(index);
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TSub>.Remove(item, index));
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public int IndexOf(TSub item)
    {
        lock (SyncRoot)
        {
            return Source.IndexOf(item);
        }
    }

    public virtual void Insert(int index, TSub item)
    {
        lock (SyncRoot)
        {
            Source.Insert(index, item);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TSub>.Add(item, index));
        }
    }

    public virtual void RemoveAt(int index)
    {
        lock (SyncRoot)
        {
            var item = Source[index];
            Source.RemoveAt(index);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TSub>.Remove(item, index));
        }
    }

    protected void InvokeCollectionChanged(NotifyCollectionChangedEventArgs<TSub> collectionChanged)
    {
        CollectionChanged?.Invoke(collectionChanged);
    }
}