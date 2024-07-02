using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using ObservableCollections.Sync;

namespace ObservableCollections.Internal;

internal class SortedView<T, TKey, TView> : SynchronizedViewBase<T, TView>
    where TKey : notnull
{
    private readonly Func<T, TKey> _identitySelector;
    private readonly SortedList<(T Value, TKey Key), (T Value, TView View)> _list;
    private readonly Func<T, TView> _transform;

    public SortedView(IObservableCollection<T> source, Func<T, TKey> identitySelector, Func<T, TView> transform,
        IComparer<T> comparer)
        : base(source)
    {
        this._identitySelector = identitySelector;
        this._transform = transform;
        lock (source.SyncRoot)
        {
            var dict = new Dictionary<(T, TKey), (T, TView)>(source.Count);
            foreach (var v in source) dict.Add((v, identitySelector(v)), (v, transform(v)));

            _list = new SortedList<(T Value, TKey Key), (T Value, TView View)>(dict, new Comparer(comparer));
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
                        _list.Add((value, id), (value, view));
                        var index = _list.IndexOfKey((value, id));
                        Filter.InvokeOnAdd(value, view, index);
                    }
                    else
                    {
                        foreach (var value in e.NewItems)
                        {
                            var view = _transform(value);
                            var id = _identitySelector(value);
                            _list.Add((value, id), (value, view));
                            var index = _list.IndexOfKey((value, id));
                            Filter.InvokeOnAdd(value, view, index);
                        }
                    }
                }
                    break;
                case NotifyCollectionChangedAction.Remove:
                {
                    if (e.IsSingleItem)
                    {
                        var value = e.OldItem;
                        var id = _identitySelector(value);
                        var key = (value, id);
                        if (_list.TryGetValue(key, out var v))
                        {
                            var index = _list.IndexOfKey(key);
                            _list.RemoveAt(index);
                            Filter.InvokeOnRemove(v.Value, v.View, index);
                        }
                    }
                    else
                    {
                        foreach (var value in e.OldItems)
                        {
                            var id = _identitySelector(value);
                            var key = (value, id);
                            if (_list.TryGetValue(key, out var v))
                            {
                                var index = _list.IndexOfKey((value, id));
                                _list.RemoveAt(index);
                                Filter.InvokeOnRemove(v.Value, v.View, index);
                            }
                        }
                    }
                }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    // ReplaceRange is not supported in all ObservableCollections collections
                    // Replace is remove old item and insert new item.
                {
                    var oldValue = e.OldItem;
                    var oldKey = (oldValue, _identitySelector(oldValue));
                    var oldIndex = -1;
                    if (_list.TryGetValue(oldKey, out var o))
                    {
                        oldIndex = _list.IndexOfKey(oldKey);
                        _list.RemoveAt(oldIndex);
                    }

                    var value = e.NewItem;
                    var view = _transform(value);
                    var id = _identitySelector(value);
                    _list.Add((value, id), (value, view));
                    var newIndex = _list.IndexOfKey((value, id));

                    Filter.InvokeOnReplace((value, view), o, newIndex, oldIndex);
                }
                    break;
                case NotifyCollectionChangedAction.Move:
                {
                    // Move(index change) does not affect sorted list.
                    var oldValue = e.OldItem;
                    var oldKey = (oldValue, _identitySelector(oldValue));
                    if (_list.TryGetValue(oldKey, out var v))
                    {
                        var index = _list.IndexOfKey(oldKey);
                        Filter.InvokeOnMove(v, index, index);
                    }
                }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _list.Clear();
                    Filter.InvokeOnReset();
                    break;
            }

            base.SourceCollectionChanged(e);
        }
    }

    private sealed class Comparer : IComparer<(T value, TKey id)>
    {
        private readonly IComparer<T> _comparer;

        public Comparer(IComparer<T> comparer)
        {
            this._comparer = comparer;
        }

        public int Compare((T value, TKey id) x, (T value, TKey id) y)
        {
            var compare = _comparer.Compare(x.value, y.value);
            if (compare == 0) compare = Comparer<TKey>.Default.Compare(x.id, y.id);

            return compare;
        }
    }
}