using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Utils.Collections
{
    /// <summary> Caches the elements of a collection when created </summary>
    public ref struct FixedEnumerator<T>
    {
        public readonly int Length;
        private readonly T[] _pool;
        
        private int _index;

        private bool _allocated;
        public readonly bool Allocated => _allocated;

        public FixedEnumerator([NotNull] List<T> source)
        {
            Length = source.Count;
            _pool = ArrayPool<T>.Shared.Rent(Length);
            source.CopyTo(_pool);
            _allocated = true;
            _index = -1;
            Current = default;
        }
        
        public FixedEnumerator([NotNull] T[] source)
        {
            Length = source.Length;
            _pool = ArrayPool<T>.Shared.Rent(Length);
            Array.Copy(source, destinationArray: _pool, Length);
            _allocated = true;
            _index = -1;
            Current = default;
        }

        public FixedEnumerator([NotNull] HashSet<T> source)
        {
            Length = source.Count;
            _pool = ArrayPool<T>.Shared.Rent(Length);
            source.CopyTo(_pool);
            _allocated = true;
            _index = -1;
            Current = default;
        }

        /// <summary> Takes ownership of the pool. </summary>
        private FixedEnumerator(T[] pool, int length)
        {
            Length = length;
            _pool = pool;
            _allocated = true;
            _index = -1;
            Current = default;
        }

        public static FixedEnumerator<KeyValuePair<TKey, TValue>> FromDictionary<TKey, TValue>([NotNull] Dictionary<TKey, TValue> source)
        {
            int count = source.Count;
            KeyValuePair<TKey, TValue>[] pool = ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Rent(count);
            
            int index = 0;
            foreach (KeyValuePair<TKey, TValue> pair in source)
            {
                pool[index] = pair;
                index++;
            }
            
            return new FixedEnumerator<KeyValuePair<TKey, TValue>>(pool, count);
        }

        public bool MoveNext()
        {
            _index++;
            if (_index >= Length)
                return false;
                
            Current = _pool[_index];
            return _index < Length;
        }

        public T Current { get; private set; }

        public FixedEnumerator<T> GetEnumerator()
        {
            if (_allocated == false)
                throw new ObjectDisposedException(nameof(FixedEnumerator<T>));

            return this;
        }

        public readonly Option<TDesired> FindType<TDesired>() where TDesired : class, T
        {
            if (_allocated == false)
                throw new ObjectDisposedException(nameof(FixedEnumerator<T>));

            for (int index = 0; index < Length; index++)
            {
                T variable = _pool[index];
                if (variable is TDesired casted)
                    return casted;
            }

            return Option.None;
        }

        public readonly Option<int> IndexOf(T value)
        {
            if (_allocated == false)
                throw new ObjectDisposedException(nameof(FixedEnumerator<T>));

            int index = Array.IndexOf(_pool, value, startIndex: 0, count: Length);
            return index != -1 ? index : Option.None;
        }

        public void Dispose()
        {
            if (_allocated == false)
                return;
                
            _allocated = false;
            ArrayPool<T>.Shared.Return(_pool);
        }
    }
}