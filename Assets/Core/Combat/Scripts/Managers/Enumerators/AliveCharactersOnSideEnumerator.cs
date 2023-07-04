/*using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using ListPool;
using NetFabric.Hyperlinq;

namespace Core.Combat.Scripts.Manager.Enumerators
{
    public ref struct AliveCharactersOnSideEnumerator
    {
        private ValueListPool<CharacterStateMachine> _characters;

        public CharacterStateMachine Current { get; private set; }
        private int _index;

        public AliveCharactersOnSideEnumerator(CombatManager combatManager, bool isLeftSide)
        {
            IReadOnlyList<CharacterStateMachine> characters = isLeftSide ? combatManager.LeftCharacters : combatManager.RightCharacters;
            _characters = new ValueListPool<CharacterStateMachine>(characters.Count);
            foreach (CharacterStateMachine character in characters)
                _characters.Add(character);
            
            _index = 0;
            Current = null;
            _unDisposed = true;
        }

        public bool MoveNext()
        {
            while (_index < _characters.Count)
            {
                Current = _characters[_index];
                _index++;
                if (Current.IsAlive())
                    return true;
            }

            return false;
        }

        public AliveCharactersOnSideEnumerator GetEnumerator() => this;
        
        private bool _unDisposed;
        private bool Disposed => _unDisposed == false;
        
        public void Dispose()
        {
            if (Disposed)
                return;
            
            _unDisposed = false;
            _characters.Dispose();
        }
    }
}*/