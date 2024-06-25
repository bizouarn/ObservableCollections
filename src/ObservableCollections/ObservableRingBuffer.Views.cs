using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace ObservableCollections;

public sealed partial class ObservableRingBuffer<T>
{
    public ISynchronizedView<T, TView> CreateView<TView>(Func<T, TView> transform, bool reverse = false)
    {
        return new View<TView>(this, transform, reverse);
    }

    // used with ObservableFixedSizeRingBuffer
    internal sealed class View<TView> : SynchronizedViewBase<T, TView>
    {
        protected readonly Func<T, TView> selector;
        private readonly bool reverse;
        private readonly RingBuffer<(T, TView)> ringBuffer;

        public View(IObservableCollection<T> source, Func<T, TView> selector, bool reverse) : base(source)
        {
            this.selector = selector;
            this.reverse = reverse;
            lock (source.SyncRoot)
            {
                ringBuffer = new RingBuffer<(T, TView)>(source.Select(x => (x, selector(x))));
            }
        }

        public override int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return ringBuffer.Count;
                }
            }
        }

        public override void AttachFilter(ISynchronizedViewFilter<T, TView> filter,
            bool invokeAddEventForCurrentElements = false)
        {
            lock (SyncRoot)
            {
                this.filter = filter;
                for (var i = 0; i < ringBuffer.Count; i++)
                {
                    var (value, view) = ringBuffer[i];
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
                    foreach (var (item, view) in ringBuffer)
                        resetAction(item, view);
            }
        }

        public override IEnumerator<(T, TView)> GetEnumerator()
        {
            lock (SyncRoot)
            {
                foreach (var item in reverse ? ringBuffer.AsEnumerable().Reverse() : ringBuffer)
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
                        // can not distinguish AddFirst and AddLast when collection count is 0.
                        // So, in that case, use AddLast.
                        // The internal structure may be different from the parent, but the result is same.
                        // RangeOperation is only exists AddLastRange because we can not distinguish FirstRange or LastRange.
                        if (e.NewStartingIndex == 0 && ringBuffer.Count != 0)
                        {
                            // AddFirst
                            if (e.IsSingleItem)
                            {
                                var v = (e.NewItem, selector(e.NewItem));
                                ringBuffer.AddFirst(v);
                                filter.InvokeOnAdd(v, 0);
                            }
                            else
                            {
                                foreach (var item in e.NewItems)
                                {
                                    var v = (item, selector(item));
                                    ringBuffer.AddFirst(v);
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
                                ringBuffer.AddLast(v);
                                filter.InvokeOnAdd(v, ringBuffer.Count - 1);
                            }
                            else
                            {
                                foreach (var item in e.NewItems)
                                {
                                    var v = (item, selector(item));
                                    ringBuffer.AddLast(v);
                                    filter.InvokeOnAdd(v, ringBuffer.Count - 1);
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
                                var v = ringBuffer.RemoveFirst();
                                filter.InvokeOnRemove(v, 0);
                            }
                            else
                            {
                                for (var i = 0; i < e.OldItems.Length; i++)
                                {
                                    var v = ringBuffer.RemoveFirst();
                                    filter.InvokeOnRemove(v, 0);
                                }
                            }
                        }
                        else
                        {
                            // RemoveLast
                            if (e.IsSingleItem)
                            {
                                var index = ringBuffer.Count - 1;
                                var v = ringBuffer.RemoveLast();
                                filter.InvokeOnRemove(v, index);
                            }
                            else
                            {
                                for (var i = 0; i < e.OldItems.Length; i++)
                                {
                                    var index = ringBuffer.Count - 1;
                                    var v = ringBuffer.RemoveLast();
                                    filter.InvokeOnRemove(v, index);
                                }
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Reset:
                        ringBuffer.Clear();
                        filter.InvokeOnReset();
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        // range is not supported
                    {
                        var ov = ringBuffer[e.OldStartingIndex];
                        var v = (e.NewItem, selector(e.NewItem));
                        ringBuffer[e.NewStartingIndex] = v;
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