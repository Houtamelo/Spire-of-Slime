/*using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using ListPool;

namespace Core.Combat.Scripts.Manager.Enumerators
{
    public ref struct EnemyEnumerator
    {
        private readonly CharacterStateMachine _character;
        private ValueListPool<CharacterStateMachine> _possibleEnemies;

        public CharacterStateMachine Current { get; private set; }
        private int _index;

        public EnemyEnumerator(CombatManager combatManager, CharacterStateMachine character)
        {
            _character = character;
            IReadOnlyList<CharacterStateMachine> enemies = character.PositionHandler.IsLeftSide ? combatManager.RightCharacters : combatManager.LeftCharacters;
            _possibleEnemies = new ValueListPool<CharacterStateMachine>(enemies.Count);
            foreach (CharacterStateMachine enemy in enemies)
                _possibleEnemies.Add(enemy);
            
            _index = 0;
            Current = null;
            _unDisposed = true;
        }

        public bool MoveNext()
        {
            while (_index < _possibleEnemies.Count)
            {
                Current = _possibleEnemies[_index];
                _index++;
                if (Current.IsAlive() && Current != _character)
                    return true;
            }
            
            return false;
        }

        public EnemyEnumerator GetEnumerator() => this;
        
        private bool _unDisposed;
        private bool Disposed => _unDisposed == false;
        
        public void Dispose()
        {
            if (Disposed)
                return;
            
            _unDisposed = false;
            _possibleEnemies.Dispose();
        }
    }
}*/