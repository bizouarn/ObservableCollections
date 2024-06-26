using System.Collections.Generic;
using System.Linq;
using ObservableCollections.Internal;

namespace ObservableCollections;

public sealed class FreezedList<T> : FreezedCollection<IReadOnlyList<T>, T>, IReadOnlyList<T>
{
    public FreezedList(IReadOnlyList<T> list) : base(list)
    {
    }

    public bool IsReadOnly => true;
    public T this[int index] => Collection[index];

    public bool Contains(T item)
    {
        return Collection.Contains(item);
    }
}