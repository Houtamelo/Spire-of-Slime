using System;
using System.Collections.Generic;
using Core.Utils.Patterns;
using ListPool;

namespace Core.Utils.Collections
{
    /// <summary> Caches a collection when created </summary>
    public ref struct FixedEnumerable<T> where T : IEquatable<T>
    {
        private ValueListPool<T> _pool;
        private bool _unDisposed;
        private bool Disposed => _unDisposed == false;
        public readonly int Length;
        
        public T this[int index]
        {
            get
            {
                if (Disposed)
                {
                    throw new ObjectDisposedException(nameof(FixedEnumerable<T>));
                }
                
                return _pool[index];
            }
        }

        public FixedEnumerable(ICollection<T> source)
        {
            Length = source.Count;
            _pool = new ValueListPool<T>(Length);
            foreach (T element in source)
                _pool.Add(element);
            
            _unDisposed = true;
        }

        public void Dispose()
        {
            if (Disposed)
                return;
                
            _unDisposed = false;
            _pool.Dispose();
        }

        public ValueListPool<T>.Enumerator GetEnumerator()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(FixedEnumerable<T>));
            }
            
            return _pool.GetEnumerator();
        }

        public Option<TDesired> FindType<TDesired>()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(FixedEnumerable<T>));
            }
            
            foreach (T variable in _pool)
                if (variable is TDesired casted)
                    return casted;

            return Option.None;
        }

        public Option<int> IndexOf(T value)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(FixedEnumerable<T>));
            }
            
            int index = 0;
            foreach (T element in _pool)
            {
                if (EqualityComparer<T>.Default.Equals(element, value))
                {
                    return Option<int>.Some(index);
                }

                index++;
            }
            
            return Option.None;
        }
    }
}