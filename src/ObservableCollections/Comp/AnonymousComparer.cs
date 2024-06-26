using System;
using System.Collections.Generic;

namespace ObservableCollections.Comp;

internal class AnonymousComparer<T, TCompare> : IComparer<T>
{
    private readonly int _f;
    private readonly Func<T, TCompare> _selector;

    public AnonymousComparer(Func<T, TCompare> selector, bool ascending)
    {
        _selector = selector;
        _f = ascending ? 1 : -1;
    }

    public int Compare(T? x, T? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return 1 * _f;
        if (y == null) return -1 * _f;

        return Comparer<TCompare>.Default.Compare(_selector(x), _selector(y)) * _f;
    }
}