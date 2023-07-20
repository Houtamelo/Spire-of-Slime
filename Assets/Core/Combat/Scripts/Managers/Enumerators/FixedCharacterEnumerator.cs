using System;
using Core.Combat.Scripts.Behaviour;
using Core.Utils.Collections;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Managers.Enumerators
{
    public ref struct FixedCharacterEnumerator
    {
        private FixedEnumerator<CharacterStateMachine> _leftCharacters;
        private FixedEnumerator<CharacterStateMachine> _rightCharacters;

        public CharacterStateMachine Current { get; private set; }

        public FixedCharacterEnumerator([NotNull] CharacterManager characterManager)
        {
            _leftCharacters = characterManager.FixedOnLeftSide;
            _rightCharacters = characterManager.FixedOnRightSide;
            Current = null;
            _unDisposed = true;
        }

        public bool MoveNext()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(FixedCharacterEnumerator));

            if (_leftCharacters.MoveNext())
            {
                Current = _leftCharacters.Current;
                return true;
            }
            
            if (_rightCharacters.MoveNext())
            {
                Current = _rightCharacters.Current;
                return true;
            }
            
            return false;
        }

        public FixedCharacterEnumerator GetEnumerator()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(FixedCharacterEnumerator));

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