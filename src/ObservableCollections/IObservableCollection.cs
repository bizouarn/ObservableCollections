using ObservableCollections.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using ObservableCollections.Comp;

namespace ObservableCollections
{
    public delegate void NotifyCollectionChangedEventHandler<T>(in NotifyCollectionChangedEventArgs<T> e);

    public interface IObservableCollection<T> : ISynchronized, IReadOnlyCollection<T>
    {
        event NotifyCollectionChangedEventHandler<T>? CollectionChanged;
        ISynchronizedView<T, TView> CreateView<TView>(Func<T, TView> transform, bool reverse = false);
    }

    public interface IReadOnlyObservableDictionary<TKey, TValue> : 
        IReadOnlyDictionary<TKey, TValue>, IObservableCollection<KeyValuePair<TKey, TValue>>
    {
    }
    
    public interface IFreezedCollection<T>
    {
        ISynchronizedView<T, TView> CreateView<TView>(Func<T, TView> transform, bool reverse = false);
        ISortableSynchronizedView<T, TView> CreateSortableView<TView>(Func<T, TView> transform);
    }
    
    // will be implemented in the future?
    //public interface IGroupedSynchoronizedView<T, TKey, TView> : ILookup<TKey, (T, TView)>, ISynchronizedView<T, TView>
    //{
    //}

    public interface INotifyCollectionChangedSynchronizedView<out TView> : IReadOnlyCollection<TView>, INotifyCollectionChanged, INotifyPropertyChanged, IDisposable
    {
    }

    public static class ObservableCollectionsExtensions
    {
        public static ISynchronizedView<T, TView> CreateSortedView<T, TKey, TView>(this IObservableCollection<T> source, Func<T, TKey> identitySelector, Func<T, TView> transform, IComparer<T> comparer)
            where TKey : notnull
        {
            return new SortedView<T, TKey, TView>(source, identitySelector, transform, comparer);
        }

        public static ISynchronizedView<T, TView> CreateSortedView<T, TKey, TView>(this IObservableCollection<T> source, Func<T, TKey> identitySelector, Func<T, TView> transform, IComparer<TView> viewComparer)
            where TKey : notnull
        {
            return new SortedViewViewComparer<T, TKey, TView>(source, identitySelector, transform, viewComparer);
        }

        public static ISynchronizedView<T, TView> CreateSortedView<T, TKey, TView, TCompare>(this IObservableCollection<T> source, Func<T, TKey> identitySelector, Func<T, TView> transform, Func<T, TCompare> compareSelector, bool ascending = true)
            where TKey : notnull
        {
            return source.CreateSortedView(identitySelector, transform, new AnonymousComparer<T, TCompare>(compareSelector, ascending));
        }

        public static ISortableSynchronizedView<T, TView> CreateSortableView<T, TView>(this IFreezedCollection<T> source, Func<T, TView> transform, IComparer<T> initialSort)
        {
            var view = source.CreateSortableView(transform);
            view.Sort(initialSort);
            return view;
        }

        public static ISortableSynchronizedView<T, TView> CreateSortableView<T, TView>(this IFreezedCollection<T> source, Func<T, TView> transform, IComparer<TView> initialViewSort)
        {
            var view = source.CreateSortableView(transform);
            view.Sort(initialViewSort);
            return view;
        }

        public static ISortableSynchronizedView<T, TView> CreateSortableView<T, TView, TCompare>(this IFreezedCollection<T> source, Func<T, TView> transform, Func<T, TCompare> initialCompareSelector, bool ascending = true)
        {
            var view = source.CreateSortableView(transform);
            view.Sort(initialCompareSelector, ascending);
            return view;
        }

        public static void Sort<T, TView, TCompare>(this ISortableSynchronizedView<T, TView> source, Func<T, TCompare> compareSelector, bool ascending = true)
        {
            source.Sort(new AnonymousComparer<T, TCompare>(compareSelector, ascending));
        }
    }
}