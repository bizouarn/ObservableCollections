using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ObservableCollections.Sync;

namespace ObservableCollections;

public sealed partial class ObservableHashSet<T> : IReadOnlyCollection<T>, IObservableCollection<T>
{
    public ISynchronizedView<T, TView> CreateView<TView>(Func<T, TView> transform, bool _ = false)
    {
        return new View<TView>(this, transform);
    }

    private sealed class View<TView> : SynchronizedViewBase<T, TView>
    {
        protected readonly Func<T, TView> selector;
        private readonly Dictionary<T, (T, TView)> dict;

        public View(ObservableHashSet<T> source, Func<T, TView> selector) : base(source)
        {
            this.selector = selector;
            lock (source.SyncRoot)
            {
                dict = source.set.ToDictionary(x => x, x => (x, selector(x)));
            }
        }

        public override int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return dict.Count;
                }
            }
        }

        public override void AttachFilter(ISynchronizedViewFilter<T, TView> filter,
            bool invokeAddEventForCurrentElements = false)
        {
            lock (SyncRoot)
            {
                this.filter = filter;
                foreach (var (_, (value, view)) in dict)
                    if (invokeAddEventForCurrentElements)
                        filter.InvokeOnAdd((value, view), -1);
                    else
                        filter.InvokeOnAttach(value, view);
            }
        }

        public override void ResetFilter(Action<T, TView>? resetAction)
        {
            lock (SyncRoot)
            {
                filter = SynchronizedViewFilter<T, TView>.Null;
                if (resetAction != null)
                    foreach (var (_, (value, view)) in dict)
                        resetAction(value, view);
            }
        }

        public override IEnumerator<(T, TView)> GetEnumerator()
        {
            lock (SyncRoot)
            {
                foreach (var item in dict)
                    if (filter.IsMatch(item.Value.Item1, item.Value.Item2))
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
                        if (e.IsSingleItem)
                        {
                            var v = (e.NewItem, selector(e.NewItem));
                            dict.Add(e.NewItem, v);
                            filter.InvokeOnAdd(v, -1);
                        }
                        else
                        {
                            var i = e.NewStartingIndex;
                            foreach (var item in e.NewItems)
                            {
                                var v = (item, selector(item));
                                dict.Add(item, v);
                                filter.InvokeOnAdd(v, i++);
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Remove:
                        if (e.IsSingleItem)
                        {
                            if (dict.Remove(e.OldItem, out var value)) filter.InvokeOnRemove(value, -1);
                        }
                        else
                        {
                            foreach (var item in e.OldItems)
                                if (dict.Remove(item, out var value))
                                    filter.InvokeOnRemove(value, -1);
                        }

                        break;
                    case NotifyCollectionChangedAction.Reset:
                        dict.Clear();
                        filter.InvokeOnReset();
                        break;
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Move:
                    default:
                        break;
                }

                base.SourceCollectionChanged(e);
            }
        }
    }
}