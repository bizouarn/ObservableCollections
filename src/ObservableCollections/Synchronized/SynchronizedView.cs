﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using ObservableCollections.Internal;

namespace ObservableCollections;

public abstract class SynchronizedView<T, TView> : Synchronized, ISynchronizedView<T, TView>
{
    protected readonly Func<T, TView> selector;
    protected readonly IObservableCollection<T> source;
    protected ISynchronizedViewFilter<T, TView> filter;

    public SynchronizedView(IObservableCollection<T> source, Func<T, TView> selector)
    {
        this.source = source;
        this.selector = selector;
        filter = SynchronizedViewFilter<T, TView>.Null;
        lock (source.SyncRoot)
        {
            this.source.CollectionChanged += SourceCollectionChanged;
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
        source.CollectionChanged -= SourceCollectionChanged;
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