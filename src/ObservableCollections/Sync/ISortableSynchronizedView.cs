using System.Collections.Generic;

namespace ObservableCollections;

public interface ISortableSynchronizedView<T, TView> : ISynchronizedView<T, TView>
{
    void Sort(IComparer<T> comparer);
    void Sort(IComparer<TView> viewComparer);
}