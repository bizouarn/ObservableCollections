using System.Collections.Generic;

namespace ObservableCollections.Comp;

internal class TypeComparerKey<TKey, TValue> : TypeComparer<TKey>, IComparer<(TKey, TValue)>
{
    public TypeComparerKey(IComparer<TKey> comparer) : base(comparer)
    {
    }

    public virtual int Compare((TKey, TValue) x, (TKey, TValue) y)
    {
        return Compare(x.Item1, y.Item1);
    }
}