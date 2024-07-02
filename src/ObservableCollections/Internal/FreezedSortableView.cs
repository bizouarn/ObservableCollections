using System;
using System.Collections.Generic;
using ObservableCollections.Comp;

namespace ObservableCollections.Internal;

internal sealed class FreezedSortableView<T, TView> : FreezedView<T, TView>, ISortableSynchronizedView<T, TView>
{
    public FreezedSortableView(IEnumerable<T> source, Func<T, TView> selector) : base(source, selector)
    {
    }

    public void Sort(IComparer<T> comparer)
    {
        Array.Sort(Collection, new TypeComparerKey<T, TView>(comparer));
    }

    public void Sort(IComparer<TView> viewComparer)
    {
        Array.Sort(Collection, new TypeComparerValue<T, TView>(viewComparer));
    }
}