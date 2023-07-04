/*using System.Buffers;
using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using ListPool;
using NetFabric.Hyperlinq;

namespace Core.Combat.Scripts.Manager.Enumerators
{
    public ref struct AliveCharacterEnumerator
    {
        private ValueListPool<CharacterStateMachine> _leftCharacters;
        private ValueListPool<CharacterStateMachine> _rightCharacters;

        public CharacterStateMachine Current { get; private set; }
        private int _leftIndex;
        private int _rightIndex;

        public AliveCharacterEnumerator(CombatManager combatManager)
        {
            _leftIndex = 0;
            _rightIndex = 0;
            
            IReadOnlyList<CharacterStateMachine> leftCharacters = combatManager.LeftCharacters;
            _leftCharacters = new ValueListPool<CharacterStateMachine>(leftCharacters.Count);
            foreach (CharacterStateMachine character in leftCharacters)
                _leftCharacters.Add(character);
            
            IReadOnlyList<CharacterStateMachine> rightCharacters = combatManager.RightCharacters;
            _rightCharacters = new ValueListPool<CharacterStateMachine>(rightCharacters.Count);
            foreach (CharacterStateMachine character in rightCharacters)
                _rightCharacters.Add(character);
            
            Current = default;
            _unDisposed = true;
        }

        public bool MoveNext()
        {
            while (_leftIndex < _leftCharacters.Count)
            {
                Current = _leftCharacters[_leftIndex];
                _leftIndex++;
                if (Current.IsAlive())
                    return true;
            }
            
            while (_rightIndex < _rightCharacters.Count)
            {
                Current = _rightCharacters[_rightIndex];
                _rightIndex++;
                if (Current.IsAlive())
                    return true;
            }

            return false;
        }

        public AliveCharacterEnumerator GetEnumerator() => this;
        
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
}*/