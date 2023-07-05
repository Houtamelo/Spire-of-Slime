using System;
using Core.Combat.Scripts.Behaviour;
using Core.Utils.Collections;

namespace Core.Combat.Scripts.Managers.Enumerators
{
    public ref struct CharacterEnumerator
    {
        private FixedEnumerable<CharacterStateMachine> _leftCharacters;
        private FixedEnumerable<CharacterStateMachine> _rightCharacters;

        public CharacterStateMachine Current { get; private set; }
        private int _leftIndex;
        private int _rightIndex;

        public CharacterEnumerator(CharacterManager characterManager)
        {
            _leftCharacters = characterManager.FixedOnLeftSide;
            _rightCharacters = characterManager.FixedOnRightSide;

            _leftIndex = 0;
            _rightIndex = 0;
            
            Current = null;
            _unDisposed = true;
        }

        public bool MoveNext()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(CharacterEnumerator));
            }
            
            if (_leftIndex < _leftCharacters.Length)
            {
                Current = _leftCharacters[_leftIndex];
                _leftIndex++;
                return true;
            }
            
            if (_rightIndex < _rightCharacters.Length)
            {
                Current = _rightCharacters[_rightIndex];
                _rightIndex++;
                return true;
            }
            
            return false;
        }

        public CharacterEnumerator GetEnumerator()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(CharacterEnumerator));
            }
            
            return this;
        }

        private bool _unDisposed;
        private bool Disposed => _unDisposed == false;
        
        public void Dispose()
        {
            if (Disposed)
                return;
            
            _unDisposed = false;
            _leftCharacters.Dispose();
            _rightCharacters.Dispose();
        }
    }
}