#pragma warning disable CS0067

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ObservableCollections.Comp;

namespace ObservableCollections.Internal;

internal sealed class FreezedView<T, TView> : Synchronized, ISynchronizedView<T, TView>
{
    private readonly List<(T, TView)> list;

    private ISynchronizedViewFilter<T, TView> filter;

    public FreezedView(IEnumerable<T> source, Func<T, TView> selector)
    {
        filter = SynchronizedViewFilter<T, TView>.Null;
        list = source.Select(x => (x, selector(x))).ToList();
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

    public event Action<NotifyCollectionChangedAction>? CollectionStateChanged;
    public event NotifyCollectionChangedEventHandler<T>? RoutingCollectionChanged;

    public int Count
    {
        get
        {
            lock (SyncRoot)
            {
                return list.Count;
            }
        }
    }

    public void AttachFilter(ISynchronizedViewFilter<T, TView> filter, bool invokeAddEventForCurrentElements = false)
    {
        lock (SyncRoot)
        {
            this.filter = filter;
            for (var i = 0; i < list.Count; i++)
            {
                var (value, view) = list[i];
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
            filter = SynchronizedViewFilter<T, TView>.Null;
            if (resetAction != null)
                foreach (var (item, view) in list)
                    resetAction(item, view);
        }
    }

    public IEnumerator<(T, TView)> GetEnumerator()
    {
        lock (SyncRoot)
        {
            foreach (var item in list)
                if (filter.IsMatch(item.Item1, item.Item2))
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

internal sealed class FreezedSortableView<T, TView> : Synchronized, ISortableSynchronizedView<T, TView>
{
    private readonly (T, TView)[] array;

    private ISynchronizedViewFilter<T, TView> filter;

    public FreezedSortableView(IEnumerable<T> source, Func<T, TView> selector)
    {
        filter = SynchronizedViewFilter<T, TView>.Null;
        array = source.Select(x => (x, selector(x))).ToArray();
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

    public event Action<NotifyCollectionChangedAction>? CollectionStateChanged;
    public event NotifyCollectionChangedEventHandler<T>? RoutingCollectionChanged;

    public int Count
    {
        get
        {
            lock (SyncRoot)
            {
                return array.Length;
            }
        }
    }

    public void AttachFilter(ISynchronizedViewFilter<T, TView> filter, bool invokeAddEventForCurrentElements = false)
    {
        lock (SyncRoot)
        {
            this.filter = filter;
            for (var i = 0; i < array.Length; i++)
            {
                var (value, view) = array[i];
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
            filter = SynchronizedViewFilter<T, TView>.Null;
            if (resetAction != null)
                foreach (var (item, view) in array)
                    resetAction(item, view);
        }
    }

    public IEnumerator<(T, TView)> GetEnumerator()
    {
        lock (SyncRoot)
        {
            foreach (var item in array)
                if (filter.IsMatch(item.Item1, item.Item2))
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

    public void Sort(IComparer<T> comparer)
    {
        Array.Sort(array, new TypeComparerKey<T, TView>(comparer));
    }

    public void Sort(IComparer<TView> viewComparer)
    {
        Array.Sort(array, new TypeComparerValue<T, TView>(viewComparer));
    }

    public INotifyCollectionChangedSynchronizedView<TView> ToNotifyCollectionChanged()
    {
        return new NotifyCollectionChangedSynchronizedView<T, TView>(this);
    }
}