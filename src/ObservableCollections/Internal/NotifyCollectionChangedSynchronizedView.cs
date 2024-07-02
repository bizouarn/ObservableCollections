using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ObservableCollections.Internal;

internal class NotifyCollectionChangedSynchronizedView<T, TView> :
    INotifyCollectionChangedSynchronizedView<TView>,
    ISynchronizedViewFilter<T, TView>
{
    private static readonly PropertyChangedEventArgs _countPropertyChangedEventArgs = new("Count");
    private readonly ISynchronizedViewFilter<T, TView> _currentFilter;

    private readonly ISynchronizedView<T, TView> _parent;

    public NotifyCollectionChangedSynchronizedView(ISynchronizedView<T, TView> parent)
    {
        this._parent = parent;
        _currentFilter = parent.CurrentFilter;
        parent.AttachFilter(this);
    }

    public int Count => _parent.Count;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose()
    {
        _parent.Dispose();
    }

    public IEnumerator<TView> GetEnumerator()
    {
        foreach (var (_, view) in _parent) yield return view;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool IsMatch(T value, TView view)
    {
        return _currentFilter.IsMatch(value, view);
    }

    public void WhenTrue(T value, TView view)
    {
        _currentFilter.WhenTrue(value, view);
    }

    public void WhenFalse(T value, TView view)
    {
        _currentFilter.WhenFalse(value, view);
    }

    public void OnCollectionChanged(in SynchronizedViewChangedEventArgs<T, TView> args)
    {
        _currentFilter.OnCollectionChanged(args);

        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Add:
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, args.NewView,
                        args.NewViewIndex));
                PropertyChanged?.Invoke(this, _countPropertyChangedEventArgs);
                break;
            case NotifyCollectionChangedAction.Remove:
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, args.OldView,
                        args.OldViewIndex));
                PropertyChanged?.Invoke(this, _countPropertyChangedEventArgs);
                break;
            case NotifyCollectionChangedAction.Reset:
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                PropertyChanged?.Invoke(this, _countPropertyChangedEventArgs);
                break;
            case NotifyCollectionChangedAction.Replace:
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, args.NewView,
                        args.OldView, args.NewViewIndex));
                break;
            case NotifyCollectionChangedAction.Move:
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, args.NewView,
                        args.NewViewIndex, args.OldViewIndex));
                break;
        }
    }

    public event Action<NotifyCollectionChangedAction>? CollectionStateChanged
    {
        add => _parent.CollectionStateChanged += value;
        remove => _parent.CollectionStateChanged -= value;
    }

    public event NotifyCollectionChangedEventHandler<T>? RoutingCollectionChanged
    {
        add => _parent.RoutingCollectionChanged += value;
        remove => _parent.RoutingCollectionChanged -= value;
    }
}