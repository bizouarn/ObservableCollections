using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using ObservableCollections.Comp;
using ObservableCollections.Internal;

namespace ObservableCollections;

public delegate void NotifyCollectionChangedEventHandler<T>(in NotifyCollectionChangedEventArgs<T> e);

public interface IObservableCollection<T> : ISynchronized, IReadOnlyCollection<T>
{
    event NotifyCollectionChangedEventHandler<T>? CollectionChanged;
    ISynchronizedView<T, TView> CreateView<TView>(Func<T, TView> transform);
}

public interface IReadOnlyObservableDictionary<TKey, TValue> :
    IReadOnlyDictionary<TKey, TValue>, IObservableCollection<KeyValuePair<TKey, TValue>>
{
}

public interface IFreezedCollection<T>
{
    ISynchronizedView<T, TView> CreateView<TView>(Func<T, TView> transform);
    ISortableSynchronizedView<T, TView> CreateSortableView<TView>(Func<T, TView> transform);
}

// will be implemented in the future?
//public interface IGroupedSynchoronizedView<T, TKey, TView> : ILookup<TKey, (T, TView)>, ISynchronizedView<T, TView>
//{
//}

public interface INotifyCollectionChangedSynchronizedView<out TView> : IReadOnlyCollection<TView>,
    INotifyCollectionChanged, INotifyPropertyChanged, IDisposable
{
}