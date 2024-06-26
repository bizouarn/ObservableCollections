using System;
using System.Collections;
using System.Collections.Generic;

namespace ObservableCollections.Internal;

public class FreezedCollection<TCol, TSub> : IFreezedCollection<TSub>, IEnumerable<TSub>
    where TCol : IReadOnlyCollection<TSub>
{
    protected readonly TCol Collection;

    public int Count => Collection.Count;

    protected FreezedCollection(TCol collection)
    {
        Collection = collection;
    }

    public ISynchronizedView<TSub, TView> CreateView<TView>(Func<TSub, TView> transform)
    {
        return new FreezedView<TSub, TView>(Collection, transform);
    }

    public ISortableSynchronizedView<TSub, TView> CreateSortableView<TView>(
        Func<TSub, TView> transform)
    {
        return new FreezedSortableView<TSub, TView>(Collection, transform);
    }

    public IEnumerator<TSub> GetEnumerator()
    {
        foreach (var item in Collection)
            yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}