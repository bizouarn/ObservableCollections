#pragma warning disable CS0067

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace ObservableCollections.Internal;

internal class FreezedView<T, TView> : FreezedCollection<(T, TView)[], (T, TView)>, ISynchronizedView<T, TView>
{
    private ISynchronizedViewFilter<T, TView> _filter;

    public FreezedView(IEnumerable<T> source, Func<T, TView> selector) : base(source.Select(x => (x, selector(x)))
        .ToArray())
    {
        _filter = SynchronizedViewFilter<T, TView>._null;
    }

    public object SyncRoot { get; } = new();

    public ISynchronizedViewFilter<T, TView> CurrentFilter
    {
        get
        {
            lock (SyncRoot)
            {
                return _filter;
            }
        }
    }

    public event Action<NotifyCollectionChangedAction>? CollectionStateChanged;
    public event NotifyCollectionChangedEventHandler<T>? RoutingCollectionChanged;

    public void AttachFilter(ISynchronizedViewFilter<T, TView> filter, bool invokeAddEventForCurrentElements = false)
    {
        lock (SyncRoot)
        {
            this._filter = filter;
            for (var i = 0; i < Collection.Length; i++)
            {
                var (value, view) = Collection[i];
                if (invokeAddEventForCurrentElements)
                    filter.InvokeOnAdd(value, view, i);
                else
                    filter.InvokeOnAttach(value, view);
            }
        }
    }

    public void ResetFilter(Action<T, TView>? resetAction)
    {
        lock (SyncRoot)
        {
            _filter = SynchronizedViewFilter<T, TView>._null;
            if (resetAction != null)
                foreach (var (item, view) in Collection)
                    resetAction(item, view);
        }
    }

    public IEnumerator<(T, TView)> GetEnumerator()
    {
        lock (SyncRoot)
        {
            foreach (var item in Collection)
                if (_filter.IsMatch(item.Item1, item.Item2))
                    yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Dispose()
    {
    }

    public INotifyCollectionChangedSynchronizedView<TView> ToNotifyCollectionChanged()
    {
        return new NotifyCollectionChangedSynchronizedView<T, TView>(this);
    }
}