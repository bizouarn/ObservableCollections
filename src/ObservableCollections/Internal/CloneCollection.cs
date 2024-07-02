using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ObservableCollections.Internal;

/// <summary>
///     ReadOnly cloned collection.
/// </summary>
internal struct CloneCollection<T> : IDisposable
{
    private T[]? _array;
    private readonly int _length;

    public ReadOnlySpan<T> Span => _array.AsSpan(0, _length);

    public IEnumerable<T> AsEnumerable()
    {
        return new EnumerableCollection(_array, _length);
    }

    public CloneCollection(T item)
    {
        _array = ArrayPool<T>.Shared.Rent(1);
        _length = 1;
        _array[0] = item;
    }

    public CloneCollection(IEnumerable<T> source)
    {
        if (source.TryGetNonEnumeratedCount(out var count))
        {
            var array = ArrayPool<T>.Shared.Rent(count);

            if (source is ICollection<T> c)
            {
                c.CopyTo(array, 0);
            }
            else
            {
                var i = 0;
                foreach (var item in source) array[i++] = item;
            }

            this._array = array;
            _length = count;
        }
        else
        {
            var array = ArrayPool<T>.Shared.Rent(16);

            var i = 0;
            foreach (var item in source)
            {
                TryEnsureCapacity(ref array, i);
                array[i++] = item;
            }

            this._array = array;
            _length = i;
        }
    }

    public CloneCollection(ReadOnlySpan<T> source)
    {
        var array = ArrayPool<T>.Shared.Rent(source.Length);
        source.CopyTo(array);
        this._array = array;
        _length = source.Length;
    }

    private static void TryEnsureCapacity(ref T[] array, int index)
    {
        if (array.Length == index)
        {
            var newArray = ArrayPool<T>.Shared.Rent(index * 2);
            Array.Copy(array, newArray, index);
            ArrayPool<T>.Shared.Return(array, RuntimeHelpersEx.IsReferenceOrContainsReferences<T>());
            array = newArray;
        }
    }

    public void Dispose()
    {
        if (_array != null)
        {
            ArrayPool<T>.Shared.Return(_array, RuntimeHelpersEx.IsReferenceOrContainsReferences<T>());
            _array = null;
        }
    }

    // Optimize to use Count and CopyTo
    private class EnumerableCollection : ICollection<T>
    {
        private readonly T[] _array;
        private readonly int _count;

        public EnumerableCollection(T[]? array, int count)
        {
            if (array == null)
            {
                this._array = Array.Empty<T>();
                this._count = 0;
            }
            else
            {
                this._array = array;
                this._count = count;
            }
        }

        public int Count => _count;

        public bool IsReadOnly => true;

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(T[] dest, int destIndex)
        {
            Array.Copy(_array, 0, dest, destIndex, _count);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _count; i++) yield return _array[i];
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}