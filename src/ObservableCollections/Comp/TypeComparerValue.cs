using System.Collections.Generic;

namespace ObservableCollections.Comp;

internal class TypeComparerValue<TKey, TValue> : TypeComparer<TValue>, IComparer<(TKey, TValue)>
{
    public TypeComparerValue(IComparer<TValue> comparer) : base(comparer)
    {
    }

    public virtual int Compare((TKey, TValue) x, (TKey, TValue) y)
    {
        return Compare(x.Item2, y.Item2);
    }
}