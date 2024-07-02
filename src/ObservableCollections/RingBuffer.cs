using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ObservableCollections;

public sealed class RingBuffer<T> : IList<T>, IReadOnlyList<T>
{
    private T[] _buffer;
    private int _head;
    private int _mask;

    public RingBuffer()
    {
        _buffer = new T[8];
        _head = 0;
        Count = 0;
        _mask = _buffer.Length - 1;
    }

    public RingBuffer(int capacity)
    {
        _buffer = new T[CalculateCapacity(capacity)];
        _head = 0;
        Count = 0;
        _mask = _buffer.Length - 1;
    }

    public RingBuffer(IEnumerable<T> collection)
    {
        var array = collection.TryGetNonEnumeratedCount(out var count)
            ? new T[CalculateCapacity(count)]
            : new T[8];
        var i = 0;
        foreach (var item in collection)
        {
            if (i == array.Length) Array.Resize(ref array, i * 2);
            array[i++] = item;
        }

        _buffer = array;
        _head = 0;
        Count = i;
        _mask = _buffer.Length - 1;
    }

    public T this[int index]
    {
        get
        {
            var i = (_head + index) & _mask;
            return _buffer[i];
        }
        set
        {
            var i = (_head + index) & _mask;
            _buffer[i] = value;
        }
    }

    public int Count { get; private set; }

    public bool IsReadOnly => false;

    void ICollection<T>.Add(T item)
    {
        AddLast(item);
    }

    public void Clear()
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        _head = 0;
        Count = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        if (Count == 0) yield break;

        var start = _head & _mask;
        var end = (_head + Count) & _mask;

        if (end > start)
        {
            // start...end
            for (var i = start; i < end; i++) yield return _buffer[i];
        }
        else
        {
            // start...
            for (var i = start; i < _buffer.Length; i++) yield return _buffer[i];
            // 0...end
            for (var i = 0; i < end; i++) yield return _buffer[i];
        }
    }

    public bool Contains(T item)
    {
        return IndexOf(item) != -1;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        var span = GetSpan();
        var dest = array.AsSpan(arrayIndex);
        span.First.CopyTo(dest);
        span.Second.CopyTo(dest.Slice(span.First.Length));
    }

    public int IndexOf(T item)
    {
        var i = 0;
        foreach (var v in GetSpan())
        {
            if (EqualityComparer<T>.Default.Equals(item, v)) return i;
            i++;
        }

        return -1;
    }

    void IList<T>.Insert(int index, T item)
    {
        throw new NotSupportedException();
    }

    bool ICollection<T>.Remove(T item)
    {
        throw new NotSupportedException();
    }

    void IList<T>.RemoveAt(int index)
    {
        throw new NotSupportedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private static int CalculateCapacity(int size)
    {
        size--;
        size |= size >> 1;
        size |= size >> 2;
        size |= size >> 4;
        size |= size >> 8;
        size |= size >> 16;
        size += 1;

        if (size < 8) size = 8;
        return size;
    }

    public void AddLast(T item)
    {
        if (Count == _buffer.Length) EnsureCapacity();

        var index = (_head + Count) & _mask;
        _buffer[index] = item;
        Count++;
    }

    public void AddFirst(T item)
    {
        if (Count == _buffer.Length) EnsureCapacity();

        _head = (_head - 1) & _mask;
        _buffer[_head] = item;
        Count++;
    }

    public T RemoveLast()
    {
        if (Count == 0) ThrowForEmpty();

        var index = (_head + Count - 1) & _mask;
        var v = _buffer[index];
        _buffer[index] = default!;
        Count--;
        return v;
    }

    public T RemoveFirst()
    {
        if (Count == 0) ThrowForEmpty();

        var index = _head & _mask;
        var v = _buffer[index];
        _buffer[index] = default!;
        _head += 1;
        Count--;
        return v;
    }

    private void EnsureCapacity()
    {
        var newBuffer = new T[_buffer.Length * 2];

        var i = _head & _mask;
        _buffer.AsSpan(i).CopyTo(newBuffer);

        if (i != 0) _buffer.AsSpan(0, i).CopyTo(newBuffer.AsSpan(_buffer.Length - i));

        _head = 0;
        _buffer = newBuffer;
        _mask = newBuffer.Length - 1;
    }

    public RingBufferSpan<T> GetSpan()
    {
        if (Count == 0) return new RingBufferSpan<T>(Array.Empty<T>(), Array.Empty<T>(), 0);

        var start = _head & _mask;
        var end = (_head + Count) & _mask;

        if (end > start)
        {
            var first = _buffer.AsSpan(start, Count);
            var second = Array.Empty<T>().AsSpan();
            return new RingBufferSpan<T>(first, second, Count);
        }
        else
        {
            var first = _buffer.AsSpan(start, _buffer.Length - start);
            var second = _buffer.AsSpan(0, end);
            return new RingBufferSpan<T>(first, second, Count);
        }
    }

    public IEnumerable<T> Reverse()
    {
        if (Count == 0) yield break;

        var start = _head & _mask;
        var end = (_head + Count) & _mask;

        if (end > start)
        {
            // end...start
            for (var i = end - 1; i >= start; i--) yield return _buffer[i];
        }
        else
        {
            // end...0
            for (var i = end - 1; i >= 0; i--) yield return _buffer[i];

            // ...start
            for (var i = _buffer.Length - 1; i >= start; i--) yield return _buffer[i];
        }
    }

    public T[] ToArray()
    {
        var result = new T[Count];
        var i = 0;
        foreach (var item in GetSpan()) result[i++] = item;
        return result;
    }

    public int BinarySearch(T item)
    {
        return BinarySearch(item, Comparer<T>.Default);
    }

    public int BinarySearch(T item, IComparer<T> comparer)
    {
        var lo = 0;
        var hi = Count - 1;

        while (lo <= hi)
        {
            var mid = (int) (((uint) hi + (uint) lo) >> 1);
            var found = comparer.Compare(this[mid], item);

            if (found == 0) return mid;
            if (found < 0)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        return ~lo;
    }

    [DoesNotReturn]
    private static void ThrowForEmpty()
    {
        throw new InvalidOperationException("RingBuffer is empty.");
    }
}

public readonly ref struct RingBufferSpan<T>
{
    public readonly ReadOnlySpan<T> First;
    public readonly ReadOnlySpan<T> Second;
    public readonly int Count;

    internal RingBufferSpan(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int count)
    {
        First = first;
        Second = second;
        Count = count;
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    public ref struct Enumerator
    {
        private ReadOnlySpan<T>.Enumerator _firstEnumerator;
        private ReadOnlySpan<T>.Enumerator _secondEnumerator;
        private bool _useFirst;

        public Enumerator(RingBufferSpan<T> span)
        {
            _firstEnumerator = span.First.GetEnumerator();
            _secondEnumerator = span.Second.GetEnumerator();
            _useFirst = true;
        }

        public bool MoveNext()
        {
            if (_useFirst)
            {
                if (_firstEnumerator.MoveNext())
                    return true;
                _useFirst = false;
            }

            return _secondEnumerator.MoveNext();
        }

        public T Current => _useFirst ? _firstEnumerator.Current : _secondEnumerator.Current;
    }
}