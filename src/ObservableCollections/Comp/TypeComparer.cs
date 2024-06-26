using System.Collections.Generic;

namespace ObservableCollections.Comp;

internal class TypeComparer<T> : IComparer<T>
{
    protected readonly IComparer<T> Comparer;

    public TypeComparer(IComparer<T> comparer)
    {
        Comparer = comparer;
    }

    public virtual int Compare(T x, T y)
    {
        return Comparer.Compare(x, y);
    }
}