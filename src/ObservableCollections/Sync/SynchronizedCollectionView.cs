using System;
using System.Collections.Generic;
namespace ObservableCollections.Sync;

public abstract class SynchronizedCollectionView<T, TView, TList> : SynchronizedViewBase<T, TView> where TList : IReadOnlyCollection<(T, TView)>
{
    protected readonly TList View;

    public SynchronizedCollectionView(IObservableCollection<T> source, TList list) : base(source)
    {
        lock (SyncRoot)
        {
            View = list;
        }
    }

    public override void AttachFilter(ISynchronizedViewFilter<T, TView> filter,
        bool invokeAddEventForCurrentElements = false)
    {
        lock (SyncRoot)
        {
            this.filter = filter;
            var i = 0;
            foreach (var (value, view) in View)
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
                foreach (var (item, view) in View)
                    resetAction(item, view);
        }
    }

    public override IEnumerator<(T, TView)> GetEnumerator()
    {
        lock (SyncRoot)
        {
            foreach (var item in View)
                if (filter.IsMatch(item.Item1, item.Item2))
                    yield return item;
        }
    }

    public override int Count
    {
        get
        {
            lock (SyncRoot)
            {
                return View.Count;
            }
        }
    }
}