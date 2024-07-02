using System;
using System.Collections.Generic;
using ObservableCollections.Comp;
using ObservableCollections.Internal;

namespace ObservableCollections;

public static class ObservableCollectionsExtensions
{
    public static ISynchronizedView<T, T> CreateView<T>(this IObservableCollection<T> source)
    {
        return source.CreateView(static x => x);
    }

    public static INotifyCollectionChangedSynchronizedView<T> ToNotifyCollectionChanged<T>(this IObservableCollection<T> source)
    {
        return source.CreateView(static x => x).ToNotifyCollectionChanged();
    }

    public static ISynchronizedView<T, TView> CreateSortedView<T, TKey, TView>(this IObservableCollection<T> source,
        Func<T, TKey> identitySelector, Func<T, TView> transform, IComparer<T> comparer)
        where TKey : notnull
    {
        return new SortedView<T, TKey, TView>(source, identitySelector, transform, comparer);
    }

    public static ISynchronizedView<T, TView> CreateSortedView<T, TKey, TView>(this IObservableCollection<T> source,
        Func<T, TKey> identitySelector, Func<T, TView> transform, IComparer<TView> viewComparer)
        where TKey : notnull
    {
        return new SortedViewViewComparer<T, TKey, TView>(source, identitySelector, transform, viewComparer);
    }

    public static ISynchronizedView<T, TView> CreateSortedView<T, TKey, TView, TCompare>(
        this IObservableCollection<T> source, Func<T, TKey> identitySelector, Func<T, TView> transform,
        Func<T, TCompare> compareSelector, bool ascending = true)
        where TKey : notnull
    {
        return source.CreateSortedView(identitySelector, transform,
            new AnonymousComparer<T, TCompare>(compareSelector, ascending));
    }

    public static ISortableSynchronizedView<T, TView> CreateSortableView<T, TView>(this IFreezedCollection<T> source,
        Func<T, TView> transform, IComparer<T> initialSort)
    {
        var view = source.CreateSortableView(transform);
        view.Sort(initialSort);
        return view;
    }

    public static ISortableSynchronizedView<T, TView> CreateSortableView<T, TView>(this IFreezedCollection<T> source,
        Func<T, TView> transform, IComparer<TView> initialViewSort)
    {
        var view = source.CreateSortableView(transform);
        view.Sort(initialViewSort);
        return view;
    }

    public static ISortableSynchronizedView<T, TView> CreateSortableView<T, TView, TCompare>(
        this IFreezedCollection<T> source, Func<T, TView> transform, Func<T, TCompare> initialCompareSelector,
        bool ascending = true)
    {
        var view = source.CreateSortableView(transform);
        view.Sort(initialCompareSelector, ascending);
        return view;
    }

    public static void Sort<T, TView, TCompare>(this ISortableSynchronizedView<T, TView> source,
        Func<T, TCompare> compareSelector, bool ascending = true)
    {
        source.Sort(new AnonymousComparer<T, TCompare>(compareSelector, ascending));
    }
}