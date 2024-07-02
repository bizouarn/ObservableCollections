using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using ObservableCollections.Comp;
using ObservableCollections.Sync;

namespace ObservableCollections.Internal;

internal class SortedViewViewComparer<T, TKey, TView> : SynchronizedViewBase<T, TView>
    where TKey : notnull
{
    private readonly Func<T, TKey> _identitySelector;
    private readonly SortedList<(TView View, TKey Key), (T Value, TView View)> _list;
    private readonly Func<T, TView> _transform;
    private readonly Dictionary<TKey, TView> _viewMap; // view-map needs to use in remove.

    public SortedViewViewComparer(IObservableCollection<T> source, Func<T, TKey> identitySelector,
        Func<T, TView> transform, IComparer<TView> comparer)
        : base(source)
    {
        this._identitySelector = identitySelector;
        this._transform = transform;
        lock (source.SyncRoot)
        {
            var dict = new Dictionary<(TView, TKey), (T, TView)>(source.Count);
            _viewMap = new Dictionary<TKey, TView>();
            foreach (var value in source)
            {
                var view = transform(value);
                var id = identitySelector(value);
                dict.Add((view, id), (value, view));
                _viewMap.Add(id, view);
            }

            _list = new SortedList<(TView View, TKey Key), (T Value, TView View)>(dict, new Comparer(comparer));
        }
    }

    public override int Count
    {
        get
        {
            lock (SyncRoot)
            {
                return _list.Count;
            }
        }
    }

    public override void AttachFilter(ISynchronizedViewFilter<T, TView> filter,
        bool invokeAddEventForCurrentElements = false)
    {
        lock (SyncRoot)
        {
            this.Filter = filter;
            var i = 0;
            foreach (var (_, (value, view)) in _list)
                if (invokeAddEventForCurrentElements)
                    filter.InvokeOnAdd(value, view, i++);
                else
                    filter.InvokeOnAttach(value, view);
        }
    }

    public override void ResetFilter(Action<T, TView>? resetAction)
    {
        lock (SyncRoot)
        {
            Filter = SynchronizedViewFilter<T, TView>._null;
            if (resetAction != null)
                foreach (var (_, (value, view)) in _list)
                    resetAction(value, view);
        }
    }

    public override IEnumerator<(T, TView)> GetEnumerator()
    {
        lock (SyncRoot)
        {
            foreach (var item in _list)
                if (Filter.IsMatch(item.Value.Value, item.Value.View))
                    yield return item.Value;
        }
    }

    protected override void SourceCollectionChanged(in NotifyCollectionChangedEventArgs<T> e)
    {
        lock (SyncRoot)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    // Add, Insert
                    if (e.IsSingleItem)
                    {
                        var value = e.NewItem;
                        var view = _transform(value);
                        var id = _identitySelector(value);
                        _list.Add((view, id), (value, view));
                        _viewMap.Add(id, view);
                        var index = _list.IndexOfKey((view, id));
                        Filter.InvokeOnAdd(value, view, index);
                    }
                    else
                    {
                        foreach (var value in e.NewItems)
                        {
                            var view = _transform(value);
                            var id = _identitySelector(value);
                            _list.Add((view, id), (value, view));
                            _viewMap.Add(id, view);
                            var index = _list.IndexOfKey((view, id));
                            Filter.InvokeOnAdd(value, view, index);
                        }
                    }

                    break;
                }
                case NotifyCollectionChangedAction.Remove:
                {
                    if (e.IsSingleItem)
                    {
                        var value = e.OldItem;
                        var id = _identitySelector(value);
                        if (_viewMap.Remove(id, out var view))
                        {
                            var key = (view, id);
                            if (_list.TryGetValue(key, out var v))
                            {
                                var index = _list.IndexOfKey(key);
                                _list.RemoveAt(index);
                                Filter.InvokeOnRemove(v, index);
                            }
                        }
                    }
                    else
                    {
                        foreach (var value in e.OldItems)
                        {
                            var id = _identitySelector(value);
                            if (_viewMap.Remove(id, out var view))
                            {
                                var key = (view, id);
                                if (_list.TryGetValue(key, out var v))
                                {
                                    var index = _list.IndexOfKey((view, id));
                                    _list.RemoveAt(index);
                                    Filter.InvokeOnRemove(v, index);
                                }
                            }
                        }
                    }

                    break;
                }
                case NotifyCollectionChangedAction.Replace:
                    // Replace is remove old item and insert new item.
                {
                    var oldValue = e.OldItem;
                    var oldId = _identitySelector(oldValue);
                    var oldIndex = -1;
                    if (_viewMap.Remove(oldId, out var oldView))
                    {
                        var oldKey = (oldView, oldId);
                        oldIndex = _list.IndexOfKey(oldKey);
                        if (oldIndex > -1) _list.RemoveAt(oldIndex);
                    }

                    var value = e.NewItem;
                    var view = _transform(value);
                    var id = _identitySelector(value);
                    _list.Add((view, id), (value, view));
                    _viewMap.Add(id, view);

                    var index = _list.IndexOfKey((view, id));
                    Filter.InvokeOnReplace(value, view, oldValue, oldView!, index, oldIndex);
                    break;
                }
                case NotifyCollectionChangedAction.Move:
                    // Move(index change) does not affect soreted dict.
                {
                    var value = e.OldItem;
                    var id = _identitySelector(value);
                    if (_viewMap.TryGetValue(id, out var view))
                    {
                        var index = _list.IndexOfKey((view, id));
                        Filter.InvokeOnMove(value, view, index, index);
                    }

                    break;
                }
                case NotifyCollectionChangedAction.Reset:
                    _list.Clear();
                    _viewMap.Clear();
                    Filter.InvokeOnReset();
                    break;
            }

            base.SourceCollectionChanged(e);
        }
    }

    private sealed class Comparer : TypeComparerKey<TView, TKey>
    {
        public Comparer(IComparer<TView> comparer) : base(comparer)
        {
        }

        public override int Compare((TView, TKey) x, (TView, TKey) y)
        {
            var compare = base.Compare(x, y);
            if (compare == 0) compare = Comparer<TKey>.Default.Compare(x.Item2, y.Item2);
            return compare;
        }
    }
}