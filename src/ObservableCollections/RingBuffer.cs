﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ObservableCollections;

public sealed class RingBuffer<T> : IList<T>, IReadOnlyList<T>
{
    private T[] buffer;
    private int head;
    private int count;
    private int mask;

    public RingBuffer()
    {
        buffer = new T[8];
        head = 0;
        count = 0;
        mask = buffer.Length - 1;
    }

    public RingBuffer(int capacity)
    {
        buffer = new T[CalculateCapacity(capacity)];
        head = 0;
        count = 0;
        mask = buffer.Length - 1;
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

        buffer = array;
        head = 0;
        this.count = i;
        mask = buffer.Length - 1;
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

    public T this[int index]
    {
        get
        {
            var i = (head + index) & mask;
            return buffer[i];
        }
        set
        {
            var i = (head + index) & mask;
            buffer[i] = value;
        }
    }

    public int Count => count;

    public bool IsReadOnly => false;

    public void AddLast(T item)
    {
        if (count == buffer.Length) EnsureCapacity();

        var index = (head + count) & mask;
        buffer[index] = item;
        count++;
    }

    public void AddFirst(T item)
    {
        if (count == buffer.Length) EnsureCapacity();

        head = (head - 1) & mask;
        buffer[head] = item;
        count++;
    }

    public T RemoveLast()
    {
        if (count == 0) ThrowForEmpty();

        var index = (head + count - 1) & mask;
        var v = buffer[index];
        buffer[index] = default!;
        count--;
        return v;
    }

    public T RemoveFirst()
    {
        if (count == 0) ThrowForEmpty();

        var index = head & mask;
        var v = buffer[index];
        buffer[index] = default!;
        head += 1;
        count--;
        return v;
    }

    private void EnsureCapacity()
    {
        var newBuffer = new T[buffer.Length * 2];

        var i = head & mask;
        buffer.AsSpan(i).CopyTo(newBuffer);

        if (i != 0) buffer.AsSpan(0, i).CopyTo(newBuffer.AsSpan(buffer.Length - i));

        head = 0;
        buffer = newBuffer;
        mask = newBuffer.Length - 1;
    }

    void ICollection<T>.Add(T item)
    {
        AddLast(item);
    }

    public void Clear()
    {
        Array.Clear(buffer, 0, buffer.Length);
        head = 0;
        count = 0;
    }

    public RingBufferSpan<T> GetSpan()
    {
        if (count == 0) return new RingBufferSpan<T>(Array.Empty<T>(), Array.Empty<T>(), 0);

        var start = head & mask;
        var end = (head + count) & mask;

        if (end > start)
        {
            var first = buffer.AsSpan(start, count);
            var second = Array.Empty<T>().AsSpan();
            return new RingBufferSpan<T>(first, second, count);
        }
        else
        {
            var first = buffer.AsSpan(start, buffer.Length - start);
            var second = buffer.AsSpan(0, end);
            return new RingBufferSpan<T>(first, second, count);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        if (count == 0) yield break;

        var start = head & mask;
        var end = (head + count) & mask;

        if (end > start)
        {
            // start...end
            for (var i = start; i < end; i++) yield return buffer[i];
        }
        else
        {
            // start...
            for (var i = start; i < buffer.Length; i++) yield return buffer[i];
            // 0...end
            for (var i = 0; i < end; i++) yield return buffer[i];
        }
    }

    public IEnumerable<T> Reverse()
    {
        if (count == 0) yield break;

        var start = head & mask;
        var end = (head + count) & mask;

        if (end > start)
        {
            // end...start
            for (var i = end - 1; i >= start; i--) yield return buffer[i];
        }
        else
        {
            // end...0
            for (var i = end - 1; i >= 0; i--) yield return buffer[i];

            // ...start
            for (var i = buffer.Length - 1; i >= start; i--) yield return buffer[i];
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

    public T[] ToArray()
    {
        var result = new T[count];
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
        var hi = count - 1;

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
        private ReadOnlySpan<T>.Enumerator firstEnumerator;
        private ReadOnlySpan<T>.Enumerator secondEnumerator;
        private bool useFirst;

        public Enumerator(RingBufferSpan<T> span)
        {
            firstEnumerator = span.First.GetEnumerator();
            secondEnumerator = span.Second.GetEnumerator();
            useFirst = true;
        }

        public bool MoveNext()
        {
            if (useFirst)
            {
                if (firstEnumerator.MoveNext())
                    return true;
                else
                    useFirst = false;
            }

            return secondEnumerator.MoveNext();
        }

        public T Current => useFirst ? firstEnumerator.Current : secondEnumerator.Current;
    }
}