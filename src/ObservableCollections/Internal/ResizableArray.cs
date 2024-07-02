using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ObservableCollections.Internal;

// internal ref struct ResizableArray<T>
internal struct ResizableArray<T> : IDisposable
{
    private T[]? _array;
    private int _count;

    public ReadOnlySpan<T> Span => _array.AsSpan(0, _count);

    public ResizableArray(int initialCapacity)
    {
        _array = ArrayPool<T>.Shared.Rent(initialCapacity);
        _count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if (_array == null) Throw();
        if (_array.Length == _count) EnsureCapacity();
        _array[_count++] = item;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureCapacity()
    {
        var newArray = _array.AsSpan().ToArray();
        ArrayPool<T>.Shared.Return(_array!, RuntimeHelpersEx.IsReferenceOrContainsReferences<T>());
        _array = newArray;
    }

    public void Dispose()
    {
        if (_array != null)
        {
            ArrayPool<T>.Shared.Return(_array, RuntimeHelpersEx.IsReferenceOrContainsReferences<T>());
            _array = null;
        }
    }

    [DoesNotReturn]
    private static void Throw()
    {
        throw new ObjectDisposedException("ResizableArray");
    }
}