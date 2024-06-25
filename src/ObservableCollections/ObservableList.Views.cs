using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace ObservableCollections;

public sealed partial class ObservableList<T> : IList<T>, IReadOnlyList<T>, IObservableCollection<T>
{
    public ISynchronizedView<T, TView> CreateView<TView>(Func<T, TView> transform, bool reverse = false)
    {
        return new View<TView>(this, transform, reverse);
    }

    private sealed class View<TView> : SynchronizedView<T, TView>
    {
        private readonly List<(T, TView)> list;
        private readonly bool reverse;

        public View(ObservableList<T> source, Func<T, TView> selector, bool reverse) : base(source, selector)
        {
            this.reverse = reverse;
            lock (source.SyncRoot)
            {
                list = source.list.Select(x => (x, selector(x))).ToList();
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

        public override void AttachFilter(ISynchronizedViewFilter<T, TView> filter,
            bool invokeAddEventForCurrentElements = false)
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

        public override void ResetFilter(Action<T, TView>? resetAction)
        {
            lock (SyncRoot)
            {
                filter = SynchronizedViewFilter<T, TView>.Null;
                if (resetAction != null)
                    foreach (var (item, view) in list)
                        resetAction(item, view);
            }
        }

        public override IEnumerator<(T, TView)> GetEnumerator()
        {
            lock (SyncRoot)
            {
                foreach (var item in reverse ? list.AsEnumerable().Reverse() : list)
                    if (filter.IsMatch(item.Item1, item.Item2))
                        yield return item;
            }
        }

        protected override void SourceCollectionChanged(in NotifyCollectionChangedEventArgs<T> e)
        {
            lock (SyncRoot)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        // Add
                        if (e.NewStartingIndex == list.Count)
                        {
                            if (e.IsSingleItem)
                            {
                                var v = (e.NewItem, selector(e.NewItem));
                                list.Add(v);
                                filter.InvokeOnAdd(v, e.NewStartingIndex);
                            }
                            else
                            {
                                var i = e.NewStartingIndex;
                                foreach (var item in e.NewItems)
                                {
                                    var v = (item, selector(item));
                                    list.Add(v);
                                    filter.InvokeOnAdd(v, i++);
                                }
                            }
                        }
                        // Insert
                        else
                        {
                            if (e.IsSingleItem)
                            {
                                var v = (e.NewItem, selector(e.NewItem));
                                list.Insert(e.NewStartingIndex, v);
                                filter.InvokeOnAdd(v, e.NewStartingIndex);
                            }
                            else
                            {
                                // inefficient copy, need refactoring
                                var newArray = new (T, TView)[e.NewItems.Length];
                                var span = e.NewItems;
                                for (var i = 0; i < span.Length; i++)
                                {
                                    var v = (span[i], selector(span[i]));
                                    newArray[i] = v;
                                    filter.InvokeOnAdd(v, e.NewStartingIndex + i);
                                }

                                list.InsertRange(e.NewStartingIndex, newArray);
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Remove:
                        if (e.IsSingleItem)
                        {
                            var v = list[e.OldStartingIndex];
                            list.RemoveAt(e.OldStartingIndex);
                            filter.InvokeOnRemove(v, e.OldStartingIndex);
                        }
                        else
                        {
                            var len = e.OldStartingIndex + e.OldItems.Length;
                            for (var i = e.OldStartingIndex; i < len; i++)
                            {
                                var v = list[i];
                                filter.InvokeOnRemove(v, e.OldStartingIndex + i);
                            }

                            list.RemoveRange(e.OldStartingIndex, e.OldItems.Length);
                        }

                        break;
                    case NotifyCollectionChangedAction.Replace:
                        // ObservableList does not support replace range
                    {
                        var v = (e.NewItem, selector(e.NewItem));
                        var ov = (e.OldItem, list[e.OldStartingIndex].Item2);
                        list[e.NewStartingIndex] = v;
                        filter.InvokeOnReplace(v, ov, e.NewStartingIndex);
                        break;
                    }
                    case NotifyCollectionChangedAction.Move:
                    {
                        var removeItem = list[e.OldStartingIndex];
                        list.RemoveAt(e.OldStartingIndex);
                        list.Insert(e.NewStartingIndex, removeItem);

                        filter.InvokeOnMove(removeItem, e.NewStartingIndex, e.OldStartingIndex);
                    }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        list.Clear();
                        filter.InvokeOnReset();
                        break;
                }

                base.SourceCollectionChanged(e);
            }
        }
    }
}