using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using ObservableCollections.Sync;

namespace ObservableCollections.Internal
{
    internal class SortedViewViewComparer<T, TKey, TView> : SynchronizedViewBase<T, TView>
        where TKey : notnull
    {
        readonly Func<T, TView> transform;
        readonly Func<T, TKey> identitySelector;
        readonly Dictionary<TKey, TView> viewMap; // view-map needs to use in remove.
        readonly SortedList<(TView View, TKey Key), (T Value, TView View)> list;

        public SortedViewViewComparer(IObservableCollection<T> source, Func<T, TKey> identitySelector, Func<T, TView> transform, IComparer<TView> comparer)
            : base(source)
        {
            this.identitySelector = identitySelector;
            this.transform = transform;
            lock (source.SyncRoot)
            {
                var dict = new Dictionary<(TView, TKey), (T, TView)>(source.Count);
                this.viewMap = new Dictionary<TKey, TView>();
                foreach (var value in source)
                {
                    var view = transform(value);
                    var id = identitySelector(value);
                    dict.Add((view, id), (value, view));
                    viewMap.Add(id, view);
                }
                this.list = new SortedList<(TView View, TKey Key), (T Value, TView View)>(dict, new Comparer(comparer));
            }
        }

        public override int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return list.Count;
                }
            }
        }

        public override void AttachFilter(ISynchronizedViewFilter<T, TView> filter, bool invokeAddEventForCurrentElements = false)
        {
            lock (SyncRoot)
            {
                this.filter = filter;
                var i = 0;
                foreach (var (_, (value, view)) in list)
                {
                    if (invokeAddEventForCurrentElements)
                    {
                        filter.InvokeOnAdd(value, view, i++);
                    }
                    else
                    {
                        filter.InvokeOnAttach(value, view);
                    }
                }
            }
        }

        public override void ResetFilter(Action<T, TView>? resetAction)
        {
            lock (SyncRoot)
            {
                this.filter = SynchronizedViewFilter<T, TView>.Null;
                if (resetAction != null)
                {
                    foreach (var (_, (value, view)) in list)
                    {
                        resetAction(value, view);
                    }
                }
            }
        }

        public override IEnumerator<(T, TView)> GetEnumerator()
        {

            lock (SyncRoot)
            {
                foreach (var item in list)
                {
                    if (filter.IsMatch(item.Value.Value, item.Value.View))
                    {
                        yield return item.Value;
                    }
                }
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
                            var view = transform(value);
                            var id = identitySelector(value);
                            list.Add((view, id), (value, view));
                            viewMap.Add(id, view);
                            var index = list.IndexOfKey((view, id));
                            filter.InvokeOnAdd(value, view, index);
                        }
                        else
                        {
                            foreach (var value in e.NewItems)
                            {
                                var view = transform(value);
                                var id = identitySelector(value);
                                list.Add((view, id), (value, view));
                                viewMap.Add(id, view);
                                var index = list.IndexOfKey((view, id));
                                filter.InvokeOnAdd(value, view, index);
                            }
                        }
                        break;
                    }
                    case NotifyCollectionChangedAction.Remove:
                    {
                        if (e.IsSingleItem)
                        {
                            var value = e.OldItem;
                            var id = identitySelector(value);
                            if (viewMap.Remove(id, out var view))
                            {
                                var key = (view, id);
                                if (list.TryGetValue(key, out var v))
                                {
                                    var index = list.IndexOfKey(key);
                                    list.RemoveAt(index);
                                    filter.InvokeOnRemove(v, index);
                                }
                            }
                        }
                        else
                        {
                            foreach (var value in e.OldItems)
                            {
                                var id = identitySelector(value);
                                if (viewMap.Remove(id, out var view))
                                {
                                    var key = (view, id);
                                    if (list.TryGetValue(key, out var v))
                                    {
                                        var index = list.IndexOfKey((view, id));
                                        list.RemoveAt(index);
                                        filter.InvokeOnRemove(v, index);
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
                        var oldId = identitySelector(oldValue);
                        var oldIndex = -1;
                        if (viewMap.Remove(oldId, out var oldView))
                        {
                            var oldKey = (oldView, oldId);
                            oldIndex = list.IndexOfKey(oldKey);
                            if (oldIndex > -1)
                            {
                                list.RemoveAt(oldIndex);
                            }
                        }

                        var value = e.NewItem;
                        var view = transform(value);
                        var id = identitySelector(value);
                        list.Add((view, id), (value, view));
                        viewMap.Add(id, view);

                        var index = list.IndexOfKey((view, id));
                        filter.InvokeOnReplace(value, view, oldValue, oldView!, index, oldIndex);
                        break;
                    }
                    case NotifyCollectionChangedAction.Move:
                        // Move(index change) does not affect soreted dict.
                    {
                        var value = e.OldItem;
                        var id = identitySelector(value);
                        if (viewMap.TryGetValue(id, out var view))
                        {
                            var index = list.IndexOfKey((view, id));
                            filter.InvokeOnMove(value, view, index, index);
                        }
                        break;
                    }
                    case NotifyCollectionChangedAction.Reset:
                        list.Clear();
                        viewMap.Clear();
                        filter.InvokeOnReset();
                        break;
                    default:
                        break;
                }

                base.SourceCollectionChanged(e);
            }
        }

        sealed class Comparer : IComparer<(TView view, TKey id)>
        {
            readonly IComparer<TView> comparer;

            public Comparer(IComparer<TView> comparer)
            {
                this.comparer = comparer;
            }

            public int Compare((TView view, TKey id) x, (TView view, TKey id) y)
            {
                var compare = comparer.Compare(x.view, y.view);
                if (compare == 0)
                {
                    compare = Comparer<TKey>.Default.Compare(x.id, y.id);
                }

                return compare;
            }
        }
    }
}