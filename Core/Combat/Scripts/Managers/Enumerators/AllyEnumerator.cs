/*using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using ListPool;

namespace Core.Combat.Scripts.Manager.Enumerators
{
    public ref struct AllyEnumerator
    {
        private readonly CharacterStateMachine _character;
        private ValueListPool<CharacterStateMachine> _possibleAllies;
        
        public CharacterStateMachine Current { get; private set; }
        private int _index;

        public AllyEnumerator(CombatManager combatManager, CharacterStateMachine character)
        {
            _character = character;
            IReadOnlyList<CharacterStateMachine> allies = character.PositionHandler.IsLeftSide ? combatManager.LeftCharacters : combatManager.RightCharacters;
            _possibleAllies = new ValueListPool<CharacterStateMachine>(allies.Count);
            foreach (CharacterStateMachine ally in allies)
                _possibleAllies.Add(ally);
            
            _index = 0;
            Current = null;
            _unDisposed = true;
        }

        public bool MoveNext()
        {
            while (_index < _possibleAllies.Count)
            {
                Current = _possibleAllies[_index];
                _index++;
                if (Current.IsAlive() && Current != _character)
                    return true;
            }

            return false;
        }

        public AllyEnumerator GetEnumerator() => this;
        
        private bool _unDisposed;
        private bool Disposed => _unDisposed == false;
        
        public void Dispose()
        {
            if (Disposed)
                return;
            
            _unDisposed = false;
            _possibleAllies.Dispose();
        }
    }
}*/