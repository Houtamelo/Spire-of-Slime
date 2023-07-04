using System;
using System.Collections;

namespace Core.Visual_Novel.Scripts
{
    public class YieldableCommandWrapper : IEnumerator
    {
        private IEnumerator _source;
        private readonly bool _allowImmediateFinish;
        private readonly Action _onImmediateFinish;
        private bool _requestedFinish;
        
        public YieldableCommandWrapper(IEnumerator source, bool allowImmediateFinish, Action onImmediateFinish)
        {
            _source = source;
            _allowImmediateFinish = allowImmediateFinish;
            _onImmediateFinish = onImmediateFinish;
        }

        public bool TryImmediateFinish()
        {
            if (_requestedFinish)
                return false;
            
            _requestedFinish = true;
            if (_allowImmediateFinish)
            {
                _onImmediateFinish?.Invoke();
                return true;
            }
            
            return false;
        }

        public bool MoveNext() => _source.MoveNext();
        public void Reset() => _source.Reset();
        public object Current => _source.Current;
    }
}