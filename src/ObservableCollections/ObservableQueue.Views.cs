using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ObservableCollections.Sync;

namespace ObservableCollections;

public sealed partial class ObservableQueue<T> : IObservableCollection<T>
{
    public ISynchronizedView<T, TView> CreateView<TView>(Func<T, TView> transform)
    {
        return new View<TView>(this, transform);
    }

    private class View<TView> : SynchronizedViewBase<T, TView>
    {
        private readonly Func<T, TView> selector;
        private readonly Queue<(T, TView)> queue;

        public View(ObservableQueue<T> source, Func<T, TView> selector) : base(source)
        {
            this.selector = selector;
            lock (source.SyncRoot)
            {
                queue = new Queue<(T, TView)>(source.Source.Select(x => (x, selector(x))));
            }
        }

        public override int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return queue.Count;
                }
            }
        }

        public override void AttachFilter(ISynchronizedViewFilter<T, TView> filter,
            bool invokeAddEventForCurrentElements = false)
        {
            lock (SyncRoot)
            {
                this.filter = filter;
                var i = 0;
                foreach (var (value, view) in queue)
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
                filter = SynchronizedViewFilter<T, TView>.Null;
                if (resetAction != null)
                    foreach (var (item, view) in queue)
                        resetAction(item, view);
            }
        }

        public override IEnumerator<(T, TView)> GetEnumerator()
        {
            lock (SyncRoot)
            {
                foreach (var item in queue)
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
                        // Add(Enqueue, EnqueueRange)
                        if (e.IsSingleItem)
                        {
                            var v = (e.NewItem, selector(e.NewItem));
                            queue.Enqueue(v);
                            filter.InvokeOnAdd(v, e.NewStartingIndex);
                        }
                        else
                        {
                            var i = e.NewStartingIndex;
                            foreach (var item in e.NewItems)
                            {
                                var v = (item, selector(item));
                                queue.Enqueue(v);
                                filter.InvokeOnAdd(v, i++);
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Remove:
                        // Dequeue, DequeuRange
                        if (e.IsSingleItem)
                        {
                            var v = queue.Dequeue();
                            filter.InvokeOnRemove(v.Item1, v.Item2, 0);
                        }
                        else
                        {
                            var len = e.OldItems.Length;
                            for (var i = 0; i < len; i++)
                            {
                                var v = queue.Dequeue();
                                filter.InvokeOnRemove(v.Item1, v.Item2, 0);
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Reset:
                        queue.Clear();
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