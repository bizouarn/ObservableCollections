using ObservableCollections.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ObservableCollections;

public sealed class FreezedList<T> : FreezedCollection<IReadOnlyList<T>,T>, IReadOnlyList<T>
{
    public T this[int index] => Collection[index];

    public bool IsReadOnly => true;

    public FreezedList(IReadOnlyList<T> list) : base(list)
    {
    }

    public bool Contains(T item)
    {
        return Collection.Contains(item);
    }
}