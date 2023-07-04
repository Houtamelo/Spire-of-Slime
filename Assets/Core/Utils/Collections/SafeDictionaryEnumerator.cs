using System.Buffers;
using System.Collections.Generic;
using NetFabric.Hyperlinq;

namespace Utils.Collections
{
    public ref struct SafeDictionaryEnumerator<TKey, TValue>
    {
        private readonly Lease<KeyValuePair<TKey, TValue>> _lease;
        private int _index;
        private bool _unDisposed;
        private bool Disposed => _unDisposed == false;

        public SafeDictionaryEnumerator(IDictionary<TKey, TValue> source)
        {
            _lease = source.AsValueEnumerable().ToArray(ArrayPool<KeyValuePair<TKey, TValue>>.Shared);
            _index = -1;
            Current = default;
            _unDisposed = true;
        }
            
        public bool MoveNext()
        {
            _index++;
            if (_index >= _lease.Length)
                return false;
                
            Current = _lease.Rented[_index];
            return _index < _lease.Length;
        }
            
        public KeyValuePair<TKey, TValue> Current { get; private set; }
            
        public void Dispose()
        {
            if (Disposed)
                return;
                
            _unDisposed = false;
            _lease.Dispose();
        }
        
        public SafeDictionaryEnumerator<TKey, TValue> GetEnumerator() => this;
    }
}