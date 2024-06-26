using System;
using System.Collections.Specialized;
using System.Linq;
using ObservableCollections.Sync;

namespace ObservableCollections;

public sealed partial class ObservableRingBuffer<T>
{
    public ISynchronizedView<T, TView> CreateView<TView>(Func<T, TView> transform)
    {
        return new View<TView>(this, transform);
    }

    // used with ObservableFixedSizeRingBuffer
    internal sealed class View<TView> : SynchronizedCollectionView<T, TView, RingBuffer<(T, TView)>>
    {
        private readonly Func<T, TView> selector;

        public View(IObservableCollection<T> source, Func<T, TView> selector)
            : base(source, new RingBuffer<(T, TView)>(source.Select(x => (x, selector(x)))))
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
                        // can not distinguish AddFirst and AddLast when collection count is 0.
                        // So, in that case, use AddLast.
                        // The internal structure may be different from the parent, but the result is same.
                        // RangeOperation is only exists AddLastRange because we can not distinguish FirstRange or LastRange.
                        if (e.NewStartingIndex == 0 && View.Count != 0)
                        {
                            // AddFirst
                            if (e.IsSingleItem)
                            {
                                var v = (e.NewItem, selector(e.NewItem));
                                View.AddFirst(v);
                                filter.InvokeOnAdd(v, 0);
                            }
                            else
                            {
                                foreach (var item in e.NewItems)
                                {
                                    var v = (item, selector(item));
                                    View.AddFirst(v);
                                    filter.InvokeOnAdd(v, 0);
                                }
                            }
                        }
                        else
                        {
                            // AddLast
                            if (e.IsSingleItem)
                            {
                                var v = (e.NewItem, selector(e.NewItem));
                                View.AddLast(v);
                                filter.InvokeOnAdd(v, View.Count - 1);
                            }
                            else
                            {
                                foreach (var item in e.NewItems)
                                {
                                    var v = (item, selector(item));
                                    View.AddLast(v);
                                    filter.InvokeOnAdd(v, View.Count - 1);
                                }
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Remove:
                        // starting from 0 is RemoveFirst
                        if (e.OldStartingIndex == 0)
                        {
                            // RemoveFirst
                            if (e.IsSingleItem)
                            {
                                var v = View.RemoveFirst();
                                filter.InvokeOnRemove(v, 0);
                            }
                            else
                            {
                                for (var i = 0; i < e.OldItems.Length; i++)
                                {
                                    var v = View.RemoveFirst();
                                    filter.InvokeOnRemove(v, 0);
                                }
                            }
                        }
                        else
                        {
                            // RemoveLast
                            if (e.IsSingleItem)
                            {
                                var index = View.Count - 1;
                                var v = View.RemoveLast();
                                filter.InvokeOnRemove(v, index);
                            }
                            else
                            {
                                for (var i = 0; i < e.OldItems.Length; i++)
                                {
                                    var index = View.Count - 1;
                                    var v = View.RemoveLast();
                                    filter.InvokeOnRemove(v, index);
                                }
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Reset:
                        View.Clear();
                        filter.InvokeOnReset();
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        // range is not supported
                    {
                        var ov = View[e.OldStartingIndex];
                        var v = (e.NewItem, selector(e.NewItem));
                        View[e.NewStartingIndex] = v;
                        filter.InvokeOnReplace(v, ov, e.NewStartingIndex);
                        break;
                    }
                    case NotifyCollectionChangedAction.Move:
                    default:
                        break;
                }

                base.SourceCollectionChanged(e);
            }
        }
    }
}