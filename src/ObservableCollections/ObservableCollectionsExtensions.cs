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
}