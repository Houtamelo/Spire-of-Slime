using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Core.Utils.Collections
{
    /// <summary> High-performance implementation of IList with zero heap allocations. </summary>
    public ref struct CustomValuePooledList<T>
    {
        public enum SourceType
        {
            UseAsInitialBuffer,
            UseAsReferenceData,
            Copy
        }

        private const int MinimumCapacity = 16;
        private T[] _disposableBuffer;
        public Span<T> Buffer;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CustomValuePooledList(int capacity)
        {
            _disposableBuffer = ArrayPool<T>.Shared.Rent(capacity < MinimumCapacity ? MinimumCapacity : capacity);
            Buffer = _disposableBuffer;
            Count = 0;
        }

        public CustomValuePooledList(Span<T> source, SourceType sourceType)
        {
            switch (sourceType)
            {
                case SourceType.UseAsInitialBuffer:
                    Buffer = source;
                    Count = 0;
                    _disposableBuffer = null;

                    break;
                case SourceType.UseAsReferenceData:
                    Buffer = source;
                    Count = source.Length;
                    _disposableBuffer = null;

                    break;
                case SourceType.Copy:
                {
                    T[] disposableBuffer = ArrayPool<T>.Shared.Rent(source.Length > MinimumCapacity ? source.Length : MinimumCapacity);

                    source.CopyTo(disposableBuffer);
                    Buffer = disposableBuffer;
                    _disposableBuffer = disposableBuffer;
                    Count = source.Length;

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, message: null);
            }
        }
        
        public int Capacity => Buffer.Length;
        
        public void Dispose()
        {
            Count = 0;
            T[] buffer = _disposableBuffer;
            if (buffer != null)
                ArrayPool<T>.Shared.Return(buffer);
        }
        
        public int Count { get; private set; }

        public bool IsReadOnly => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            Span<T> buffer = Buffer;
            int count = Count;

            if (count < buffer.Length)
            {
                buffer[count] = item;
                Count = count + 1;
            }
            else
            {
                AddWithResize(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => Count = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(T item) => IndexOf(item) > -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int IndexOf(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            Span<T> slice = Buffer.Slice(0, Count);
            for (int i = 0; i < slice.Length; i++)
            {
                if (comparer.Equals(slice[i], item))
                    return i;
            }

            return -1;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int IndexOf(T item, IEqualityComparer<T> comparer)
        {
            Span<T> slice = Buffer.Slice(0, Count);
            for (int i = 0; i < slice.Length; i++)
            {
                if (comparer.Equals(slice[i], item))
                    return i;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(Span<T> array) => Buffer.Slice(0, Count).CopyTo(array);

        public bool Remove([CanBeNull] T item)
        {
            if (item is null) return false;

            int index = IndexOf(item);

            if (index == -1) return false;

            RemoveAt(index);

            return true;
        }

        public void Insert(int index, T item)
        {
            int count = Count;
            Span<T> buffer = Buffer;

            if (buffer.Length == count)
            {
                int newCapacity = count * 2;
                EnsureCapacity(newCapacity);
                buffer = Buffer;
            }

            if (index < count)
            {
                buffer.Slice(index, count).CopyTo(buffer.Slice(index + 1));
                buffer[index] = item;
                Count++;
            }
            else if (index == count)
            {
                buffer[index] = item;
                Count++;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void RemoveAt(int index)
        {
            int count = Count;
            Span<T> buffer = Buffer;

            if (index >= count) throw new IndexOutOfRangeException(nameof(index));

            count--;
            buffer.Slice(index + 1).CopyTo(buffer.Slice(index));
            Count = count;
        }

        public T TakeRandom([NotNull] Random random)
        {
            int index = random.Next(Count);
            T item = this[index];
            RemoveAt(index);
            return item;
        }

#if NETSTANDARD2_1
        [MaybeNull]
#endif
        public readonly ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index >= Count)
                    throw new IndexOutOfRangeException(nameof(index));

                return ref Buffer[index];
            }
        }

        public void AddRange(ReadOnlySpan<T> items)
        {
            int count = Count;
            Span<T> buffer = Buffer;

            bool isCapacityEnough = buffer.Length - items.Length - count >= 0;
            if (!isCapacityEnough)
            {
                EnsureCapacity(buffer.Length + items.Length);
                buffer = _disposableBuffer;
            }

            items.CopyTo(buffer.Slice(count));
            Count += items.Length;
        }

        public void AddRange([NotNull] T[] array)
        {
            int count = Count;
            T[] disposableBuffer = _disposableBuffer;
            Span<T> buffer = Buffer;

            bool isCapacityEnough = buffer.Length - array.Length - count >= 0;
            if (!isCapacityEnough)
            {
                EnsureCapacity(buffer.Length + array.Length);
                disposableBuffer = _disposableBuffer;
                array.CopyTo(disposableBuffer, count);
                Count += array.Length;
                return;
            }

            if (disposableBuffer != null)
                array.CopyTo(disposableBuffer, count);
            else
                array.AsSpan().CopyTo(buffer.Slice(count));

            Count += array.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan() => Buffer.Slice(0, Count);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddWithResize(T item)
        {
            ArrayPool<T> arrayPool = ArrayPool<T>.Shared;
            if (_disposableBuffer == null)
            {
                Span<T> oldBuffer = Buffer;
                int newSize = oldBuffer.Length * 2;
                T[] newBuffer = arrayPool.Rent(newSize > MinimumCapacity ? newSize : MinimumCapacity);
                oldBuffer.CopyTo(newBuffer);
                newBuffer[oldBuffer.Length] = item;
                _disposableBuffer = newBuffer;
                Buffer = newBuffer;
                Count++;
            }
            else
            {
                T[] oldBuffer = _disposableBuffer;
                T[] newBuffer = arrayPool.Rent(oldBuffer.Length * 2);
                int count = oldBuffer.Length;

                Array.Copy(oldBuffer, 0, newBuffer, 0, count);

                newBuffer[count] = item;
                _disposableBuffer = newBuffer;
                Buffer = newBuffer;
                Count = count + 1;
                arrayPool.Return(oldBuffer);
            }
        }
        
        public void EnsureCapacity(int capacity)
        {
            if(capacity <= Buffer.Length) return;
            ArrayPool<T> arrayPool = ArrayPool<T>.Shared;
            T[] newBuffer = arrayPool.Rent(capacity);
            Span<T> oldBuffer = Buffer;

            oldBuffer.CopyTo(newBuffer);

            Buffer = newBuffer;
            if (_disposableBuffer != null)
                arrayPool.Return(_disposableBuffer);

            _disposableBuffer = newBuffer;
        }
        
        [NotNull]
        public readonly T[] ToArray()
        {
            T[] array = new T[Count];

            for (int i = 0; i < Count; i++)
                array[i] = Buffer[i];

            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Enumerator GetEnumerator() => new Enumerator(Buffer.Slice(0, Count));

        public ref struct Enumerator
        {
            private readonly Span<T> _source;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(Span<T> source)
            {
                _source = source;
                _index = -1;
            }

#if NETSTANDARD2_1
        [MaybeNull]
#endif
            public readonly ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _source[_index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => unchecked(++_index < _source.Length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                _index = -1;
            }
        }
    }
    
    public static class ValuePooledListUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FastIndexOf<T>(this ref CustomValuePooledList<T> source, T item) where T : IEquatable<T>
        {
            ref Span<T> buffer = ref source.Buffer;
            return buffer.Slice(0, source.Count).IndexOf(item);
        }
    }
}