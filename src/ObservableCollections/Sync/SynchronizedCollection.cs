using System.Collections;
using System.Collections.Generic;

namespace ObservableCollections;

public abstract class SynchronizedCollection<T, TCol> : Synchronized, IReadOnlyCollection<T>, IEnumerable<T> where TCol : IEnumerable<T>, IReadOnlyCollection<T>
{
    protected TCol Source { get; set; }

    /// <summary>Returns an enumerator that iterates through the collection.</summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<T> GetEnumerator() => Source.GetEnumerator();

    /// <summary>Returns an enumerator that iterates through a collection.</summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the
    ///     collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Gets the number of elements in the collection.</summary>
    /// <returns>The number of elements in the collection.</returns>
    public int Count
    {
        get
        {
            lock (SyncRoot)
            {
                return Source.Count;
            }
        }
    }
}