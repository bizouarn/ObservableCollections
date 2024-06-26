using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ObservableCollections;

public abstract class SynchronizedCollection<TCol, TSub> : Synchronized, IReadOnlyCollection<TSub>
    where TCol : class, IEnumerable<TSub>, IReadOnlyCollection<TSub>
{
    protected TCol Source { get; set; }

    /// <summary>Returns an enumerator that iterates through the collection.</summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<TSub> GetEnumerator()
    {
        foreach (var item in Source)
        {
            yield return item;
        }
    }

    /// <summary>Returns an enumerator that iterates through a collection.</summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the
    ///     collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

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

    public TSub[] ToArray()
    {
        lock (SyncRoot)
        {
            return Source.ToArray();
        }
    }
}