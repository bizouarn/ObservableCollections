using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ObservableCollections.Sync;

namespace ObservableCollections;

public sealed partial class ObservableStack<T> : IObservableCollection<T>
{
    public ISynchronizedView<T, TView> CreateView<TView>(Func<T, TView> transform)
    {
        return new View<TView>(this, transform);
    }

    private class View<TView> : SynchronizedCollectionView<T, TView, Stack<(T, TView)>>
    {
        private readonly Func<T, TView> _selector;

        public View(ObservableStack<T> source, Func<T, TView> selector)
            : base(source, new Stack<(T, TView)>(source.Source.Select(x => (x, selector(x)))))
        {
            this._selector = selector;
        }

        public override void AttachFilter(ISynchronizedViewFilter<T, TView> filter,
            bool invokeAddEventForCurrentElements = false)
        {
            lock (SyncRoot)
            {
                this.Filter = filter;
                foreach (var (value, view) in View)
                    if (invokeAddEventForCurrentElements)
                        filter.InvokeOnAdd(value, view, 0);
                    else
                        filter.InvokeOnAttach(value, view);
            }
        }

        protected override void SourceCollectionChanged(in NotifyCollectionChangedEventArgs<T> e)
        {
            lock (SyncRoot)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        // Add(Push, PushRange)
                        if (e.IsSingleItem)
                        {
                            var v = (e.NewItem, _selector(e.NewItem));
                            View.Push(v);
                            Filter.InvokeOnAdd(v, 0);
                        }
                        else
                        {
                            foreach (var item in e.NewItems)
                            {
                                var v = (item, _selector(item));
                                View.Push(v);
                                Filter.InvokeOnAdd(v, 0);
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Remove:
                        // Pop, PopRange
                        if (e.IsSingleItem)
                        {
                            var v = View.Pop();
                            Filter.InvokeOnRemove(v.Item1, v.Item2, 0);
                        }
                        else
                        {
                            var len = e.OldItems.Length;
                            for (var i = 0; i < len; i++)
                            {
                                var v = View.Pop();
                                Filter.InvokeOnRemove(v.Item1, v.Item2, 0);
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Reset:
                        View.Clear();
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