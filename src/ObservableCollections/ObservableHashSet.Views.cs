using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ObservableCollections.Sync;

namespace ObservableCollections;

public sealed partial class ObservableHashSet<T> : IObservableCollection<T>
{
    public ISynchronizedView<T, TView> CreateView<TView>(Func<T, TView> transform)
    {
        return new View<TView>(this, transform);
    }

    private sealed class View<TView> : SynchronizedViewBase<T, TView>
    {
        private readonly Dictionary<T, (T, TView)> _dict;
        private readonly Func<T, TView> _selector;

        public View(ObservableHashSet<T> source, Func<T, TView> selector) : base(source)
        {
            this._selector = selector;
            lock (source.SyncRoot)
            {
                _dict = source.Source.ToDictionary(x => x, x => (x, selector(x)));
            }
        }

        public override int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return _dict.Count;
                }
            }
        }

        public override void AttachFilter(ISynchronizedViewFilter<T, TView> filter,
            bool invokeAddEventForCurrentElements = false)
        {
            lock (SyncRoot)
            {
                this.Filter = filter;
                foreach (var (_, (value, view)) in _dict)
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
                Filter = SynchronizedViewFilter<T, TView>._null;
                if (resetAction != null)
                    foreach (var (_, (value, view)) in _dict)
                        resetAction(value, view);
            }
        }

        public override IEnumerator<(T, TView)> GetEnumerator()
        {
            lock (SyncRoot)
            {
                foreach (var item in _dict)
                    if (Filter.IsMatch(item.Value.Item1, item.Value.Item2))
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
                            var v = (e.NewItem, _selector(e.NewItem));
                            _dict.Add(e.NewItem, v);
                            Filter.InvokeOnAdd(v, -1);
                        }
                        else
                        {
                            var i = e.NewStartingIndex;
                            foreach (var item in e.NewItems)
                            {
                                var v = (item, _selector(item));
                                _dict.Add(item, v);
                                Filter.InvokeOnAdd(v, i++);
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Remove:
                        if (e.IsSingleItem)
                        {
                            if (_dict.Remove(e.OldItem, out var value)) Filter.InvokeOnRemove(value, -1);
                        }
                        else
                        {
                            foreach (var item in e.OldItems)
                                if (_dict.Remove(item, out var value))
                                    Filter.InvokeOnRemove(value, -1);
                        }

                        break;
                    case NotifyCollectionChangedAction.Reset:
                        _dict.Clear();
                        Filter.InvokeOnReset();
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