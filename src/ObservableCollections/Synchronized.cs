namespace ObservableCollections
{
    public class Synchronized : ISynchronized
    {
        public object SyncRoot { get; } = new object();
    }
}
