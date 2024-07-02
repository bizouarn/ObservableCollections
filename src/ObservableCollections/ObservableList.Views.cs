using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ObservableCollections.Sync;

namespace ObservableCollections;

public sealed partial class ObservableList<T> : IObservableCollection<T>
{
    public ISynchronizedView<T, TView> CreateView<TView>(Func<T, TView> transform)
    {
        return new View<TView>(this, transform);
    }

    private sealed class View<TView> : SynchronizedCollectionView<T, TView, List<(T, TView)>>
    {
        private readonly Func<T, TView> _selector;

        public View(ObservableList<T> source, Func<T, TView> selector)
            : base(source, source.Source.Select(x => (x, selector(x))).ToList())
        {
            this._selector = selector;
        }

        protected override void SourceCollectionChanged(in NotifyCollectionChangedEventArgs<T> e)
        {
            lock (SyncRoot)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        // Add
                        if (e.NewStartingIndex == View.Count)
                        {
                            if (e.IsSingleItem)
                            {
                                var v = (e.NewItem, _selector(e.NewItem));
                                View.Add(v);
                                Filter.InvokeOnAdd(v, e.NewStartingIndex);
                            }
                            else
                            {
                                var i = e.NewStartingIndex;
                                foreach (var item in e.NewItems)
                                {
                                    var v = (item, _selector(item));
                                    View.Add(v);
                                    Filter.InvokeOnAdd(v, i++);
                                }
                            }
                        }
                        // Insert
                        else
                        {
                            if (e.IsSingleItem)
                            {
                                var v = (e.NewItem, _selector(e.NewItem));
                                View.Insert(e.NewStartingIndex, v);
                                Filter.InvokeOnAdd(v, e.NewStartingIndex);
                            }
                            else
                            {
                                // inefficient copy, need refactoring
                                var newArray = new (T, TView)[e.NewItems.Length];
                                var span = e.NewItems;
                                for (var i = 0; i < span.Length; i++)
                                {
                                    var v = (span[i], _selector(span[i]));
                                    newArray[i] = v;
                                    Filter.InvokeOnAdd(v, e.NewStartingIndex + i);
                                }

                                View.InsertRange(e.NewStartingIndex, newArray);
                            }
                        }

                        break;
                    case NotifyCollectionChangedAction.Remove:
                        if (e.IsSingleItem)
                        {
                            var v = View[e.OldStartingIndex];
                            View.RemoveAt(e.OldStartingIndex);
                            Filter.InvokeOnRemove(v, e.OldStartingIndex);
                        }
                        else
                        {
                            var len = e.OldStartingIndex + e.OldItems.Length;
                            for (var i = e.OldStartingIndex; i < len; i++)
                            {
                                var v = View[i];
                                Filter.InvokeOnRemove(v, e.OldStartingIndex + i);
                            }

                            View.RemoveRange(e.OldStartingIndex, e.OldItems.Length);
                        }

                        break;
                    case NotifyCollectionChangedAction.Replace:
                        // ObservableList does not support replace range
                    {
                        var v = (e.NewItem, _selector(e.NewItem));
                        var ov = (e.OldItem, View[e.OldStartingIndex].Item2);
                        View[e.NewStartingIndex] = v;
                        Filter.InvokeOnReplace(v, ov, e.NewStartingIndex);
                        break;
                    }
                    case NotifyCollectionChangedAction.Move:
                    {
                        var removeItem = View[e.OldStartingIndex];
                        View.RemoveAt(e.OldStartingIndex);
                        View.Insert(e.NewStartingIndex, removeItem);

                        Filter.InvokeOnMove(removeItem, e.NewStartingIndex, e.OldStartingIndex);
                    }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        View.Clear();
                        Filter.InvokeOnReset();
                        break;
                }

                base.SourceCollectionChanged(e);
            }
        }
    }
}