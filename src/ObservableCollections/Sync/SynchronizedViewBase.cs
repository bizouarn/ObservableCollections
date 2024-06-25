using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using ObservableCollections.Internal;

namespace ObservableCollections.Sync;

public abstract class SynchronizedViewBase<T, TView> : Synchronized, ISynchronizedView<T, TView>
{
    private readonly IObservableCollection<T> _source;
    protected ISynchronizedViewFilter<T, TView> filter;

    public SynchronizedViewBase(IObservableCollection<T> source)
    {
        _source = source;
        filter = SynchronizedViewFilter<T, TView>.Null;
        lock (source.SyncRoot)
        {
            _source.CollectionChanged += SourceCollectionChanged;
        }
    }

    public ISynchronizedViewFilter<T, TView> CurrentFilter
    {
        get
        {
            lock (SyncRoot)
            {
                return filter;
            }
        }
    }

    public abstract IEnumerator<(T Value, TView View)> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>Gets the number of elements in the collection.</summary>
    /// <returns>The number of elements in the collection.</returns>
    public abstract int Count { get; }

    public void Dispose()
    {
        _source.CollectionChanged -= SourceCollectionChanged;
    }

    public event NotifyCollectionChangedEventHandler<T>? RoutingCollectionChanged;
    public event Action<NotifyCollectionChangedAction>? CollectionStateChanged;

    public abstract void AttachFilter(ISynchronizedViewFilter<T, TView> filter,
        bool invokeAddEventForCurrentElements = false);

    public abstract void ResetFilter(Action<T, TView>? resetAction);

    public INotifyCollectionChangedSynchronizedView<TView> ToNotifyCollectionChanged()
    {
        lock (SyncRoot)
        {
            return new NotifyCollectionChangedSynchronizedView<T, TView>(this);
        }
    }

    protected virtual void SourceCollectionChanged(in NotifyCollectionChangedEventArgs<T> e)
    {
        RoutingCollectionChanged?.Invoke(e);
        CollectionStateChanged?.Invoke(e.Action);
    }
}