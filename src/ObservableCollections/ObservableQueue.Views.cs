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

    private class View<TView> : SynchronizedCollectionView<T, TView, Queue<(T, TView)>>
    {
        private readonly Func<T, TView> selector;

        public View(ObservableQueue<T> source, Func<T, TView> selector)
            : base(source, new Queue<(T, TView)>(source.Source.Select(x => (x, selector(x)))))
        {
            this.selector = selector;
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
                            View.Enqueue(v);
                            filter.InvokeOnAdd(v, e.NewStartingIndex);
                        }
                        else
                        {
                            var i = e.NewStartingIndex;
                            foreach (var item in e.NewItems)
                            {
                                var v = (item, selector(item));
                                View.Enqueue(v);
                                filter.InvokeOnAdd(v, i++);
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Remove:
                        // Dequeue, DequeuRange
                        if (e.IsSingleItem)
                        {
                            var v = View.Dequeue();
                            filter.InvokeOnRemove(v.Item1, v.Item2, 0);
                        }
                        else
                        {
                            var len = e.OldItems.Length;
                            for (var i = 0; i < len; i++)
                            {
                                var v = View.Dequeue();
                                filter.InvokeOnRemove(v.Item1, v.Item2, 0);
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Reset:
                        View.Clear();
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